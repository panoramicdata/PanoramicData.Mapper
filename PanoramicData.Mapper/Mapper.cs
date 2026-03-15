using PanoramicData.Mapper.Internal;
using System.Collections;

namespace PanoramicData.Mapper;

/// <summary>
/// Default implementation of IMapper that uses a MapperConfiguration.
/// </summary>
public sealed class Mapper : IMapper
{
	private readonly MapperConfiguration _configuration;

	/// <summary>
	/// Creates a new Mapper with the given configuration.
	/// </summary>
	public Mapper(MapperConfiguration configuration)
	{
		_configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	}

	/// <inheritdoc />
	public IConfigurationProvider ConfigurationProvider => _configuration;

	/// <inheritdoc />
	public TDestination Map<TDestination>(object source)
	{
		ArgumentNullException.ThrowIfNull(source);

		var sourceType = source.GetType();
		var destType = typeof(TDestination);
		var typeMap = _configuration.FindTypeMap(sourceType, destType);

		if (typeMap is not null)
		{
			return (TDestination)typeMap.Map(source);
		}

		// Try collection mapping
		if (TryMapCollection(source, sourceType, destType, out var collectionResult))
		{
			return (TDestination)collectionResult;
		}

		throw new AutoMapperMappingException(
			$"Missing type map configuration or unsupported mapping. Mapping types: {sourceType.FullName} -> {destType.FullName}");
	}

	/// <inheritdoc />
	public TDestination Map<TSource, TDestination>(TSource source)
	{
		ArgumentNullException.ThrowIfNull(source);

		var typeMap = _configuration.FindTypeMap(typeof(TSource), typeof(TDestination));

		if (typeMap is not null)
		{
			return (TDestination)typeMap.Map(source);
		}

		// Try collection mapping
		if (TryMapCollection(source, typeof(TSource), typeof(TDestination), out var collectionResult))
		{
			return (TDestination)collectionResult;
		}

		throw new AutoMapperMappingException(
			$"Missing type map configuration or unsupported mapping. Mapping types: {typeof(TSource).FullName} -> {typeof(TDestination).FullName}");
	}

	/// <inheritdoc />
	public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(destination);

		var typeMap = _configuration.FindTypeMap(typeof(TSource), typeof(TDestination))
			?? throw new AutoMapperMappingException(
				$"Missing type map configuration or unsupported mapping. Mapping types: {typeof(TSource).FullName} -> {typeof(TDestination).FullName}");

		return (TDestination)typeMap.MapToExisting(source, destination);
	}

	/// <inheritdoc />
	public TDestination Map<TSource, TDestination>(TSource source, Action<IMappingOperationOptions<TSource, TDestination>> opts)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(opts);

		var typeMap = _configuration.FindTypeMap(typeof(TSource), typeof(TDestination));

		if (typeMap is null)
		{
			// Try collection mapping — options not applicable for collections
			if (TryMapCollection(source, typeof(TSource), typeof(TDestination), out var collectionResult))
			{
				return (TDestination)collectionResult;
			}

			throw new AutoMapperMappingException(
				$"Missing type map configuration or unsupported mapping. Mapping types: {typeof(TSource).FullName} -> {typeof(TDestination).FullName}");
		}

		// Collect the user's inline options
		var options = new MappingOperationOptions<TSource, TDestination>();
		opts(options);

		// Create destination, execute inline BeforeMap, then map properties, then execute inline AfterMap
		var destination = (TDestination)typeMap.CreateDestination(source);
		options.ExecuteBeforeMapActions(source, destination);
		typeMap.MapToExisting(source, destination);
		options.ExecuteAfterMapActions(source, destination);

		return destination;
	}

	/// <inheritdoc />
	public object Map(object source, Type sourceType, Type destinationType)
	{
		ArgumentNullException.ThrowIfNull(source);

		var typeMap = _configuration.FindTypeMap(sourceType, destinationType);

		if (typeMap is not null)
		{
			return typeMap.Map(source);
		}

		// Try collection mapping
		if (TryMapCollection(source, sourceType, destinationType, out var collectionResult))
		{
			return collectionResult;
		}

		throw new AutoMapperMappingException(
			$"Missing type map configuration or unsupported mapping. Mapping types: {sourceType.FullName} -> {destinationType.FullName}");
	}

	/// <inheritdoc />
	public object Map(object source, object destination, Type sourceType, Type destinationType)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(destination);

		var typeMap = _configuration.FindTypeMap(sourceType, destinationType)
			?? throw new AutoMapperMappingException(
				$"Missing type map configuration or unsupported mapping. Mapping types: {sourceType.FullName} -> {destinationType.FullName}");

		return typeMap.MapToExisting(source, destination);
	}

	private bool TryMapCollection(object source, Type sourceType, Type destType, out object result)
	{
		result = default!;

		if (!TypeMap.TryGetCollectionElementType(sourceType, out var srcElemType) ||
			!TypeMap.TryGetCollectionElementType(destType, out var destElemType))
		{
			return false;
		}

		var elemTypeMap = _configuration.FindTypeMap(srcElemType, destElemType);
		if (elemTypeMap is null)
		{
			return false;
		}

		result = TypeMap.MapCollection((IEnumerable)source, elemTypeMap, destType, destElemType);
		return true;
	}
}