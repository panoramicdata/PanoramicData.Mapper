namespace PanoramicData.Mapper;

/// <summary>
/// Context information for the current resolution operation.
/// </summary>
public class ResolutionContext
{
	/// <summary>
	/// Gets the mapper instance.
	/// </summary>
	public IMapper? Mapper { get; internal set; }
}