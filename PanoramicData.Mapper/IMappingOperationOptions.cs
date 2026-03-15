namespace PanoramicData.Mapper;

/// <summary>
/// Options available when calling Map with inline configuration.
/// </summary>
public interface IMappingOperationOptions<TSource, TDestination>
{
    /// <summary>
    /// Execute an action after the mapping is complete.
    /// </summary>
    void AfterMap(Action<TSource, TDestination> afterFunction);

    /// <summary>
    /// Execute an action before the mapping begins.
    /// </summary>
    void BeforeMap(Action<TSource, TDestination> beforeFunction);

    /// <summary>
    /// Gets contextual items passed through the mapping pipeline.
    /// </summary>
    IDictionary<string, object> Items { get; }
}
