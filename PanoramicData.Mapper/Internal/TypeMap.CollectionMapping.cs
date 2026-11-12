using System.Collections;

namespace PanoramicData.Mapper.Internal;

/// <summary>
/// Collection element discovery and element-wise collection mapping.
/// </summary>
public sealed partial class TypeMap
{
	private static bool IsGenericEnumerable(Type type)
		=> type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);

	internal static bool TryGetCollectionElementType(Type type, out Type elementType)
	{
		// Arrays
		if (type.IsArray)
		{
			elementType = type.GetElementType()!;
			return true;
		}

		// IEnumerable<T> implemented by the type
		var enumerableInterface = IsGenericEnumerable(type)
			? type
			: type.GetInterfaces().FirstOrDefault(IsGenericEnumerable);

		if (enumerableInterface is not null)
		{
			elementType = enumerableInterface.GetGenericArguments()[0];
			return true;
		}

		elementType = default!;
		return false;
	}

	internal static object MapCollection(IEnumerable source, TypeMap elementTypeMap, Type destCollectionType, Type destElementType)
	{
		return destCollectionType.IsArray
			? MapToArray(source, elementTypeMap, destElementType)
			: MapToList(source, elementTypeMap, destElementType);
	}

	private static Array MapToArray(IEnumerable source, TypeMap elementTypeMap, Type destElementType)
	{
		var items = source.Cast<object>().Select(item => elementTypeMap.Map(item)).ToList();
		var array = Array.CreateInstance(destElementType, items.Count);
		for (var i = 0; i < items.Count; i++)
		{
			array.SetValue(items[i], i);
		}

		return array;
	}

	// List<T> or any interface assignable from List<T>
	private static IList MapToList(IEnumerable source, TypeMap elementTypeMap, Type destElementType)
	{
		var listType = typeof(List<>).MakeGenericType(destElementType);
		if (Activator.CreateInstance(listType) is not IList list)
		{
			throw new InvalidOperationException($"Cannot create a list of {destElementType.FullName}.");
		}

		foreach (var item in source)
		{
			list.Add(elementTypeMap.Map(item));
		}

		return list;
	}
}
