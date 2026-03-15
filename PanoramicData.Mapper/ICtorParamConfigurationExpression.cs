using System.Linq.Expressions;

namespace PanoramicData.Mapper;

/// <summary>
/// Configuration options for a constructor parameter mapping.
/// </summary>
public interface ICtorParamConfigurationExpression<TSource>
{
    /// <summary>
    /// Map this constructor parameter from a source expression.
    /// </summary>
    void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember);
}
