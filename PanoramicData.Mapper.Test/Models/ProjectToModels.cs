namespace PanoramicData.Mapper.Test.Models;

// Models for the IQueryable projection tests.

// --- ProjectTo type mismatch models ---

public class NullableDoubleEntity
{
	public int Id { get; set; }
	public double? Score { get; set; }
	public string Name { get; set; } = string.Empty;
}

public class StringScoreDestination
{
	public int Id { get; set; }
	public string Score { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
}

// --- ProjectTo nullable-to-non-nullable models ---

public class NullablePortEntity
{
	public int Id { get; set; }
	public double? TrafficSentKbps { get; set; }
	public int? ClientCount { get; set; }
	public bool? IsOnline { get; set; }
}

public class NonNullablePortDto
{
	public int Id { get; set; }
	public double TrafficSentKbps { get; set; }
	public int ClientCount { get; set; }
	public bool IsOnline { get; set; }
}

// Interface collection property models
public class OrderSourceWithItems
{
	public int Id { get; set; }
	public List<LineItemSource> Items { get; set; } = [];
}

public class OrderDestWithIList
{
	public int Id { get; set; }
	public IList<LineItemDest> Items { get; set; } = [];
}

public class OrderDestWithICollection
{
	public int Id { get; set; }
	public ICollection<LineItemDest> Items { get; set; } = [];
}

public class OrderDestWithIEnumerable
{
	public int Id { get; set; }
	public IEnumerable<LineItemDest> Items { get; set; } = [];
}

// --- ProjectTo nested child-collection projection models (MS-24516) ---
// Mirror DataMagic's SubscriptionModel -> SubscriptionDto: a parent EF entity with a child
// navigation collection. A naive ProjectTo assigns the EF navigation collection straight to the
// DTO collection property, which throws InvalidCastException at materialisation because the source
// element type (entity) is not the destination element type (DTO). These models reproduce that
// shape so ProjectTo can be proven to project the nested collection element-by-element.

public class ProjParentEntity
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public ICollection<ProjChildEntity> Children { get; set; } = [];
}

public class ProjChildEntity
{
	public int Id { get; set; }
	public int ProjParentEntityId { get; set; }
	public string Sku { get; set; } = string.Empty;
	public int Seats { get; set; }
}

public class ProjChildDto
{
	public int Id { get; set; }
	public string Sku { get; set; } = string.Empty;
	public int Seats { get; set; }
}

// Destination with an interface collection (the real SubscriptionDto.Entitlements shape).
public class ProjParentDto
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public ICollection<ProjChildDto> Children { get; set; } = [];
}

// Destination with a concrete List collection.
public class ProjParentListDto
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public List<ProjChildDto> Children { get; set; } = [];
}

// Destination with an array collection.
public class ProjParentArrayDto
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public ProjChildDto[] Children { get; set; } = [];
}

// Destination with an IEnumerable collection.
public class ProjParentEnumerableDto
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public IEnumerable<ProjChildDto> Children { get; set; } = [];
}

// Single nested reference-navigation projection.
public class ProjOrderEntity
{
	public int Id { get; set; }
	public string Customer { get; set; } = string.Empty;
	public int? AddressId { get; set; }
	public ProjAddressEntity? Address { get; set; }
}

public class ProjAddressEntity
{
	public int Id { get; set; }
	public string Street { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;
}

public class ProjOrderDto
{
	public int Id { get; set; }
	public string Customer { get; set; } = string.Empty;
	public ProjAddressDto? Address { get; set; }
}

public class ProjAddressDto
{
	public int Id { get; set; }
	public string Street { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;
}

// Self-referencing entity for ProjectTo cycle/recursion-guard tests.
public class ProjTreeEntity
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public int? ChildId { get; set; }
	public ProjTreeEntity? Child { get; set; }
}

public class ProjTreeDto
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public ProjTreeDto? Child { get; set; }
}

// Backward-compatibility models: the destination reuses the SAME complex type as the source (no
// element map registered) and a collection of primitives. These must continue to be copied straight
// through by ProjectTo, exactly as before nested projection was added.
public class PassThroughTag
{
	public string Label { get; set; } = string.Empty;
}

public class PassThroughSource
{
	public int Id { get; set; }
	public PassThroughTag? Marker { get; set; }
	public List<PassThroughTag> Tags { get; set; } = [];
	public List<string> Notes { get; set; } = [];
}

public class PassThroughDest
{
	public int Id { get; set; }
	public PassThroughTag? Marker { get; set; }
	public List<PassThroughTag> Tags { get; set; } = [];
	public List<string> Notes { get; set; } = [];
}
