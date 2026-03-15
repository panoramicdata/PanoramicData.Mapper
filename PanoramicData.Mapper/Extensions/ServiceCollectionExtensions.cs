using PanoramicData.Mapper;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up AutoMapper-compatible services in an IServiceCollection.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Add mapper configuration and IMapper using profile assembly marker types.
	/// Scans the assemblies containing the specified types for Profile implementations.
	/// </summary>
	public static IServiceCollection AddAutoMapper(this IServiceCollection services, params Type[] profileAssemblyMarkerTypes)
	{
		var assemblies = profileAssemblyMarkerTypes
			.Select(t => t.Assembly)
			.Distinct()
			.ToArray();

		return services.AddAutoMapper(assemblies);
	}

	/// <summary>
	/// Add mapper configuration and IMapper using specified assemblies.
	/// </summary>
	public static IServiceCollection AddAutoMapper(this IServiceCollection services, params Assembly[] assemblies)
	{
		var config = new MapperConfiguration(cfg =>
		{
			cfg.AddMaps(assemblies);
		});

		services.AddSingleton<IConfigurationProvider>(config);
		services.AddSingleton(config.CreateMapper());

		return services;
	}

	/// <summary>
	/// Add mapper configuration and IMapper using a configuration action.
	/// </summary>
	public static IServiceCollection AddAutoMapper(this IServiceCollection services, Action<IMapperConfigurationExpression> configAction)
	{
		var config = new MapperConfiguration(configAction);

		services.AddSingleton<IConfigurationProvider>(config);
		services.AddSingleton(config.CreateMapper());

		return services;
	}
}