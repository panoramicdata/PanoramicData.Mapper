using Microsoft.Extensions.DependencyInjection;
using PanoramicData.Mapper.Test.Models;
using PanoramicData.Mapper.Test.Profiles;

namespace PanoramicData.Mapper.Test;

public class DependencyInjectionTests
{
	[Fact]
	public void AddAutoMapper_WithMarkerTypes_RegistersIMapperAndConfigurationProvider()
	{
		var services = new ServiceCollection();
		services.AddAutoMapper(typeof(SimpleProfile));

		var provider = services.BuildServiceProvider();

		var mapper = provider.GetService<IMapper>();
		var configProvider = provider.GetService<IConfigurationProvider>();

		mapper.Should().NotBeNull();
		configProvider.Should().NotBeNull();
	}

	[Fact]
	public void AddAutoMapper_WithMarkerTypes_MapperWorksCorrectly()
	{
		var services = new ServiceCollection();
		services.AddAutoMapper(typeof(SimpleProfile));

		var provider = services.BuildServiceProvider();
		var mapper = provider.GetRequiredService<IMapper>();

		var source = new SimpleSource { Id = 1, Name = "DI Test", Description = "Desc", Amount = 5m };
		var dest = mapper.Map<SimpleDestination>(source);

		dest.Id.Should().Be(1);
		dest.Name.Should().Be("DI Test");
	}

	[Fact]
	public void AddAutoMapper_WithAssembly_RegistersServicesAsSingletons()
	{
		var services = new ServiceCollection();
		services.AddAutoMapper(typeof(SimpleProfile).Assembly);

		var provider = services.BuildServiceProvider();

		var mapper1 = provider.GetRequiredService<IMapper>();
		var mapper2 = provider.GetRequiredService<IMapper>();
		var config1 = provider.GetRequiredService<IConfigurationProvider>();
		var config2 = provider.GetRequiredService<IConfigurationProvider>();

		mapper1.Should().BeSameAs(mapper2);
		config1.Should().BeSameAs(config2);
	}

	[Fact]
	public void AddAutoMapper_WithAssembly_ScansAndRegistersProfiles()
	{
		var services = new ServiceCollection();
		services.AddAutoMapper(typeof(SimpleProfile).Assembly);

		var provider = services.BuildServiceProvider();
		var configProvider = provider.GetRequiredService<IConfigurationProvider>();

		// SimpleProfile should have been found via assembly scanning
		var typeMap = configProvider.FindTypeMap(typeof(SimpleSource), typeof(SimpleDestination));
		typeMap.Should().NotBeNull();
	}

	[Fact]
	public void AddAutoMapper_WithConfigurationAction_RegistersServices()
	{
		var services = new ServiceCollection();
		services.AddAutoMapper(cfg =>
			cfg.AddProfile<SimpleProfile>());

		var provider = services.BuildServiceProvider();

		var mapper = provider.GetRequiredService<IMapper>();
		var configProvider = provider.GetRequiredService<IConfigurationProvider>();

		mapper.Should().NotBeNull();
		configProvider.Should().NotBeNull();

		var source = new SimpleSource { Id = 10, Name = "ActionTest", Description = "D", Amount = 1m };
		var dest = mapper.Map<SimpleDestination>(source);

		dest.Id.Should().Be(10);
		dest.Name.Should().Be("ActionTest");
	}
}