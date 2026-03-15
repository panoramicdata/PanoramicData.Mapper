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

	/// <summary>
	/// Map this member using a custom value resolver.
	/// </summary>
	void MapFrom<TValueResolver>() where TValueResolver : IValueResolver<TSource, TDestination, TMember>, new();

	/// <summary>
	/// Map this member using a custom value resolver instance.
	/// </summary>
	void MapFrom(IValueResolver<TSource, TDestination, TMember> resolver);

	/// <summary>
	/// Only map this member if the condition is met. Evaluated after the value is resolved.
	/// </summary>
	void Condition(Func<TSource, TDestination, TMember, bool> condition);

	/// <summary>
	/// Only map this member if the condition is met (simple source-only overload).
	/// </summary>
	void Condition(Func<TSource, bool> condition);

	/// <summary>
	/// Pre-condition evaluated before resolving the source value. If false, skip this member entirely.
	/// </summary>
	void PreCondition(Func<TSource, bool> condition);

	/// <summary>
	/// Use the specified value when the source value resolves to null.
	/// </summary>
	void NullSubstitute(TMember value);

	/// <summary>
	/// Use the existing destination value instead of creating a new one (useful for collections).
	/// </summary>
	void UseDestinationValue();
}