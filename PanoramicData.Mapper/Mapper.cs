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
		var typeMap = _configuration.FindTypeMap(sourceType, destType)
			?? throw new AutoMapperMappingException(
				$"Missing type map configuration or unsupported mapping. Mapping types: {sourceType.FullName} -> {destType.FullName}");

		return (TDestination)typeMap.Map(source);
	}

	/// <inheritdoc />
	public TDestination Map<TSource, TDestination>(TSource source)
	{
		ArgumentNullException.ThrowIfNull(source);

		var typeMap = _configuration.FindTypeMap(typeof(TSource), typeof(TDestination))
			?? throw new AutoMapperMappingException(
				$"Missing type map configuration or unsupported mapping. Mapping types: {typeof(TSource).FullName} -> {typeof(TDestination).FullName}");

		return (TDestination)typeMap.Map(source);
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
	public object Map(object source, Type sourceType, Type destinationType)
	{
		ArgumentNullException.ThrowIfNull(source);

		var typeMap = _configuration.FindTypeMap(sourceType, destinationType)
			?? throw new AutoMapperMappingException(
				$"Missing type map configuration or unsupported mapping. Mapping types: {sourceType.FullName} -> {destinationType.FullName}");

		return typeMap.Map(source);
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
}