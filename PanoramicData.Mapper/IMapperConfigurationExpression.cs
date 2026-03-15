namespace PanoramicData.Mapper;

/// <summary>
/// Configuration expression for setting up the mapper.
/// </summary>
public interface IMapperConfigurationExpression
{
	/// <summary>
	/// Add mapping profiles from the specified assemblies.
	/// </summary>
	void AddMaps(params System.Reflection.Assembly[] assemblies);

	/// <summary>
	/// Add a specific mapping profile.
	/// </summary>
	void AddProfile<TProfile>() where TProfile : Profile, new();

	/// <summary>
	/// Add a specific mapping profile instance.
	/// </summary>
	void AddProfile(Profile profile);
}