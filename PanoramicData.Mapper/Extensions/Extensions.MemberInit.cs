using PanoramicData.Mapper.Configuration.Annotations;
using PanoramicData.Mapper.Internal;
using System.Linq.Expressions;
using System.Reflection;

namespace PanoramicData.Mapper.QueryableExtensions;

/// <summary>
/// Builds the member-init projection and decides which destination members are bound.
/// </summary>
public static partial class Extensions
{
	/// <summary>
	/// Builds a <c>src =&gt; new TDest { ... }</c> lambda for the given type pair. The <c>path</c>
	/// argument carries the chain of (source, destination) type pairs currently being expanded, used
	/// to prevent infinite recursion on self-referential maps and to honour any configured MaxDepth.
	/// </summary>
	private static LambdaExpression BuildMemberInitLambda(
		Type sourceType,
		Type destType,
		IConfigurationProvider provider,
		IReadOnlyList<(Type Source, Type Dest)> path,
		IReadOnlyCollection<string> membersToExpand)
	{
		var typeMap = provider.FindTypeMap(sourceType, destType);
		var sourceParam = Expression.Parameter(sourceType, "src");
		var body = BuildMemberInit(sourceParam, sourceType, destType, typeMap, provider, path, membersToExpand);
		return Expression.Lambda(body, sourceParam);
	}

	private static MemberInitExpression BuildMemberInit(
		Expression sourceAccess,
		Type sourceType,
		Type destType,
		TypeMap? typeMap,
		IConfigurationProvider provider,
		IReadOnlyList<(Type Source, Type Dest)> path,
		IReadOnlyCollection<string> membersToExpand)
	{
		var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanRead)
			.ToDictionary(p => p.Name, StringComparer.Ordinal);

		var destProperties = destType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanWrite);

		var bindings = new List<MemberBinding>();

		foreach (var destProp in destProperties)
		{
			if (IsExcludedFromProjection(destProp, typeMap, membersToExpand))
			{
				continue;
			}

			if (TryResolveSourceValue(destProp, sourceAccess, sourceProperties, typeMap, out var sourceValue, out var sourceValueType))
			{
				AddBinding(bindings, destProp, sourceValue, sourceValueType, provider, path);
			}
		}

		return Expression.MemberInit(Expression.New(destType), bindings);
	}

	/// <summary>
	/// Whether the destination member is kept out of the projection entirely, either by attribute
	/// or by the type map's ignore / explicit-expansion configuration.
	/// </summary>
	private static bool IsExcludedFromProjection(
		PropertyInfo destProp,
		TypeMap? typeMap,
		IReadOnlyCollection<string> membersToExpand)
	{
		if (destProp.GetCustomAttribute<IgnoreAttribute>() is not null)
		{
			return true;
		}

		if (typeMap is null)
		{
			return false;
		}

		if (typeMap.AllMembersIgnored || typeMap.IgnoredMembers.Contains(destProp.Name))
		{
			return true;
		}

		// ExplicitExpansion members are excluded from the projection unless explicitly requested.
		return typeMap.ExplicitExpansionMembers.Contains(destProp.Name)
			&& !membersToExpand.Contains(destProp.Name);
	}

	/// <summary>
	/// Finds the source expression feeding a destination member - a configured MapFrom expression
	/// if one exists, otherwise a same-named source property. Returns false when neither applies,
	/// leaving the member unbound.
	/// </summary>
	private static bool TryResolveSourceValue(
		PropertyInfo destProp,
		Expression sourceAccess,
		IReadOnlyDictionary<string, PropertyInfo> sourceProperties,
		TypeMap? typeMap,
		out Expression sourceValue,
		out Type sourceValueType)
	{
		if (typeMap is not null
			&& typeMap.PropertyMappings.TryGetValue(destProp.Name, out var mapping)
			&& mapping.SourceExpression is not null)
		{
			// Rebind the expression to use our source access (parameter or nested member)
			sourceValue = RebindExpression(mapping.SourceExpression, sourceAccess);
			sourceValueType = sourceValue.Type;
			return true;
		}

		// Convention-based: match by name
		if (sourceProperties.TryGetValue(destProp.Name, out var sourceProp))
		{
			sourceValue = Expression.Property(sourceAccess, sourceProp);
			sourceValueType = sourceProp.PropertyType;
			return true;
		}

		sourceValue = null!;
		sourceValueType = null!;
		return false;
	}

	/// <summary>
	/// Adds a member binding, projecting nested complex members and collections recursively when a
	/// type map exists, and falling back to scalar type-compatibility coercion otherwise.
	/// </summary>
	private static void AddBinding(
		List<MemberBinding> bindings,
		PropertyInfo destProp,
		Expression sourceValue,
		Type sourceType,
		IConfigurationProvider provider,
		IReadOnlyList<(Type Source, Type Dest)> path)
	{
		if (TryBuildComplexBinding(sourceValue, sourceType, destProp.PropertyType, provider, path, out var complexBinding))
		{
			// The member is a complex object / collection that this projector owns. Only emit a
			// binding when it was expanded; otherwise leave the destination member at its default
			// (e.g. an empty collection) rather than emitting a cast that would throw.
			if (complexBinding is not null)
			{
				bindings.Add(Expression.Bind(destProp, complexBinding));
			}

			return;
		}

		var converted = EnsureTypeCompatibility(sourceValue, destProp.PropertyType);
		bindings.Add(Expression.Bind(destProp, converted));
	}
}
