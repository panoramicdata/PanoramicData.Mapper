using System.Linq.Expressions;

namespace PanoramicData.Mapper.Internal;

/// <summary>
/// Represents a custom property mapping from source to destination.
/// </summary>
public sealed class PropertyMapping
{
	/// <summary>
	/// The name of the destination property.
	/// </summary>
	public string DestinationMemberName { get; }

	/// <summary>
	/// The source expression lambda (untyped).
	/// </summary>
	public LambdaExpression? SourceExpression { get; set; }

	public PropertyMapping(string destinationMemberName)
	{
		DestinationMemberName = destinationMemberName;
	}
}