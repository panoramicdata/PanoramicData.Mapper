using PanoramicData.Mapper.Internal;
using System.Linq.Expressions;

namespace PanoramicData.Mapper.QueryableExtensions;

/// <summary>
/// Projection of nested complex objects, and the recursion guard.
/// </summary>
public static partial class Extensions
{
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
			|| ScalarValueTypes.Contains(underlying)
			|| underlying.FullName is "System.DateOnly" or "System.TimeOnly";
	}
}
