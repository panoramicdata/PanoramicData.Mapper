using System.Collections;
using System.Reflection;

namespace PanoramicData.Mapper.Internal;

/// <summary>
/// Reads and writes member values, and coerces a source value into the destination type
/// where the two are not directly assignable.
/// </summary>
public sealed partial class TypeMap
{
	private static Func<object, object?> CreateGetter(PropertyInfo prop)
	{
		return obj => prop.GetValue(obj);
	}

	private static Action<object, object?> CreateSetter(PropertyInfo prop)
	{
		return (obj, value) => prop.SetValue(obj, value);
	}

	private static bool IsAssignableOrConvertible(Type sourceType, Type destType)
		=> IsDirectlyAssignable(sourceType, destType) || IsConvertible(sourceType, destType);

	/// <summary>
	/// Assignable as-is, either straight across or through a Nullable&lt;T&gt; wrapper on either side.
	/// </summary>
	private static bool IsDirectlyAssignable(Type sourceType, Type destType)
	{
		if (destType.IsAssignableFrom(sourceType))
		{
			return true;
		}

		// Handle nullable destination
		var underlyingDest = Nullable.GetUnderlyingType(destType);
		if (underlyingDest is not null && underlyingDest.IsAssignableFrom(sourceType))
		{
			return true;
		}

		// Handle nullable source
		var underlyingSource = Nullable.GetUnderlyingType(sourceType);
		return underlyingSource is not null && destType.IsAssignableFrom(underlyingSource);
	}

	/// <summary>
	/// Conversions the mapper can perform even though the types are not assignable: string to enum
	/// via Enum.Parse, plus the IConvertible pairings - numeric widening/narrowing, enum to and
	/// from integral, primitive to and from string, enum to string.
	/// </summary>
	private static bool IsConvertible(Type sourceType, Type destType)
	{
		var srcCore = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
		var dstCore = Nullable.GetUnderlyingType(destType) ?? destType;

		if (srcCore == typeof(string) && dstCore.IsEnum)
		{
			return true;
		}

		return typeof(IConvertible).IsAssignableFrom(srcCore)
			&& typeof(IConvertible).IsAssignableFrom(dstCore);
	}

	/// <summary>
	/// Converts a source value to the destination type, handling null, enum, numeric,
	/// and string conversions that PropertyInfo.SetValue cannot perform implicitly.
	/// </summary>
	private static object? ConvertValue(object? value, Type destType)
	{
		var dstCore = Nullable.GetUnderlyingType(destType) ?? destType;

		if (value is null)
		{
			return DefaultForNull(destType);
		}

		// Already the right type
		if (dstCore.IsAssignableFrom(value.GetType()))
		{
			return value;
		}

		// Any -> string
		if (dstCore == typeof(string))
		{
			return value.ToString();
		}

		if (dstCore.IsEnum)
		{
			return ConvertToEnum(value, destType, dstCore);
		}

		// Enum -> integral, numeric widening/narrowing, string -> numeric, etc.
		if (value is IConvertible)
		{
			return ChangeType(value, destType, dstCore);
		}

		return value;
	}

	/// <summary>
	/// A null source value keeps its null for a nullable or reference destination. A non-nullable
	/// value type cannot hold null, so it gets default(T) instead.
	/// </summary>
	private static object? DefaultForNull(Type destType)
		=> Nullable.GetUnderlyingType(destType) is not null || !destType.IsValueType
			? null
			: Activator.CreateInstance(destType);

	private static object ConvertToEnum(object value, Type destType, Type dstCore)
	{
		// String -> enum
		if (value is string str)
		{
			if (str.Length > 0 && Enum.TryParse(dstCore, str, out var parsed))
			{
				return parsed;
			}

			// Unparseable string - return default for the enum
			return Activator.CreateInstance(destType) ?? Activator.CreateInstance(dstCore)!;
		}

		// Integral -> enum (Convert.ChangeType cannot handle this)
		return Enum.ToObject(dstCore, value);
	}

	private static object? ChangeType(object value, Type destType, Type dstCore)
	{
		try
		{
			return Convert.ChangeType(value, dstCore);
		}
		catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
		{
			// Conversion failed (e.g. empty string -> int) - return default
			return destType.IsValueType ? Activator.CreateInstance(destType) : null;
		}
	}

	internal static bool TryGetCollectionElementType(Type type, out Type elementType)
	{
		// Arrays
		if (type.IsArray)
		{
			elementType = type.GetElementType()!;
			return true;
		}

		// IEnumerable<T> implemented by the type
		var enumerableInterface = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
			? type
			: type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

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
		// Array destination
		if (destCollectionType.IsArray)
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
