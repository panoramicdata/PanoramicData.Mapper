using System.Linq.Expressions;

namespace PanoramicData.Mapper;

/// <summary>
/// Configuration options for a specific destination member.
/// </summary>
internal sealed class MemberConfigurationExpression<TSource, TDestination, TMember>(string memberName) : IMemberConfigurationExpression<TSource, TDestination, TMember>
{
	internal string MemberName { get; } = memberName;

	internal bool IsIgnored { get; private set; }

	internal LambdaExpression? SourceExpression { get; private set; }

	public void Ignore()
	{
		IsIgnored = true;
	}

	public void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember)
	{
		SourceExpression = sourceMember;
	}
}