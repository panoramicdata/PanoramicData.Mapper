using PanoramicData.Mapper.Internal;
using System.Linq.Expressions;

namespace PanoramicData.Mapper.QueryableExtensions;

/// <summary>
/// Projection of collections of complex elements.
/// </summary>
public static partial class Extensions
{
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

		return ShapeCollection(selectCall, destCollectionType, destElem);
	}

	/// <summary>
	/// Wraps a projected element sequence in the shape the destination collection type needs.
	/// </summary>
	private static Expression ShapeCollection(Expression selectCall, Type destCollectionType, Type destElem)
	{
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
}
