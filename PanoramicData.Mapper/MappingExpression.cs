using PanoramicData.Mapper.Internal;
using System.Linq.Expressions;

namespace PanoramicData.Mapper;

/// <summary>
/// Fluent configuration for a mapping between source and destination types.
/// </summary>
internal sealed class MappingExpression<TSource, TDestination>(TypeMap typeMap) : IMappingExpression<TSource, TDestination>
{
	public IMappingExpression<TSource, TDestination> ForMember<TMember>(
		Expression<Func<TDestination, TMember>> destinationMember,
		Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions)
	{
		var memberName = GetMemberName(destinationMember);
		var config = new MemberConfigurationExpression<TSource, TDestination, TMember>(memberName);
		memberOptions(config);
		ApplyMemberConfig(config);
		typeMap.ResetCompiledMapper();
		return this;
	}

	public IMappingExpression<TSource, TDestination> ForMember(
		string name,
		Action<IMemberConfigurationExpression<TSource, TDestination, object>> memberOptions)
	{
		var config = new MemberConfigurationExpression<TSource, TDestination, object>(name);
		memberOptions(config);
		ApplyMemberConfig(config);
		typeMap.ResetCompiledMapper();
		return this;
	}

	public IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> afterFunction)
	{
		typeMap.AfterMapActions.Add(afterFunction);
		return this;
	}

	public IMappingExpression<TSource, TDestination> AfterMap<TMappingAction>()
		where TMappingAction : IMappingAction<TSource, TDestination>, new()
	{
		typeMap.AfterMapActionTypes.Add(typeof(TMappingAction));
		return this;
	}

	public IMappingExpression<TSource, TDestination> ForAllMembers(
		Action<IMemberConfigurationExpression<TSource, TDestination, object>> memberOptions)
	{
		// Create a probe to detect what the caller wants
		var config = new MemberConfigurationExpression<TSource, TDestination, object>("__all__");
		memberOptions(config);

		if (config.IsIgnored)
		{
			typeMap.AllMembersIgnored = true;
			typeMap.ResetCompiledMapper();
		}

		return this;
	}

	private static string GetMemberName<TMember>(Expression<Func<TDestination, TMember>> expression)
	{
		if (expression.Body is MemberExpression memberExpression)
		{
			return memberExpression.Member.Name;
		}

		if (expression.Body is UnaryExpression { Operand: MemberExpression unaryMember })
		{
			return unaryMember.Member.Name;
		}

		throw new ArgumentException($"Expression '{expression}' does not refer to a property or field.");
	}

	private void ApplyMemberConfig<TMember>(MemberConfigurationExpression<TSource, TDestination, TMember> config)
	{
		if (config.IsIgnored)
		{
			typeMap.IgnoredMembers.Add(config.MemberName);
			typeMap.PropertyMappings.Remove(config.MemberName);
		}
		else if (config.SourceExpression is not null)
		{
			typeMap.PropertyMappings[config.MemberName] = new PropertyMapping(config.MemberName)
			{
				SourceExpression = config.SourceExpression
			};
		}
	}
}