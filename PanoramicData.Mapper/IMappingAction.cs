namespace PanoramicData.Mapper;

/// <summary>
/// Defines an action to execute after mapping.
/// </summary>
public interface IMappingAction<in TSource, in TDestination>
{
	/// <summary>
	/// Execute the mapping action.
	/// </summary>
	void Process(TSource source, TDestination destination, ResolutionContext context);
}