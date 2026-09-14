using PanoramicData.Mapper.Test.Models;
using PanoramicData.Mapper.Test.Profiles;

namespace PanoramicData.Mapper.Test;

public class MapFromTests
{
	private static IMapper CreateMapper<TProfile>()
		where TProfile : Profile, new()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<TProfile>());
		return config.CreateMapper();
	}

	[Fact]
	public void MapFrom_NestedProperty_MapsCorrectly()
	{
		var mapper = CreateMapper<MapFromProfile>();

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
		var mapper = CreateMapper<MapFromWithTransformProfile>();

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
		var mapper = CreateMapper<MapFromComputedProfile>();

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
		var mapper = CreateMapper<StringNameProfile>();

		var source = new SourceWithExtra { Id = 1, Name = "Test", Extra = "data" };
		var dest = mapper.Map<DestinationWithExtra>(source);

		dest.Id.Should().Be(1);
		dest.Name.Should().Be("Test");
		dest.Computed.Should().Be("data!");
	}
}