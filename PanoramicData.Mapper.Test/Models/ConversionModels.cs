namespace PanoramicData.Mapper.Test.Models;

// Models for enum mapping and for implicit type conversion.

// --- Enum mapping models ---

public enum MyStatus
{
	Unknown = 0,
	Active = 1,
	Inactive = 2,
	Deleted = 3
}

public class IntToEnumSource
{
	public int Status { get; set; }
}

public class EnumToIntSource
{
	public MyStatus Status { get; set; }
}

public class EnumDestination
{
	public MyStatus Status { get; set; }
}

public class IntDestination
{
	public int Status { get; set; }
}

public class NullableIntToNullableEnumSource
{
	public int? Status { get; set; }
}

public class NullableEnumDestination
{
	public MyStatus? Status { get; set; }
}

public class IntToNullableEnumSource
{
	public int Status { get; set; }
}

public class NullableIntToEnumSource
{
	public int? Status { get; set; }
}

public class EnumToEnumSource
{
	public MyStatus Status { get; set; }
}

public class EnumToEnumDestination
{
	public MyStatus Status { get; set; }
}

// --- Implicit type conversion models ---

public class LongSource
{
	public long Value { get; set; }
}

public class IntFromLongDestination
{
	public int Value { get; set; }
}

public class IntSource
{
	public int Value { get; set; }
}

public class LongDestination
{
	public long Value { get; set; }
}

public class IntToStringSource
{
	public int Value { get; set; }
}

public class StringFromIntDestination
{
	public string Value { get; set; } = string.Empty;
}

public class LongToStringSource
{
	public long Value { get; set; }
}

public class StringFromLongDestination
{
	public string Value { get; set; } = string.Empty;
}

public class EnumToStringSource
{
	public MyStatus Status { get; set; }
}

public class StringFromEnumDestination
{
	public string Status { get; set; } = string.Empty;
}

public class StringToEnumSource
{
	public string Status { get; set; } = string.Empty;
}

public class EnumFromStringDestination
{
	public MyStatus Status { get; set; }
}

public class NullableIntSource
{
	public int? Value { get; set; }
}

public class IntFromNullableDestination
{
	public int Value { get; set; }
}

public class DoubleSource
{
	public double Value { get; set; }
}

public class IntFromDoubleDestination
{
	public int Value { get; set; }
}

public class IntToDoubleDestination
{
	public double Value { get; set; }
}

public class BoolToStringSource
{
	public bool Active { get; set; }
}

public class StringFromBoolDestination
{
	public string Active { get; set; } = string.Empty;
}

public class StringToBoolSource
{
	public string Active { get; set; } = string.Empty;
}

public class BoolFromStringDestination
{
	public bool Active { get; set; }
}

public class DecimalSource
{
	public decimal Amount { get; set; }
}

public class DoubleFromDecimalDestination
{
	public double Amount { get; set; }
}
