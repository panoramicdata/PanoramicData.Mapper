namespace PanoramicData.Mapper;

/// <summary>
/// Specifies which member list to validate during AssertConfigurationIsValid.
/// </summary>
public enum MemberList
{
    /// <summary>
    /// Validate that all writable destination members are mapped (default).
    /// </summary>
    Destination = 0,

    /// <summary>
    /// Validate that all readable source members are mapped to a destination member.
    /// </summary>
    Source = 1,

    /// <summary>
    /// Skip member validation entirely for this map.
    /// </summary>
    None = 2
}
