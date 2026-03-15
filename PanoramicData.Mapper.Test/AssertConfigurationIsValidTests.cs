using PanoramicData.Mapper.Test.Profiles;

namespace PanoramicData.Mapper.Test;

public class AssertConfigurationIsValidTests
{
	[Fact]
	public void AssertConfigurationIsValid_AllMembersMapped_DoesNotThrow()
	{
		// SimpleSource -> SimpleDestination: all properties match by convention
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile<SimpleProfile>());

		var act = () => config.AssertConfigurationIsValid();

		act.Should().NotThrow();
	}

	[Fact]
	public void AssertConfigurationIsValid_IgnoredMembersDoNotCountAsUnmapped()
	{
		// IgnoreProfile maps SimpleSource -> DestinationWithIgnoredProps
		// Secret and Timestamp are ignored, Id and Name are convention-matched
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile<IgnoreProfile>());

		var act = () => config.AssertConfigurationIsValid();

		act.Should().NotThrow();
	}

	[Fact]
	public void AssertConfigurationIsValid_UnmappedMembers_ThrowsAutoMapperConfigurationException()
	{
		// Map SimpleSource to a destination that has an extra property with no matching source
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(new UnmappedProfile()));

		var act = () => config.AssertConfigurationIsValid();

		act.Should().Throw<AutoMapperConfigurationException>();
	}

	[Fact]
	public void AssertConfigurationIsValid_ForAllMembersIgnored_DoesNotThrow()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile<ForAllMembersProfile>());

		var act = () => config.AssertConfigurationIsValid();

		act.Should().NotThrow();
	}

	[Fact]
	public void AssertConfigurationIsValid_MapFromCoversUnmapped_DoesNotThrow()
	{
		// MapFromComputedProfile maps PersonSource -> PersonDest
		// PersonDest.FullName has no matching source property but is covered by MapFrom
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile<MapFromComputedProfile>());

		var act = () => config.AssertConfigurationIsValid();

		act.Should().NotThrow();
	}
}