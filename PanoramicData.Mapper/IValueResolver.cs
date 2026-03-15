namespace PanoramicData.Mapper;

/// <summary>
/// Custom value resolver for mapping a specific destination member.
/// </summary>
public interface IValueResolver<in TSource, in TDestination, TDestMember>
{
    /// <summary>
    /// Resolve the destination member value.
    /// </summary>
    TDestMember Resolve(TSource source, TDestination destination, TDestMember destMember, ResolutionContext context);
}
