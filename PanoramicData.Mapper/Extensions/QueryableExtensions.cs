using PanoramicData.Mapper.Configuration.Annotations;
using PanoramicData.Mapper.Internal;
using System.Linq.Expressions;
using System.Reflection;

namespace PanoramicData.Mapper.QueryableExtensions;

/// <summary>
/// Extension methods for IQueryable to support projection.
/// </summary>
public static class Extensions
{
	private static readonly MethodInfo SelectMethod = typeof(Enumerable).GetMethods()
		.Single(m => m.Name == nameof(Enumerable.Select)
			&& m.GetParameters() is { Length: 2 } parameters
			&& parameters[1].ParameterType.IsGenericType
			&& parameters[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>));

	private static readonly MethodInfo ToListMethod = typeof(Enumerable).GetMethods()
		.Single(m => m.Name == nameof(Enumerable.ToList) && m.GetParameters().Length == 1);

	private static readonly MethodInfo ToArrayMethod = typeof(Enumerable).GetMethods()
		.Single(m => m.Name == nameof(Enumerable.ToArray) && m.GetParameters().Length == 1);

	private static readonly IReadOnlyCollection<string> NoMembers = [];

	/// <summary>
	/// Non-primitive types that a projection nevertheless treats as scalar values rather than as
	/// nested objects to expand member-by-member.
	/// </summary>
	private static readonly HashSet<Type> ScalarValueTypes =
	[
		typeof(string),
		typeof(decimal),
		typeof(DateTime),
		typeof(DateTimeOffset),
		typeof(TimeSpan),
		typeof(Guid),
		typeof(DateOnly),
		typeof(TimeOnly),
	];

	/// <summary>
	/// Projects the source queryable to the destination type using the mapper configuration.
	/// This produces an Expression that can be translated by EF Core to SQL.
	/// Nested complex members and collections of complex members are projected recursively
	/// (element-by-element) whenever a type map exists for them.
	/// </summary>
	public static IQueryable<TDestination> ProjectTo<TDestination>(
		this IQueryable source,
		IConfigurationProvider configurationProvider)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(configurationProvider);

