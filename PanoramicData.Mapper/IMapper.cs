namespace PanoramicData.Mapper;

/// <summary>
/// Provides object mapping capabilities.
/// </summary>
public interface IMapper
{
	/// <summary>
	/// Gets the configuration provider for this mapper.
	/// </summary>
	IConfigurationProvider ConfigurationProvider { get; }

	/// <summary>
	/// Maps an object to a new instance of the destination type.
	/// </summary>
	TDestination Map<TDestination>(object source);

	/// <summary>
	/// Maps from source type to a new instance of destination type.
	/// </summary>
	TDestination Map<TSource, TDestination>(TSource source);

	/// <summary>
	/// Maps from source to an existing destination object.
	/// </summary>
	TDestination Map<TSource, TDestination>(TSource source, TDestination destination);

	/// <summary>
	/// Maps an object to a new instance of the destination type using runtime types.
	/// </summary>
	object Map(object source, Type sourceType, Type destinationType);

	/// <summary>
	/// Maps from source to an existing destination object using runtime types.
	/// </summary>
	object Map(object source, object destination, Type sourceType, Type destinationType);
}