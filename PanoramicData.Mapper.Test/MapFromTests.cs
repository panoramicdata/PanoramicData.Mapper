using PanoramicData.Mapper.Test.Models;
using PanoramicData.Mapper.Test.Profiles;

namespace PanoramicData.Mapper.Test;

public class MapFromTests
{
	[Fact]
	public void MapFrom_NestedProperty_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<MapFromProfile>());
		var mapper = config.CreateMapper();

		var source = new SourceWithNested
		{
			Id = 1,
			Inner = new InnerSource { Value = "hello", Number = 42 }
		};

		var dest = mapper.Map<FlatDestination>(source);

		dest.Id.Should().Be(1);
		dest.InnerValue.Should().Be("hello");
		dest.InnerNumber.Should().Be(42);
	}

	[Fact]
	public void MapFrom_WithTransform_AppliesTransformation()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<MapFromWithTransformProfile>());
		var mapper = config.CreateMapper();

		var source = new SourceForTransform
		{
			ChannelWidth = "40 MHz",
			Power = "20 dBm"
		};

		var dest = mapper.Map<DestForTransform>(source);

		dest.ChannelWidth.Should().Be("40");
		dest.Power.Should().Be("20");
	}

	[Fact]
	public void MapFrom_ComputedExpression_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<MapFromComputedProfile>());
		var mapper = config.CreateMapper();

		var source = new PersonSource
		{
			FirstName = "John",
			LastName = "Doe",
			Age = 30
		};

		var dest = mapper.Map<PersonDest>(source);

		dest.FullName.Should().Be("John Doe");
		dest.Age.Should().Be(30);
	}

	[Fact]
	public void ForMember_StringName_MapsCorrectly()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<StringNameProfile>());
		var mapper = config.CreateMapper();

		var source = new SourceWithExtra { Id = 1, Name = "Test", Extra = "data" };
		var dest = mapper.Map<DestinationWithExtra>(source);

		dest.Id.Should().Be(1);
		dest.Name.Should().Be("Test");
		dest.Computed.Should().Be("data!");
	}
}