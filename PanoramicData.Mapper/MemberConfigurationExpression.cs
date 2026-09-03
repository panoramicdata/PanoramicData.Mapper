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

	internal bool IsExplicitExpansion { get; private set; }

	/// <summary>
	/// Whether anything beyond the member's name has actually been configured. False for a member
	/// that was named but left with default behaviour, which needs no stored mapping.
	/// </summary>
	internal bool HasConfiguration
		=> SourceExpression is not null
			|| ValueResolverType is not null
			|| ConditionDelegate is not null
			|| PreConditionDelegate is not null
			|| HasNullSubstitute
			|| UseDestValue;

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

	public void Condition(Func<TSource, TDestination, TMember, bool> predicate)
	{
		ConditionDelegate = predicate;
	}

	public void Condition(Func<TSource, bool> predicate)
	{
		// Wrap simple condition into the full signature
		ConditionDelegate = new Func<TSource, TDestination, TMember, bool>((src, _, _) => predicate(src));
	}

	public void PreCondition(Func<TSource, bool> predicate)
	{
		PreConditionDelegate = predicate;
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

	public void ExplicitExpansion()
	{
		IsExplicitExpansion = true;
	}
}