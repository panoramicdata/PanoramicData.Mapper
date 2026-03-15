using PanoramicData.Mapper.Internal;

namespace PanoramicData.Mapper;

/// <summary>
/// Base class for mapping profiles. Derive from this class to define mappings.
/// </summary>
public abstract class Profile
{
	internal List<TypeMap> TypeMaps { get; } = [];

	/// <summary>
	/// Create a mapping between the source and destination types.
	/// </summary>
	protected IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
	{
		var typeMap = new TypeMap(typeof(TSource), typeof(TDestination));
		TypeMaps.Add(typeMap);
		return new MappingExpression<TSource, TDestination>(typeMap);
	}
}