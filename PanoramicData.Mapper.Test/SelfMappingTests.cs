using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class SelfMappingTests
{
	private static Mapper CreateMapper()
	{
		var config = new MapperConfiguration(_ => { });
		return new Mapper(config);
	}

	// --- Create-new overloads ---

	[Fact]
	public void MapTDest_SelfMap_CreatesNewInstance()
	{
		var mapper = CreateMapper();
		var source = new SelfMapEntity { Id = 1, Name = "Test", Amount = 42.5m };

		var dest = mapper.Map<SelfMapEntity>(source);

		dest.Should().NotBeSameAs(source);
		dest.Id.Should().Be(1);
		dest.Name.Should().Be("Test");
		dest.Amount.Should().Be(42.5m);
	}

	[Fact]
	public void MapTSourceTDest_SelfMap_CreatesNewInstance()
	{
		var mapper = CreateMapper();
		var source = new SelfMapEntity { Id = 2, Name = "Hello", Amount = 100m };

		var dest = mapper.Map<SelfMapEntity, SelfMapEntity>(source);

		dest.Should().NotBeSameAs(source);
		dest.Id.Should().Be(2);
		dest.Name.Should().Be("Hello");
		dest.Amount.Should().Be(100m);
	}

	[Fact]
	public void MapUntyped_SelfMap_CreatesNewInstance()
	{
		var mapper = CreateMapper();
		var source = new SelfMapEntity { Id = 3, Name = "Untyped", Amount = 9.99m };

		var dest = mapper.Map<SelfMapEntity, SelfMapEntity>(source);

		dest.Should().NotBeSameAs(source);
		dest.Id.Should().Be(3);
		dest.Name.Should().Be("Untyped");
		dest.Amount.Should().Be(9.99m);
	}

	// --- Map-to-existing overloads ---

	[Fact]
	public void MapToExisting_SelfMap_CopiesProperties()
	{
		var mapper = CreateMapper();
		var source = new SelfMapEntity { Id = 10, Name = "Source", Amount = 50m };
		var dest = new SelfMapEntity { Id = 0, Name = "", Amount = 0m };

		var result = mapper.Map(source, dest);

		result.Should().BeSameAs(dest);
		dest.Id.Should().Be(10);
		dest.Name.Should().Be("Source");
		dest.Amount.Should().Be(50m);
	}

	[Fact]
	public void MapUntypedToExisting_SelfMap_CopiesProperties()
	{
		var mapper = CreateMapper();
		var source = new SelfMapEntity { Id = 20, Name = "Obj", Amount = 1.5m };
		var dest = new SelfMapEntity { Id = 0, Name = "", Amount = 0m };

		var result = mapper.Map(source, dest);

		result.Should().BeSameAs(dest);
		dest.Id.Should().Be(20);
		dest.Name.Should().Be("Obj");
		dest.Amount.Should().Be(1.5m);
	}

	// --- Required properties and inheritance ---

	[Fact]
	public void MapTDest_SelfMap_WithRequiredAndInheritance_MapsCorrectly()
	{
		var mapper = CreateMapper();
		var source = new DataSourceGraphStoreItem
		{
			Id = 5,
			CreatedAt = new DateTime(2025, 1, 1),
			Name = "Graph1",
			Title = "My Graph",
			Width = 800,
			IsActive = true
		};

		var dest = mapper.Map<DataSourceGraphStoreItem>(source);

		dest.Should().NotBeSameAs(source);
		dest.Id.Should().Be(5);
		dest.CreatedAt.Should().Be(new DateTime(2025, 1, 1));
		dest.Name.Should().Be("Graph1");
		dest.Title.Should().Be("My Graph");
		dest.Width.Should().Be(800);
		dest.IsActive.Should().BeTrue();
	}

	[Fact]
	public void MapToExisting_SelfMap_WithRequiredAndInheritance_CopiesAll()
	{
		var mapper = CreateMapper();
		var source = new DataSourceGraphStoreItem
		{
			Id = 6,
			CreatedAt = new DateTime(2024, 6, 15),
			Name = "Updated",
			Title = "Updated Title",
			Width = 1024,
			IsActive = false
		};
		var dest = new DataSourceGraphStoreItem
		{
			Id = 0,
			Name = "",
			Title = "",
			Width = 0
		};

		var result = mapper.Map(source, dest);

		result.Should().BeSameAs(dest);
		dest.Id.Should().Be(6);
		dest.CreatedAt.Should().Be(new DateTime(2024, 6, 15));
		dest.Name.Should().Be("Updated");
		dest.Title.Should().Be("Updated Title");
		dest.Width.Should().Be(1024);
		dest.IsActive.Should().BeFalse();
	}

	// --- Explicit CreateMap still takes precedence ---

	[Fact]
	public void MapTDest_WithExplicitSelfMap_UsesExplicitMap()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new ExplicitSelfMapProfile()));
		var mapper = config.CreateMapper();

		var source = new SelfMapEntity { Id = 7, Name = "Explicit", Amount = 99m };

		var dest = mapper.Map<SelfMapEntity>(source);

		dest.Should().NotBeSameAs(source);
		dest.Id.Should().Be(7);
		dest.Name.Should().Be("Explicit");
		dest.Amount.Should().Be(99m);
	}

	private class ExplicitSelfMapProfile : Profile
	{
		public ExplicitSelfMapProfile() => CreateMap<SelfMapEntity, SelfMapEntity>();
	}
}
