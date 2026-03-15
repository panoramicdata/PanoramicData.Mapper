namespace PanoramicData.Mapper;

/// <summary>
/// Provides access to the mapper configuration for features like ProjectTo.
/// </summary>
public interface IConfigurationProvider
{
	/// <summary>
	/// Gets the type map for the specified source and destination types.
	/// </summary>
	Internal.TypeMap? FindTypeMap(Type sourceType, Type destinationType);

	/// <summary>
	/// Gets all registered type maps.
	/// </summary>
	IReadOnlyCollection<Internal.TypeMap> GetAllTypeMaps();
}