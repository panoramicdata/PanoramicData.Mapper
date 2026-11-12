using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class SelfMappingTests
{
	private static Mapper CreateMapper()
	{
		var config = new MapperConfiguration(_ => { });
		return new Mapper(config);
	}

	private static SelfMapEntity NewEmpty() => new() { Id = 0, Name = "", Amount = 0m };

	private static void AssertEntity(SelfMapEntity dest, int id, string name, decimal amount)
	{
		dest.Id.Should().Be(id);
		dest.Name.Should().Be(name);
		dest.Amount.Should().Be(amount);
	}

	private static void AssertGraphItem(DataSourceGraphStoreItem dest, DataSourceGraphStoreItem expected)
	{
		dest.Id.Should().Be(expected.Id);
		dest.CreatedAt.Should().Be(expected.CreatedAt);
		dest.Name.Should().Be(expected.Name);
		dest.Title.Should().Be(expected.Title);
		dest.Width.Should().Be(expected.Width);
		dest.IsActive.Should().Be(expected.IsActive);
	}

	// --- Create-new overloads ---

	[Fact]
	public void MapTDest_SelfMap_CreatesNewInstance()
	{
		var mapper = CreateMapper();
		var source = new SelfMapEntity { Id = 1, Name = "Test", Amount = 42.5m };

		var dest = mapper.Map<SelfMapEntity>(source);

		dest.Should().NotBeSameAs(source);
		AssertEntity(dest, 1, "Test", 42.5m);
	}

	[Fact]
	public void MapTSourceTDest_SelfMap_CreatesNewInstance()
	{
		var mapper = CreateMapper();
		var source = new SelfMapEntity { Id = 2, Name = "Hello", Amount = 100m };

		var dest = mapper.Map<SelfMapEntity, SelfMapEntity>(source);

		dest.Should().NotBeSameAs(source);
		AssertEntity(dest, 2, "Hello", 100m);
	}

	[Fact]
	public void MapUntyped_SelfMap_CreatesNewInstance()
	{
		var mapper = CreateMapper();
		var source = new SelfMapEntity { Id = 3, Name = "Untyped", Amount = 9.99m };

		var dest = mapper.Map<SelfMapEntity, SelfMapEntity>(source);

		dest.Should().NotBeSameAs(source);
		AssertEntity(dest, 3, "Untyped", 9.99m);
	}

	// --- Map-to-existing overloads ---

	[Fact]
	public void MapToExisting_SelfMap_CopiesProperties()
	{
		var mapper = CreateMapper();
		var source = new SelfMapEntity { Id = 10, Name = "Source", Amount = 50m };
		var dest = NewEmpty();

		var result = mapper.Map(source, dest);

		result.Should().BeSameAs(dest);
		AssertEntity(dest, 10, "Source", 50m);
	}

	[Fact]
	public void MapUntypedToExisting_SelfMap_CopiesProperties()
	{
		var mapper = CreateMapper();
		var source = new SelfMapEntity { Id = 20, Name = "Obj", Amount = 1.5m };
		var dest = NewEmpty();

		var result = mapper.Map(source, dest);

		result.Should().BeSameAs(dest);
		AssertEntity(dest, 20, "Obj", 1.5m);
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
		AssertGraphItem(dest, source);
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
		AssertGraphItem(dest, source);
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
		AssertEntity(dest, 7, "Explicit", 99m);
	}

	// --- Runtime-typed overloads ---

	[Fact]
	public void MapRuntimeTypes_SelfMap_CreatesNewInstance()
	{
		var mapper = CreateMapper();
		var source = new SelfMapEntity { Id = 30, Name = "Runtime", Amount = 7.77m };

		var dest = (SelfMapEntity)mapper.Map(source, typeof(SelfMapEntity), typeof(SelfMapEntity));

		dest.Should().NotBeSameAs(source);
		AssertEntity(dest, 30, "Runtime", 7.77m);
	}

	[Fact]
	public void MapRuntimeTypesToExisting_SelfMap_CopiesProperties()
	{
		var mapper = CreateMapper();
		var source = new SelfMapEntity { Id = 40, Name = "RuntimeExisting", Amount = 3.14m };
		var dest = NewEmpty();

		var result = (SelfMapEntity)mapper.Map(source, dest, typeof(SelfMapEntity), typeof(SelfMapEntity));

		result.Should().BeSameAs(dest);
		AssertEntity(dest, 40, "RuntimeExisting", 3.14m);
	}

	// --- Options overload ---

	[Fact]
	public void MapWithOptions_SelfMap_ExecutesBeforeAndAfterActions()
	{
		var mapper = CreateMapper();
		var source = new SelfMapEntity { Id = 50, Name = "Opts", Amount = 1m };
		var beforeCalled = false;
		var afterCalled = false;

		var dest = mapper.Map<SelfMapEntity, SelfMapEntity>(source, opts =>
		{
			opts.BeforeMap((_, _) => beforeCalled = true);
			opts.AfterMap((_, _) => afterCalled = true);
		});

		dest.Should().NotBeSameAs(source);
		dest.Id.Should().Be(50);
		dest.Name.Should().Be("Opts");
		beforeCalled.Should().BeTrue();
		afterCalled.Should().BeTrue();
	}

	private class ExplicitSelfMapProfile : Profile
	{
		public ExplicitSelfMapProfile() => CreateMap<SelfMapEntity, SelfMapEntity>();
	}
}
