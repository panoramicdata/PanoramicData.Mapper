using System.Reflection;

namespace PanoramicData.Mapper;

/// <summary>
/// Extension methods for <see cref="IMappingExpression{TSource, TDestination}"/>.
/// </summary>
public static class MappingExpressionExtensions
{
    /// <summary>
    /// Ignore all destination properties that have an inaccessible (non-public or absent) setter.
    /// This prevents configuration validation from failing on properties that cannot be set.
    /// </summary>
    public static IMappingExpression<TSource, TDestination> IgnoreAllPropertiesWithAnInaccessibleSetter<TSource, TDestination>(
        this IMappingExpression<TSource, TDestination> expression)
    {
        var destProperties = typeof(TDestination).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in destProperties)
        {
            var setter = prop.GetSetMethod(nonPublic: true);
            if (setter is null || !setter.IsPublic)
            {
                expression.ForMember(prop.Name, opt => opt.Ignore());
            }
        }

        return expression;
    }
}
