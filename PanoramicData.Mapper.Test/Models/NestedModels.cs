namespace PanoramicData.Mapper.Test.Models;

// Models for nested mapping and for convention-based flattening.

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
