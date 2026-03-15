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
}