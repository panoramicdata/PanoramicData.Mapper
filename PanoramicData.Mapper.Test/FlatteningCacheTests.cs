using PanoramicData.Mapper.Test.Models;

namespace PanoramicData.Mapper.Test;

public class FlatteningCacheTests
{
	private static (IMapper Mapper, Internal.TypeMap TypeMap, MapperConfiguration Config) Create()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new FlattenCacheProfile()));
		var typeMap = config.FindTypeMap(typeof(CustomerSource), typeof(FlatCustomerDest))!;
		return (config.CreateMapper(), typeMap, config);
	}

	private static CustomerSource NewSource(int id, string name, int age)
		=> new() { Id = id, Customer = new CustomerNameSource { Name = name, Age = age } };

	[Fact]
	public void Map_RepeatedMapping_ResolvesEachFlattenedMemberOnce()
	{
		var (mapper, typeMap, _) = Create();

		for (var i = 0; i < 5; i++)
		{
			mapper.Map<FlatCustomerDest>(NewSource(i, "n" + i, i));
		}

		typeMap.FlatteningCacheMisses.Should().Be(2);
	}

	[Fact]
	public void Map_RepeatedMapping_ProducesIdenticalResultsToTheFirstMapping()
	{
		var (mapper, _, _) = Create();
		var source = NewSource(7, "Alice", 30);

		var first = mapper.Map<FlatCustomerDest>(source);
		var second = mapper.Map<FlatCustomerDest>(source);

		second.Should().BeEquivalentTo(first);
		second.CustomerName.Should().Be("Alice");
		second.CustomerAge.Should().Be(30);
	}

	[Fact]
	public void AssertConfigurationIsValid_AfterMapping_ReusesCachedAccessors()
	{
		var (mapper, typeMap, config) = Create();
		mapper.Map<FlatCustomerDest>(NewSource(1, "a", 1));
		var missesBefore = typeMap.FlatteningCacheMisses;

		config.AssertConfigurationIsValid();

		typeMap.FlatteningCacheMisses.Should().Be(missesBefore);
		typeMap.FlatteningCacheLookups.Should().BeGreaterThan(missesBefore);
	}

	[Fact]
	public void Map_ConcurrentMapping_ResolvesEachFlattenedMemberOnce()
	{
		var (mapper, typeMap, _) = Create();

		Parallel.For(0, 200, i =>
		{
			var dest = mapper.Map<FlatCustomerDest>(NewSource(i, "n" + i, i));
			dest.CustomerAge.Should().Be(i);
		});

		typeMap.FlatteningCacheMisses.Should().Be(2);
	}

	[Fact]
	public void Map_UnresolvableFlattenedMember_IsCachedAsAMiss()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile(new UnresolvableProfile()));
		var typeMap = config.FindTypeMap(typeof(CustomerSource), typeof(UnresolvableDest))!;
		var mapper = config.CreateMapper();

		mapper.Map<UnresolvableDest>(NewSource(1, "a", 1));
		mapper.Map<UnresolvableDest>(NewSource(2, "b", 2));

		typeMap.FlatteningCacheMisses.Should().Be(1);
	}

	private sealed class UnresolvableDest
	{
		public int Id { get; set; }
		public string NoSuchThing { get; set; } = string.Empty;
	}

	private sealed class FlattenCacheProfile : Profile
	{
		public FlattenCacheProfile() => CreateMap<CustomerSource, FlatCustomerDest>();
	}

	private sealed class UnresolvableProfile : Profile
	{
		public UnresolvableProfile() => CreateMap<CustomerSource, UnresolvableDest>();
	}
}
