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
			// Check for [Ignore] attribute
			if (destProp.GetCustomAttribute<IgnoreAttribute>() is not null)
			{
				continue;
			}

			// Check if explicitly ignored in the type map
			if (typeMap is not null && typeMap.AllMembersIgnored)
			{
				continue;
			}

			if (typeMap is not null && typeMap.IgnoredMembers.Contains(destProp.Name))
			{
				continue;
			}

			// ExplicitExpansion: excluded from the projection unless explicitly requested.
			if (typeMap is not null
				&& typeMap.ExplicitExpansionMembers.Contains(destProp.Name)
				&& !membersToExpand.Contains(destProp.Name))
			{
				continue;
			}

			// Check for custom MapFrom expression
			if (typeMap is not null
				&& typeMap.PropertyMappings.TryGetValue(destProp.Name, out var mapping)
				&& mapping.SourceExpression is not null)
			{
				// Rebind the expression to use our source access (parameter or nested member)
				var rebound = RebindExpression(mapping.SourceExpression, sourceAccess);
				AddBinding(bindings, destProp, rebound, rebound.Type, provider, path);
				continue;
			}

			// Convention-based: match by name
			if (sourceProperties.TryGetValue(destProp.Name, out var sourceProp))
			{
				var sourceMember = Expression.Property(sourceAccess, sourceProp);
				AddBinding(bindings, destProp, sourceMember, sourceProp.PropertyType, provider, path);
			}
		}

		return Expression.MemberInit(Expression.New(destType), bindings);
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

		// Collection of complex elements -> projected Select(...).
		if (sourceType != typeof(string)
			&& destType != typeof(string)
			&& TypeMap.TryGetCollectionElementType(sourceType, out var srcElem)
			&& TypeMap.TryGetCollectionElementType(destType, out var destElem)
			&& !IsSimple(destElem))
		{
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

		// Single nested complex object -> projected new TDest { ... }, guarded for null source.
		if (!IsSimple(sourceType) && !IsSimple(destType))
		{
			var nestedMap = provider.FindTypeMap(sourceType, destType);
			if (nestedMap is not null)
			{
				if (ShouldExpand((sourceType, destType), nestedMap, path))
				{
					binding = BuildNestedObjectProjection(
						sourceValue, sourceType, destType, provider, Push(path, (sourceType, destType)));
				}

				return true;
			}
		}

		return false;
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
		return underlying.IsPrimitive
			|| underlying.IsEnum
			|| underlying == typeof(string)
			|| underlying == typeof(decimal)
			|| underlying == typeof(DateTime)
			|| underlying == typeof(DateTimeOffset)
			|| underlying == typeof(TimeSpan)
			|| underlying == typeof(Guid)
			|| underlying == typeof(DateOnly)
			|| underlying == typeof(TimeOnly);
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
			// For nullable source, coalesce to empty string to avoid NullReferenceException
			if (Nullable.GetUnderlyingType(sourceType) is not null)
			{
				// (src.Prop == null) ? null : src.Prop.Value.ToString()
				var hasValue = Expression.Property(expression, "HasValue");
				var value = Expression.Property(expression, "Value");
				var toString = Expression.Call(value, nameof(object.ToString), Type.EmptyTypes);
				return Expression.Condition(hasValue, toString, Expression.Constant(null, typeof(string)));
			}

			return Expression.Call(expression, nameof(object.ToString), Type.EmptyTypes);
		}

		// Nullable<T> -> non-nullable value type: coalesce to default(T) so EF Core
		// generates COALESCE in SQL instead of throwing on NULL materialization
		if (sourceCore != sourceType && targetCore == targetType && targetType.IsValueType)
		{
			var coalesced = Expression.Coalesce(expression, Expression.Default(sourceCore));
			if (sourceCore == targetType)
			{
				return coalesced;
			}

			// Different value type (e.g. int? -> double): coalesce then convert
			try
			{
				return Expression.Convert(coalesced, targetType);
			}
			catch (InvalidOperationException)
			{
				return Expression.Default(targetType);
			}
		}

		// An interface target that the source does not implement would compile to a reference cast
		// that throws InvalidCastException at materialization. Leave the member at its default
		// instead (complex collections/objects are handled earlier via TryBuildComplexBinding).
		if (targetType.IsInterface && !targetType.IsAssignableFrom(sourceType))
		{
			return Expression.Default(targetType);
		}

		// Nullable<T> -> T or T -> Nullable<T> where T is the same core type
		// or numeric/enum conversions where Expression.Convert has a CLR operator
		try
		{
			return Expression.Convert(expression, targetType);
		}
		catch (InvalidOperationException)
		{
			// No coercion operator exists (e.g. string -> double?) - skip this binding
			// by returning a default value expression for the target type
			return Expression.Default(targetType);
		}
	}

	private sealed class ParameterReplacer(ParameterExpression oldParam, Expression replacement) : ExpressionVisitor
	{
		protected override Expression VisitParameter(ParameterExpression node)
			=> node == oldParam ? replacement : base.VisitParameter(node);
	}
}
