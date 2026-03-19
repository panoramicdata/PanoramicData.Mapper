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

// --- Nested mapping models ---

public class OrderSource
{
	public int Id { get; set; }
	public AddressSource Address { get; set; } = new();
}

public class AddressSource
{
	public string Street { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;
}

public class OrderDest
{
	public int Id { get; set; }
	public AddressDest Address { get; set; } = new();
}

public class AddressDest
{
	public string Street { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;
}

public class OrderWithCollectionSource
{
	public int Id { get; set; }
	public List<LineItemSource> Items { get; set; } = [];
}

public class LineItemSource
{
	public string Product { get; set; } = string.Empty;
	public int Quantity { get; set; }
}

public class OrderWithCollectionDest
{
	public int Id { get; set; }
	public List<LineItemDest> Items { get; set; } = [];
}

public class LineItemDest
{
	public string Product { get; set; } = string.Empty;
	public int Quantity { get; set; }
}

// --- Flattening models ---

public class CustomerSource
{
	public int Id { get; set; }
	public CustomerNameSource Customer { get; set; } = new();
}

public class CustomerNameSource
{
	public string Name { get; set; } = string.Empty;
	public int Age { get; set; }
}

public class FlatCustomerDest
{
	public int Id { get; set; }
	public string CustomerName { get; set; } = string.Empty;
	public int CustomerAge { get; set; }
}

public class GetterSource
{
	public int Id { get; set; }
	private readonly string _total = string.Empty;

	public GetterSource() { }

	public GetterSource(string total) => _total = total;

	public string GetTotal() => _total;
}

public class GetterDest
{
	public int Id { get; set; }
	public string Total { get; set; } = string.Empty;
}

// --- P4 Feature models ---

// ReverseMap
public class ReverseSource
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
}

public class ReverseDest
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
}

// Conditional mapping
public class ConditionalSource
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public int Age { get; set; }
	public bool IsActive { get; set; }
}

public class ConditionalDest
{
	public int Id { get; set; }
	public string Name { get; set; } = "default";
	public int Age { get; set; }
}

// NullSubstitute
public class NullSubSource
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public string? Email { get; set; }
}

public class NullSubDest
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Email { get; set; } = string.Empty;
}

// Value resolver
public class ValueResolverSource
{
	public string FirstName { get; set; } = string.Empty;
	public string LastName { get; set; } = string.Empty;
}

public class ValueResolverDest
{
	public string FullName { get; set; } = string.Empty;
}

// Type converter
public class ConverterSource
{
	public string Value { get; set; } = string.Empty;
	public int Multiplier { get; set; }
}

public class ConverterDest
{
	public string Result { get; set; } = string.Empty;
}

// ConstructUsing
public class ConstructSource
{
	public string First { get; set; } = string.Empty;
	public string Last { get; set; } = string.Empty;
	public int Value { get; set; }
}

public class ConstructDest
{
	public string Combined { get; set; }
	public int Value { get; set; }

	public ConstructDest(string combined)
	{
		Combined = combined;
	}
}

// ForCtorParam
public class CtorParamSource
{
	public string FirstName { get; set; } = string.Empty;
	public int Age { get; set; }
}

public class CtorParamDest
{
	public string Name { get; }
	public int Age { get; }

	public CtorParamDest(string name, int age)
	{
		Name = name;
		Age = age;
	}
}

// ForPath
public class ForPathSource
{
	public string Street { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;
}

public class ForPathDest
{
	public ForPathInner Address { get; set; } = new();
}

public class ForPathInner
{
	public string Street { get; set; } = string.Empty;
	public string City { get; set; } = string.Empty;
}

// Inheritance / Include
public class AnimalSource
{
	public string Name { get; set; } = string.Empty;
	public int Legs { get; set; }
}

public class DogSource : AnimalSource
{
	public string Breed { get; set; } = string.Empty;
}

public class CatSource : AnimalSource
{
	public bool IsIndoor { get; set; }
}

public class AnimalDest
{
	public string Name { get; set; } = string.Empty;
	public int Legs { get; set; }
}

public class DogDest : AnimalDest
{
	public string Breed { get; set; } = string.Empty;
}

public class CatDest : AnimalDest
{
	public bool IsIndoor { get; set; }
}

// MaxDepth (self-referencing)
public class TreeNodeSource
{
	public string Name { get; set; } = string.Empty;
	public TreeNodeSource? Child { get; set; }
}

public class TreeNodeDest
{
	public string Name { get; set; } = string.Empty;
	public TreeNodeDest? Child { get; set; }
}

// Value transformers
public class TransformSource
{
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public int Count { get; set; }
}

public class TransformDest
{
	public string Name { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public int Count { get; set; }
}

// Open generics
public class Wrapper<T>
{
	public T Value { get; set; } = default!;
	public string Label { get; set; } = string.Empty;
}

public class WrapperDto<T>
{
	public T Value { get; set; } = default!;
	public string Label { get; set; } = string.Empty;
}

// UseDestinationValue
public class UseDestValSource
{
	public string Name { get; set; } = string.Empty;
}

public class UseDestValDest
{
	public string Name { get; set; } = string.Empty;
	public string Existing { get; set; } = "keep-me";
}

// BeforeMap
public class BeforeMapSource
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
}

public class BeforeMapDest
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string Tag { get; set; } = string.Empty;
}

public class DeepSource
{
	public int Id { get; set; }
	public Level1Source Order { get; set; } = new();
}

public class Level1Source
{
	public Level2Source Item { get; set; } = new();
}

public class Level2Source
{
	public string Name { get; set; } = string.Empty;
}

public class DeepFlatDest
{
	public int Id { get; set; }
	public string OrderItemName { get; set; } = string.Empty;
}

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