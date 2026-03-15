namespace PanoramicData.Mapper;

/// <summary>
/// Default implementation of <see cref="IMappingOperationOptions{TSource, TDestination}"/>.
/// Collects before/after actions to be executed during the mapping operation.
/// </summary>
internal sealed class MappingOperationOptions<TSource, TDestination> : IMappingOperationOptions<TSource, TDestination>
{
    private readonly List<Action<TSource, TDestination>> _afterMapActions = [];
    private readonly List<Action<TSource, TDestination>> _beforeMapActions = [];
    private readonly Dictionary<string, object> _items = [];

    /// <inheritdoc />
    public void AfterMap(Action<TSource, TDestination> afterFunction)
        => _afterMapActions.Add(afterFunction ?? throw new ArgumentNullException(nameof(afterFunction)));

    /// <inheritdoc />
    public void BeforeMap(Action<TSource, TDestination> beforeFunction)
        => _beforeMapActions.Add(beforeFunction ?? throw new ArgumentNullException(nameof(beforeFunction)));

    /// <inheritdoc />
    public IDictionary<string, object> Items => _items;

    /// <summary>
    /// Execute all registered before-map actions.
    /// </summary>
    internal void ExecuteBeforeMapActions(TSource source, TDestination destination)
    {
        foreach (var action in _beforeMapActions)
        {
            action(source, destination);
        }
    }

    /// <summary>
    /// Execute all registered after-map actions.
    /// </summary>
    internal void ExecuteAfterMapActions(TSource source, TDestination destination)
    {
        foreach (var action in _afterMapActions)
        {
            action(source, destination);
        }
    }
}
