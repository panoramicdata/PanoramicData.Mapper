namespace PanoramicData.Mapper.Test.Models;

// Models for the individually configured mapping features - conditions, resolvers,
// converters, before/after map actions, ForPath, MaxDepth and inheritance.

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

public class ConstructDest(string combined)
{
	public string Combined { get; set; } = combined;
	public int Value { get; set; }
}

// ForCtorParam
public class CtorParamSource
{
	public string FirstName { get; set; } = string.Empty;
	public int Age { get; set; }
}

public class CtorParamDest(string name, int age)
{
	public string Name { get; } = name;
	public int Age { get; } = age;
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

// ForPath - deep nesting (3 levels)
public class DeepPathSource
{
	public string ZipCode { get; set; } = string.Empty;
	public string Country { get; set; } = string.Empty;
}

public class DeepPathDest
{
	public DeepPathLevel1 Location { get; set; } = new();
}

public class DeepPathLevel1
{
	public DeepPathLevel2 Region { get; set; } = new();
}

public class DeepPathLevel2
{
	public string ZipCode { get; set; } = string.Empty;
	public string Country { get; set; } = string.Empty;
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
