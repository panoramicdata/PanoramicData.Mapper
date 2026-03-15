using PanoramicData.Mapper.Test.Models;
using PanoramicData.Mapper.Test.Profiles;

namespace PanoramicData.Mapper.Test;

public class AfterMapTests
{
	[Fact]
	public void AfterMap_InlineLambda_ExecutesAfterMapping()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<AfterMapProfile>());
		var mapper = config.CreateMapper();

		var source = new CloneableEntity
		{
			Id = 10,
			Name = "Original",
			CreatedDateTimeUtc = DateTime.UtcNow,
			LastModifiedDateTimeUtc = DateTime.UtcNow,
			Data = "some data"
		};

		var dest = mapper.Map<CloneableEntity, CloneableEntity>(source);

		dest.Id.Should().Be(0); // Ignored
		dest.Name.Should().Be("Original - Clone"); // AfterMap modified
		dest.CreatedDateTimeUtc.Should().Be(default); // Ignored
		dest.LastModifiedDateTimeUtc.Should().Be(default); // Ignored
		dest.Data.Should().Be("some data"); // Convention-mapped
	}

	[Fact]
	public void AfterMap_GenericAction_ExecutesAfterMapping()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new AfterMapGenericProfile()));
		var mapper = config.CreateMapper();

		var source = new SimpleSource { Id = 1, Name = "a]very long name that exceeds the limit" };
		var dest = mapper.Map<SimpleDestination>(source);

		dest.Id.Should().Be(1);
		dest.Name.Should().Be("PROCESSED");
	}

	private class TestMappingAction : IMappingAction<SimpleSource, SimpleDestination>
	{
		public void Process(SimpleSource source, SimpleDestination destination, ResolutionContext context)
		{
			destination.Name = "PROCESSED";
		}
	}

	private class AfterMapGenericProfile : Profile
	{
		public AfterMapGenericProfile()
		{
			CreateMap<SimpleSource, SimpleDestination>()
				.AfterMap<TestMappingAction>();
		}
	}
}