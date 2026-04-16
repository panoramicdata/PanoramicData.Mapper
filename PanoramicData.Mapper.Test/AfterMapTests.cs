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

	[Fact]
	public void AfterMap_Lambda_MapToExisting_ExecutesAfterMapping()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new AfterMapLambdaForExistingProfile()));
		var mapper = config.CreateMapper();

		var source = new SimpleSource { Id = 1, Name = "Hello" };
		var dest = new SimpleDestination { Name = "original" };

		mapper.Map(source, dest);

		dest.Id.Should().Be(1);
		dest.Name.Should().Be("Hello-post");
	}

	[Fact]
	public void AfterMap_GenericAction_MapToExisting_ExecutesAfterMapping()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new AfterMapGenericProfile()));
		var mapper = config.CreateMapper();

		var source = new SimpleSource { Id = 2, Name = "World" };
		var dest = new SimpleDestination();

		mapper.Map(source, dest);

		dest.Id.Should().Be(2);
		dest.Name.Should().Be("PROCESSED");
	}

	private class AfterMapLambdaForExistingProfile : Profile
	{
		public AfterMapLambdaForExistingProfile()
		{
			CreateMap<SimpleSource, SimpleDestination>()
				.AfterMap((src, dest) => dest.Name += "-post");
		}
	}
}