namespace PanoramicData.Mapper;

/// <summary>
/// Implementation of source member configuration that tracks DoNotValidate calls.
/// </summary>
internal sealed class SourceMemberConfigurationExpression : ISourceMemberConfigurationExpression
{
    internal bool IsDoNotValidate { get; private set; }

    public void DoNotValidate() => IsDoNotValidate = true;
}
