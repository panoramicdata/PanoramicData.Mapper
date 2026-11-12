using System.Reflection;

namespace PanoramicData.Mapper.Internal;

/// <summary>
/// Reads and writes member values, and coerces a source value into the destination type
/// where the two are not directly assignable.
/// </summary>
internal static partial class TypeMapConversion
{
	internal static Func<object, object?> CreateGetter(PropertyInfo prop)
	{
		return obj => prop.GetValue(obj);
	}

	internal static Action<object, object?> CreateSetter(PropertyInfo prop)
	{
		return (obj, value) => prop.SetValue(obj, value);
	}

	internal static bool IsAssignableOrConvertible(Type sourceType, Type destType)
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
}
