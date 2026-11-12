namespace PanoramicData.Mapper.Internal;

/// <summary>
/// Coerces a source value into the destination type where the two are not directly assignable.
/// </summary>
internal static partial class TypeMapConversion
{
	/// <summary>
	/// Converts a source value to the destination type, handling null, enum, numeric,
	/// and string conversions that PropertyInfo.SetValue cannot perform implicitly.
	/// </summary>
	internal static object? ConvertValue(object? value, Type destType)
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
			if (str.Length > 0 && TryParseEnum(dstCore, str, out var parsed))
			{
				return parsed!;
			}

			// Unparseable string - return default for the enum
			return Activator.CreateInstance(destType) ?? Activator.CreateInstance(dstCore)!;
		}

		// Integral -> enum (Convert.ChangeType cannot handle this)
		return Enum.ToObject(dstCore, value);
	}

	private static bool TryParseEnum(Type enumType, string text, out object? parsed)
	{
#if NET5_0_OR_GREATER
		return Enum.TryParse(enumType, text, out parsed);
#else
		try
		{
			parsed = Enum.Parse(enumType, text);
			return true;
		}
		catch (Exception ex) when (ex is ArgumentException or OverflowException)
		{
			parsed = null;
			return false;
		}
#endif
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
}
