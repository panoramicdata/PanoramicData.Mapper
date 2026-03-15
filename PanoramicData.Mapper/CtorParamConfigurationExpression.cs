using System.Linq.Expressions;

namespace PanoramicData.Mapper;

/// <summary>
/// Configuration options for a constructor parameter mapping.
/// </summary>
internal sealed class CtorParamConfigurationExpression<TSource>(string paramName) : ICtorParamConfigurationExpression<TSource>
{
    internal string ParamName { get; } = paramName;

    internal LambdaExpression? SourceExpression { get; private set; }

    public void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember)
    {
        SourceExpression = sourceMember;
    }
}
