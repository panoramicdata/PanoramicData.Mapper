namespace PanoramicData.Mapper.Test;

/// <summary>
/// Builds the mappers the tests arrange against, so a test that needs nothing more than "a mapper
/// with this profile" does not repeat the configuration ceremony.
/// </summary>
internal static class MapperFactory
{
	/// <summary>
	/// A mapper configured with a single profile.
	/// </summary>
	internal static IMapper Create<TProfile>() where TProfile : Profile, new()
		=> new MapperConfiguration(cfg => cfg.AddProfile<TProfile>()).CreateMapper();

	/// <summary>
	/// A mapper with no type maps at all, for the cases that assert a missing map is reported
	/// rather than quietly ignored.
	/// </summary>
	internal static IMapper CreateWithNoMaps()
		=> new MapperConfiguration(_ => { }).CreateMapper();
}
