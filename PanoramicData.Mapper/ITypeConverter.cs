namespace PanoramicData.Mapper;

/// <summary>
/// Custom type converter for full control over the mapping between two types.
/// </summary>
public interface ITypeConverter<in TSource, TDestination>
{
    /// <summary>
    /// Convert the source to destination type.
    /// </summary>
    TDestination Convert(TSource source, TDestination destination, ResolutionContext context);
}
