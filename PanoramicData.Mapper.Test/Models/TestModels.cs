namespace PanoramicData.Mapper.Test.Models;

public class SimpleSource
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public DateTime CreatedDate { get; set; }
	public decimal Amount { get; set; }
}

public class SimpleDestination
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public DateTime CreatedDate { get; set; }
	public decimal Amount { get; set; }
}

public class SourceWithExtra
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Extra { get; set; } = string.Empty;
}

public class DestinationWithExtra
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Computed { get; set; } = string.Empty;
}

public class SourceWithNested
{
	public int Id { get; set; }
	public InnerSource Inner { get; set; } = new();
}

public class InnerSource
{
	public string Value { get; set; } = string.Empty;
	public int Number { get; set; }
}

public class FlatDestination
{
	public int Id { get; set; }
	public string InnerValue { get; set; } = string.Empty;
	public int InnerNumber { get; set; }
}

public class DestinationWithIgnoredProps
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Secret { get; set; } = "original";
	public DateTime Timestamp { get; set; }
}

public class DestinationWithIgnoreAttribute
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;

	[Ignore]
	public string Secret { get; set; } = "original";
}

public class CloneableEntity
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public DateTime CreatedDateTimeUtc { get; set; }
	public DateTime LastModifiedDateTimeUtc { get; set; }
	public string Data { get; set; } = string.Empty;
}

public class SourceForTransform
{
	public string ChannelWidth { get; set; } = string.Empty;
	public string Power { get; set; } = string.Empty;
}

public class DestForTransform
{
	public string ChannelWidth { get; set; } = string.Empty;
	public string Power { get; set; } = string.Empty;
}

public class PersonSource
{
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
	public int Age { get; set; }
}

public class PersonDest
{
	public string FullName { get; set; } = string.Empty;
	public int Age { get; set; }
}

public class DestinationWithUnmappedProp
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string ThisPropertyHasNoSource { get; set; } = string.Empty;
}