		return ProjectToInternal<TDestination>(source, configurationProvider, NoMembers);
	}

	/// <summary>
	/// Projects the source queryable to the destination type, additionally expanding the specified
	/// members that were configured with <c>ExplicitExpansion()</c>. Members marked with
	/// <c>ExplicitExpansion()</c> are otherwise excluded from the projection.
	/// </summary>
	public static IQueryable<TDestination> ProjectTo<TDestination>(
		this IQueryable source,
		IConfigurationProvider configurationProvider,
		params Expression<Func<TDestination, object?>>[] membersToExpand)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(configurationProvider);
		ArgumentNullException.ThrowIfNull(membersToExpand);

		var names = new HashSet<string>(StringComparer.Ordinal);
		foreach (var member in membersToExpand)
		{
			names.Add(GetExpandedMemberName(member));
		}

		return ProjectToInternal<TDestination>(source, configurationProvider, names);
	}

	private static IQueryable<TDestination> ProjectToInternal<TDestination>(
		IQueryable source,
		IConfigurationProvider configurationProvider,
		IReadOnlyCollection<string> membersToExpand)
	{
		var sourceType = source.ElementType;
		var destType = typeof(TDestination);

		// Build a Select expression: source.Select(s => new TDestination { Prop1 = s.Prop1, ... })
		var selectExpression = BuildMemberInitLambda(sourceType, destType, configurationProvider, [], membersToExpand);

		// Call Queryable.Select with the expression
		var selectMethod = typeof(Queryable)
			.GetMethods()
			.First(m => m.Name == nameof(Queryable.Select) && m.GetParameters().Length == 2)
			.MakeGenericMethod(sourceType, destType);

		var result = selectMethod.Invoke(null, [source, selectExpression])
			?? throw new InvalidOperationException("Failed to create projected queryable.");

		return (IQueryable<TDestination>)result;
	}

	private static string GetExpandedMemberName<TDestination>(Expression<Func<TDestination, object?>> expression)
	{
		var body = expression.Body;
		if (body is UnaryExpression { NodeType: ExpressionType.Convert, Operand: MemberExpression convertMember })
		{
			return convertMember.Member.Name;
		}

		if (body is MemberExpression member)
		{
			return member.Member.Name;
		}

		throw new ArgumentException(
			$"Expression '{expression}' must be a simple member access (e.g. d => d.Entitlements).",
			nameof(expression));
	}

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

	/// <summary>
	/// Attempts to treat the member as a nested complex object or a collection of complex elements.
	/// Returns true when the projector owns the member (whether or not it was expanded); the
	/// <paramref name="binding"/> is null when the member should be left at its default value.
	/// Returns false when the member is a scalar that should be handled by type coercion.
	/// </summary>
	private static bool TryBuildComplexBinding(
		Expression sourceValue,
		Type sourceType,
		Type destType,
		IConfigurationProvider provider,
		IReadOnlyList<(Type Source, Type Dest)> path,
		out Expression? binding)
	{
		binding = null;

		// If the source is directly assignable to the destination (identical type, a covariant
		// IEnumerable, or a base type), no element-wise projection is needed: leave it to the
		// scalar/compatibility path so members that already worked are copied unchanged. This keeps
		// the change strictly additive - only members that previously failed are now projected.
		if (destType.IsAssignableFrom(sourceType))
		{
			return false;
		}

		return TryBuildCollectionBinding(sourceValue, sourceType, destType, provider, path, out binding)
			|| TryBuildNestedObjectBinding(sourceValue, sourceType, destType, provider, path, out binding);
	}

	/// <summary>
	/// Handles a collection of complex elements, projecting it to a <c>Select(...)</c> over an
	/// element-wise member init.
	/// </summary>
	private static bool TryBuildCollectionBinding(
		Expression sourceValue,
		Type sourceType,
		Type destType,
		IConfigurationProvider provider,
		IReadOnlyList<(Type Source, Type Dest)> path,
		out Expression? binding)
	{
		binding = null;

		if (sourceType == typeof(string)
			|| destType == typeof(string)
			|| !TypeMap.TryGetCollectionElementType(sourceType, out var srcElem)
			|| !TypeMap.TryGetCollectionElementType(destType, out var destElem)
			|| IsSimple(destElem))
		{
			return false;
		}

		var elementMap = provider.FindTypeMap(srcElem, destElem);
		if (elementMap is not null && ShouldExpand((srcElem, destElem), elementMap, path))
		{
			binding = BuildCollectionProjection(
				sourceValue, destType, srcElem, destElem, provider, Push(path, (srcElem, destElem)));
		}

		// We own collection-of-complex members even when no element map exists: returning true
		// (with a null binding) keeps the destination collection at its initializer default
		// instead of emitting an InvalidCastException-throwing reference cast.
		return true;
	}

	/// <summary>
	/// Handles a single nested complex object, projecting it to <c>new TDest { ... }</c> guarded
	/// for a null source.
	/// </summary>
	private static bool TryBuildNestedObjectBinding(
		Expression sourceValue,
		Type sourceType,
		Type destType,
		IConfigurationProvider provider,
		IReadOnlyList<(Type Source, Type Dest)> path,
		out Expression? binding)
	{
		binding = null;

		if (IsSimple(sourceType) || IsSimple(destType))
		{
			return false;
		}

		var nestedMap = provider.FindTypeMap(sourceType, destType);
		if (nestedMap is null)
		{
			return false;
		}

		if (ShouldExpand((sourceType, destType), nestedMap, path))
		{
			binding = BuildNestedObjectProjection(
				sourceValue, sourceType, destType, provider, Push(path, (sourceType, destType)));
		}

		return true;
	}

	private static Expression BuildCollectionProjection(
		Expression sourceCollection,
		Type destCollectionType,
		Type srcElem,
		Type destElem,
		IConfigurationProvider provider,
		IReadOnlyList<(Type Source, Type Dest)> path)
	{
		var elementLambda = BuildMemberInitLambda(srcElem, destElem, provider, path, NoMembers);

		// Enumerable.Select(sourceCollection, e => new TDestElem { ... })
		var selectCall = Expression.Call(
			SelectMethod.MakeGenericMethod(srcElem, destElem),
			sourceCollection,
			elementLambda);

		if (destCollectionType.IsArray)
		{
			return Expression.Call(ToArrayMethod.MakeGenericMethod(destElem), selectCall);
		}

		var toListCall = Expression.Call(ToListMethod.MakeGenericMethod(destElem), selectCall);
		var listType = typeof(List<>).MakeGenericType(destElem);

		// List<T> satisfies List<T>, IList<T>, ICollection<T>, IEnumerable<T>, IReadOnly*<T>.
		if (destCollectionType.IsAssignableFrom(listType))
		{
			return toListCall;
		}

		// Concrete collection type with an IEnumerable<T> constructor (e.g. HashSet<T>, Collection<T>).
		var ctor = destCollectionType.GetConstructor([typeof(IEnumerable<>).MakeGenericType(destElem)]);
		return ctor is not null
			? Expression.New(ctor, selectCall)
			: toListCall;
	}

	private static Expression BuildNestedObjectProjection(
		Expression sourceValue,
		Type sourceType,
		Type destType,
		IConfigurationProvider provider,
		IReadOnlyList<(Type Source, Type Dest)> path)
	{
		var elementLambda = BuildMemberInitLambda(sourceType, destType, provider, path, NoMembers);
		var projected = RebindExpression(elementLambda, sourceValue);

		// A non-nullable value-type source can never be null; project directly.
		if (sourceType.IsValueType && Nullable.GetUnderlyingType(sourceType) is null)
		{
			return projected;
		}

		// Reference (or nullable) source: src == null ? default : new TDest { ... }
		return Expression.Condition(
			Expression.Equal(sourceValue, Expression.Constant(null, sourceValue.Type)),
			Expression.Default(destType),
			projected);
	}

	/// <summary>
	/// Decides whether the given (source, destination) pair should be expanded at the current point
	/// in the recursion. Honours a configured MaxDepth and otherwise stops at the first repeat of a
	/// pair to prevent infinite recursion on self-referential maps.
	/// </summary>
	private static bool ShouldExpand(
		(Type Source, Type Dest) pair,
		TypeMap map,
		IReadOnlyList<(Type Source, Type Dest)> path)
	{
		var occurrences = 0;
		foreach (var existing in path)
		{
			if (existing == pair)
			{
				occurrences++;
			}
		}

		return map.MaxDepthValue is int maxDepth
			? occurrences < maxDepth
			: occurrences == 0;
	}

	private static IReadOnlyList<(Type Source, Type Dest)> Push(
		IReadOnlyList<(Type Source, Type Dest)> path,
		(Type Source, Type Dest) pair)
		=> [.. path, pair];

	private static bool IsSimple(Type type)
	{
		var underlying = Nullable.GetUnderlyingType(type) ?? type;
		return underlying.IsPrimitive || underlying.IsEnum || ScalarValueTypes.Contains(underlying);
	}

	private static Expression RebindExpression(LambdaExpression sourceExpression, Expression replacement)
	{
		var oldParam = sourceExpression.Parameters[0];
		return new ParameterReplacer(oldParam, replacement).Visit(sourceExpression.Body);
	}

	private static Expression EnsureTypeCompatibility(Expression expression, Type targetType)
	{
		if (expression.Type == targetType)
		{
			return expression;
		}

		var sourceType = expression.Type;
		var sourceCore = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
		var targetCore = Nullable.GetUnderlyingType(targetType) ?? targetType;

		// Any -> string: use ToString() (handles nullable, numeric, enum, etc.)
		if (targetCore == typeof(string))
		{
			return ConvertToString(expression, sourceType);
		}

		// Nullable<T> -> non-nullable value type: coalesce to default(T) so EF Core
		// generates COALESCE in SQL instead of throwing on NULL materialization
		if (NeedsNullCoalescing(sourceType, sourceCore, targetType, targetCore))
		{
			return CoalesceNullableSource(expression, sourceCore, targetType);
		}

		// An interface target that the source does not implement would compile to a reference cast
		// that throws InvalidCastException at materialization. Leave the member at its default
		// instead (complex collections/objects are handled earlier via TryBuildComplexBinding).
		if (IsUnimplementedInterface(sourceType, targetType))
		{
			return Expression.Default(targetType);
		}

		// Nullable<T> -> T or T -> Nullable<T> where T is the same core type
		// or numeric/enum conversions where Expression.Convert has a CLR operator
		return TryConvert(expression, targetType);
	}

	/// <summary>
	/// A Nullable&lt;T&gt; source feeding a non-nullable value-type target has to be coalesced, or
	/// a NULL row throws on materialization instead of yielding default(T).
	/// </summary>
	private static bool NeedsNullCoalescing(Type sourceType, Type sourceCore, Type targetType, Type targetCore)
		=> sourceCore != sourceType && targetCore == targetType && targetType.IsValueType;

	/// <summary>
	/// An interface target the source type does not implement, which would otherwise compile to a
	/// reference cast that only fails once EF Core materializes the row.
	/// </summary>
	private static bool IsUnimplementedInterface(Type sourceType, Type targetType)
		=> targetType.IsInterface && !targetType.IsAssignableFrom(sourceType);

	private static Expression ConvertToString(Expression expression, Type sourceType)
	{
		if (Nullable.GetUnderlyingType(sourceType) is null)
		{
			return Expression.Call(expression, nameof(object.ToString), Type.EmptyTypes);
		}

		// A nullable source needs the ToString() call guarded, or a NULL materialises as a
		// NullReferenceException: (src.Prop == null) ? null : src.Prop.Value.ToString()
		var hasValue = Expression.Property(expression, "HasValue");
		var value = Expression.Property(expression, "Value");
		var toString = Expression.Call(value, nameof(object.ToString), Type.EmptyTypes);
		return Expression.Condition(hasValue, toString, Expression.Constant(null, typeof(string)));
	}

	private static Expression CoalesceNullableSource(Expression expression, Type sourceCore, Type targetType)
	{
		var coalesced = Expression.Coalesce(expression, Expression.Default(sourceCore));

		// A different value type (e.g. int? -> double) still needs converting after the coalesce.
		return sourceCore == targetType
			? coalesced
			: TryConvert(coalesced, targetType);
	}

	/// <summary>
	/// Converts where a CLR coercion operator exists. Where none does (e.g. string -&gt; double?)
	/// the binding is skipped by falling back to the target type's default.
	/// </summary>
	private static Expression TryConvert(Expression expression, Type targetType)
	{
		try
		{
			return Expression.Convert(expression, targetType);
		}
		catch (InvalidOperationException)
		{
			return Expression.Default(targetType);
		}
	}

	private sealed class ParameterReplacer(ParameterExpression oldParam, Expression replacement) : ExpressionVisitor
	{
		protected override Expression VisitParameter(ParameterExpression node)
			=> node == oldParam ? replacement : base.VisitParameter(node);
	}
}
