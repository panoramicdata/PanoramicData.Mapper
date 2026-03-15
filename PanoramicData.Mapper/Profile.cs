using PanoramicData.Mapper.Internal;

namespace PanoramicData.Mapper;

/// <summary>
/// Base class for mapping profiles. Derive from this class to define mappings.
/// </summary>
public abstract class Profile
{
	internal List<TypeMap> TypeMaps { get; } = [];

	internal List<(Type SourceType, Type DestType)> OpenGenericMaps { get; } = [];

	/// <summary>
	/// Create a mapping between the source and destination types.
	/// </summary>
	protected IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
	{
		var typeMap = new TypeMap(typeof(TSource), typeof(TDestination));
		TypeMaps.Add(typeMap);
		return new MappingExpression<TSource, TDestination>(typeMap, RegisterTypeMap);
	}

	/// <summary>
	/// Create a mapping between the source and destination types with a specific member list for validation.
	/// </summary>
	protected IMappingExpression<TSource, TDestination> CreateMap<TSource, TDestination>(MemberList memberList)
	{
		var typeMap = new TypeMap(typeof(TSource), typeof(TDestination))
		{
			MemberListValidation = memberList
		};
		TypeMaps.Add(typeMap);
		return new MappingExpression<TSource, TDestination>(typeMap, RegisterTypeMap);
	}

	/// <summary>
	/// Create a mapping between open generic types (e.g., typeof(Source&lt;&gt;), typeof(Dest&lt;&gt;)).
	/// </summary>
	protected void CreateMap(Type sourceType, Type destinationType)
	{
		if (sourceType.IsGenericTypeDefinition && destinationType.IsGenericTypeDefinition)
		{
			OpenGenericMaps.Add((sourceType, destinationType));
		}
		else
		{
			var typeMap = new TypeMap(sourceType, destinationType);
			TypeMaps.Add(typeMap);
		}
	}

	private void RegisterTypeMap(TypeMap typeMap) => TypeMaps.Add(typeMap);
}