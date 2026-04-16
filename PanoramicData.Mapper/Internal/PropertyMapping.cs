using System.Linq.Expressions;

namespace PanoramicData.Mapper.Internal;

/// <summary>
/// Represents a custom property mapping from source to destination.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PropertyMapping"/> class.
/// </remarks>
/// <param name="destinationMemberName">The destination member name that this mapping targets.</param>
public sealed class PropertyMapping(string destinationMemberName)
{
	/// <summary>
	/// The name of the destination property.
	/// </summary>
	public string DestinationMemberName { get; } = destinationMemberName;

	/// <summary>
	/// The source expression lambda (untyped).
	/// </summary>
	public LambdaExpression? SourceExpression { get; set; }

	/// <summary>
	/// Condition evaluated after getting the value. If false, skip this member.
	/// Delegate shape: Func&lt;src, dest, member, bool&gt;
	/// </summary>
	public Delegate? Condition { get; set; }

	/// <summary>
	/// Pre-condition evaluated before mapping this member. If false, skip.
	/// Delegate shape: Func&lt;src, bool&gt;
	/// </summary>
	public Delegate? PreCondition { get; set; }

	/// <summary>
	/// Value to substitute when the resolved value is null.
	/// </summary>
	public object? NullSubstitute { get; set; }

	/// <summary>
	/// Whether a null substitute has been explicitly set (to distinguish from a null substitute value).
	/// </summary>
	public bool HasNullSubstitute { get; set; }

	/// <summary>
	/// Type implementing IValueResolver to use for resolving this member.
	/// </summary>
	public Type? ValueResolverType { get; set; }

	/// <summary>
	/// Pre-constructed value resolver instance.
	/// </summary>
	public object? ValueResolverInstance { get; set; }

	/// <summary>
	/// When true, map into the existing destination value instead of replacing it.
	/// </summary>
	public bool UseDestinationValue { get; set; }

	/// <summary>
	/// For ForPath: the chain of property names to navigate to set a nested value.
	/// </summary>
	public string[]? PathSegments { get; set; }
}