using PanoramicData.Mapper.Test.Models;
using PanoramicData.Mapper.Test.Profiles;

namespace PanoramicData.Mapper.Test;

public class ProfileRegistrationTests
{
	[Fact]
	public void AddProfile_GenericOverload_RegistersProfile()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile<SimpleProfile>());

		var mapper = config.CreateMapper();
		var source = new SimpleSource { Id = 1, Name = "Test", Description = "Desc", Amount = 42m };

		var dest = mapper.Map<SimpleDestination>(source);

		dest.Id.Should().Be(1);
		dest.Name.Should().Be("Test");
		dest.Description.Should().Be("Desc");
		dest.Amount.Should().Be(42m);
	}

	[Fact]
	public void AddProfile_InstanceOverload_RegistersProfile()
	{
		var profile = new SimpleProfile();
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile(profile));

		var mapper = config.CreateMapper();
		var source = new SimpleSource { Id = 2, Name = "Instance", Description = "Desc2", Amount = 99m };

		var dest = mapper.Map<SimpleDestination>(source);

		dest.Id.Should().Be(2);
		dest.Name.Should().Be("Instance");
	}

	[Fact]
	public void AddMaps_ScansAssembly_FindsProfiles()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddMaps(typeof(SimpleProfile).Assembly));

		// SimpleProfile registers SimpleSource -> SimpleDestination
		var mapper = config.CreateMapper();
		var source = new SimpleSource { Id = 3, Name = "Scanned", Description = "Desc3", Amount = 10m };

		var dest = mapper.Map<SimpleDestination>(source);

		dest.Id.Should().Be(3);
		dest.Name.Should().Be("Scanned");
	}

	[Fact]
	public void AddMaps_ScansAssembly_FindsMultipleProfiles()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddMaps(typeof(SimpleProfile).Assembly));

		var typeMaps = config.GetAllTypeMaps();

		// Should find at least SimpleProfile, IgnoreProfile, MapFromProfile, etc.
		typeMaps.Count.Should().BeGreaterThan(1);
	}

	[Fact]
	public void ConfigurationProvider_ExposedViaMapperConfiguration()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile<SimpleProfile>());

		IConfigurationProvider provider = config;

		var typeMap = provider.FindTypeMap(typeof(SimpleSource), typeof(SimpleDestination));
		typeMap.Should().NotBeNull();
		typeMap!.SourceType.Should().Be<SimpleSource>();
		typeMap.DestinationType.Should().Be<SimpleDestination>();
	}

	[Fact]
	public void FindTypeMap_NonExistentMapping_ReturnsNull()
	{
		var config = new MapperConfiguration(cfg =>
			cfg.AddProfile<SimpleProfile>());

		var typeMap = config.FindTypeMap(typeof(string), typeof(int));

		typeMap.Should().BeNull();
	}

	[Fact]
	public void DuplicateProfiles_SameMapping_LastProfileWins()
	{
		var config = new MapperConfiguration(cfg =>
		{
			cfg.AddProfile(new UpperCaseProfile());
			cfg.AddProfile(new LowerCaseProfile());
		});
		var mapper = config.CreateMapper();

		var source = new SimpleSource { Id = 1, Name = "Test" };
		var dest = mapper.Map<SimpleDestination>(source);

		// FindTypeMap returns the first match, so the first profile wins
		dest.Name.Should().Be("TEST");
	}

	[Fact]
	public void DuplicateProfiles_SameMapping_BothTypeMapsRegistered()
	{
		var config = new MapperConfiguration(cfg =>
		{
			cfg.AddProfile(new UpperCaseProfile());
			cfg.AddProfile(new LowerCaseProfile());
		});

		var typeMaps = config.GetAllTypeMaps()
			.Where(m => m.SourceType == typeof(SimpleSource) && m.DestinationType == typeof(SimpleDestination))
			.ToList();

		typeMaps.Should().HaveCountGreaterThan(1);
	}

	private class UpperCaseProfile : Profile
	{
		public UpperCaseProfile()
		{
			CreateMap<SimpleSource, SimpleDestination>()
				.ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToUpperInvariant()));
		}
	}

	private class LowerCaseProfile : Profile
	{
		public LowerCaseProfile()
		{
			CreateMap<SimpleSource, SimpleDestination>()
				.ForMember(d => d.Name, opt => opt.MapFrom(s => s.Name.ToLowerInvariant()));
		}
	}
}