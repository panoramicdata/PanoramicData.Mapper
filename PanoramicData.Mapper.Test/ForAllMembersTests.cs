using PanoramicData.Mapper.Test.Models;
using PanoramicData.Mapper.Test.Profiles;

namespace PanoramicData.Mapper.Test;

public class ForAllMembersTests
{
	[Fact]
	public void ForAllMembers_Ignore_SkipsAllConventionMapping()
	{
		var config = new MapperConfiguration(cfg => cfg.AddProfile<ForAllMembersProfile>());
		var mapper = config.CreateMapper();

		var source = new SimpleSource
		{
			Id = 99,
			Name = "Test",
			Description = "Desc",
			CreatedDate = DateTime.UtcNow,
			Amount = 50m
		};

		var dest = mapper.Map<SimpleDestination>(source);

		// ForAllMembers(opt => opt.Ignore()) skips convention mapping
		// but AfterMap still runs
		dest.Id.Should().Be(0);
		dest.Name.Should().Be("Test"); // Set by AfterMap
		dest.Description.Should().Be("Desc"); // Set by AfterMap
		dest.Amount.Should().Be(0m);
	}

	[Fact]
	public void ForAllMembers_Ignore_WithoutAfterMap_AllPropertiesRemainDefault()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new IgnoreAllNoAfterMapProfile()));
		var mapper = config.CreateMapper();

		var source = new SimpleSource
		{
			Id = 99,
			Name = "Test",
			Description = "Desc",
			CreatedDate = new DateTime(2025, 6, 1),
			Amount = 50m
		};

		var dest = mapper.Map<SimpleDestination>(source);

		dest.Id.Should().Be(0);
		dest.Name.Should().Be(string.Empty);
		dest.Description.Should().Be(string.Empty);
		dest.CreatedDate.Should().Be(default);
		dest.Amount.Should().Be(0m);
	}

	[Fact]
	public void ForAllMembers_Ignore_MapToExisting_PreservesExistingValues()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new IgnoreAllNoAfterMapProfile()));
		var mapper = config.CreateMapper();

		var source = new SimpleSource { Id = 99, Name = "New", Amount = 50m };
		var dest = new SimpleDestination { Id = 1, Name = "Original", Amount = 10m };

		mapper.Map(source, dest);

		dest.Id.Should().Be(1);
		dest.Name.Should().Be("Original");
		dest.Amount.Should().Be(10m);
	}

	private class IgnoreAllNoAfterMapProfile : Profile
	{
		public IgnoreAllNoAfterMapProfile()
		{
			CreateMap<SimpleSource, SimpleDestination>()
				.ForAllMembers(opt => opt.Ignore());
		}
	}
}