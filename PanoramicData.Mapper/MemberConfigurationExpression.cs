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

	internal Delegate? ConditionDelegate { get; private set; }

	internal Delegate? PreConditionDelegate { get; private set; }

	internal object? NullSubstituteValue { get; private set; }

	internal bool HasNullSubstitute { get; private set; }

	internal Type? ValueResolverType { get; private set; }

	internal object? ValueResolverInstance { get; private set; }

	internal bool UseDestValue { get; private set; }

	public void Ignore()
	{
		IsIgnored = true;
	}

	public void MapFrom<TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceMember)
	{
		SourceExpression = sourceMember;
	}

	public void MapFrom<TValueResolver>() where TValueResolver : IValueResolver<TSource, TDestination, TMember>, new()
	{
		ValueResolverType = typeof(TValueResolver);
	}

	public void MapFrom(IValueResolver<TSource, TDestination, TMember> resolver)
	{
		ValueResolverInstance = resolver;
		ValueResolverType = resolver.GetType();
	}

	public void Condition(Func<TSource, TDestination, TMember, bool> condition)
	{
		ConditionDelegate = condition;
	}

	public void Condition(Func<TSource, bool> condition)
	{
		// Wrap simple condition into the full signature
		ConditionDelegate = new Func<TSource, TDestination, TMember, bool>((src, _, _) => condition(src));
	}

	public void PreCondition(Func<TSource, bool> condition)
	{
		PreConditionDelegate = condition;
	}

	public void NullSubstitute(TMember value)
	{
		NullSubstituteValue = value;
		HasNullSubstitute = true;
	}

	public void UseDestinationValue()
	{
		UseDestValue = true;
	}
}