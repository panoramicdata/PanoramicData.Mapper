namespace PanoramicData.Mapper.Test.Models;

// Models for same-type mapping and for members whose names match but whose types do not.

// --- Self-mapping models ---

public class SelfMapEntity
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public decimal Amount { get; set; }
}

public class IdentifiedStoreItem
{
	public int Id { get; set; }
	public DateTime CreatedAt { get; set; }
}

public class DataSourceGraphStoreItem : IdentifiedStoreItem
{
	public required string Name { get; set; }
	public required string Title { get; set; }
	public required int Width { get; set; }
	public bool IsActive { get; set; }
}

// --- Convention mismatch models (string -> numeric/enum by name) ---

public enum ResourceGroupStatusType
{
	Unknown,
	Active,
	Inactive
}

public class StringPropertySource
{
	public string MonitorObjectId { get; set; } = string.Empty;
	public string GroupStatus { get; set; } = string.Empty;
	public string Count { get; set; } = string.Empty;
}

public class MismatchedNumericDestination
{
	public int MonitorObjectId { get; set; }
	public int Count { get; set; }
}

public class MismatchedNullableIntDestination
{
	public int? MonitorObjectId { get; set; }
}

public class MismatchedEnumDestination
{
	public ResourceGroupStatusType GroupStatus { get; set; }
}
