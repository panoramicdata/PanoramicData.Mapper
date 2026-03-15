using PanoramicData.Mapper.Test.Models;
using PanoramicData.Mapper.Test.Profiles;

namespace PanoramicData.Mapper.Test;

public class BasicMappingTests
{
	[Fact]
	public void Map_SimpleConvention_MapsAllProperties()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
		var mapper = config.CreateMapper();

		var source = new SimpleSource
		{
			Id = 42,
			Name = "Test",
			Description = "A description",
			CreatedDate = new DateTime(2026, 1, 15),
			Amount = 99.95m
		};

		var dest = mapper.Map<SimpleDestination>(source);

		dest.Id.Should().Be(42);
		dest.Name.Should().Be("Test");
		dest.Description.Should().Be("A description");
		dest.CreatedDate.Should().Be(new DateTime(2026, 1, 15));
		dest.Amount.Should().Be(99.95m);
	}

	[Fact]
	public void Map_WithGenericTypes_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
		var mapper = config.CreateMapper();

		var source = new SimpleSource { Id = 1, Name = "Hello" };
		var dest = mapper.Map<SimpleSource, SimpleDestination>(source);

		dest.Id.Should().Be(1);
		dest.Name.Should().Be("Hello");
	}

	[Fact]
	public void Map_MissingTypeMap_ThrowsAutoMapperMappingException()
	{
		var config = new MapperConfiguration(cfg => { });
		var mapper = config.CreateMapper();

		var source = new SimpleSource { Id = 1 };
		var act = () => mapper.Map<SimpleDestination>(source);

		act.Should().Throw<AutoMapperMappingException>();
	}

	[Fact]
	public void Map_NullSource_ThrowsArgumentNullException()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<SimpleProfile>());
		var mapper = config.CreateMapper();

		var act = () => mapper.Map<SimpleDestination>((object)null!);

		act.Should().Throw<ArgumentNullException>();
	}
}