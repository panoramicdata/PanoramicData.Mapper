namespace PanoramicData.Mapper.Configuration.Annotations;

/// <summary>
/// Marks a property to be ignored during mapping.
/// When applied to a destination property, the mapper will skip it.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public sealed class IgnoreAttribute : Attribute
{
}