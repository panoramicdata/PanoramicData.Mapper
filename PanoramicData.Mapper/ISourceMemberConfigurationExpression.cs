namespace PanoramicData.Mapper;

/// <summary>
/// Configuration options for source member validation.
/// </summary>
public interface ISourceMemberConfigurationExpression
{
    /// <summary>
    /// Exclude this source member from validation when using MemberList.Source.
    /// </summary>
    void DoNotValidate();
}
