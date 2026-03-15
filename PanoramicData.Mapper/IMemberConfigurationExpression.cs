using System.Linq.Expressions;

namespace PanoramicData.Mapper;

/// <summary>
/// Configuration options for a specific destination member.
/// </summary>
public interface IMemberConfigurationExpression<TSource, TDestination, TMember>
{
	/// <summary>
	/// Ignore this member during mapping.
	/// </summary>
	void Ignore();

	/// <summary>
	/// Map this member from a custom source expression.
	/// </summary>
	void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember);
}