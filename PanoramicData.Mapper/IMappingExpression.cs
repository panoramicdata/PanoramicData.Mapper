using System.Linq.Expressions;

namespace PanoramicData.Mapper;

/// <summary>
/// Fluent configuration for a mapping between source and destination types.
/// </summary>
public interface IMappingExpression<TSource, TDestination>
{
	/// <summary>
	/// Configure a specific destination member using a member configuration action.
	/// </summary>
	IMappingExpression<TSource, TDestination> ForMember<TMember>(
		Expression<Func<TDestination, TMember>> destinationMember,
		Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions);

	/// <summary>
	/// Configure a specific destination member by name.
	/// </summary>
	IMappingExpression<TSource, TDestination> ForMember(
		string name,
		Action<IMemberConfigurationExpression<TSource, TDestination, object>> memberOptions);

	/// <summary>
	/// Execute an action after mapping.
	/// </summary>
	IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> afterFunction);

	/// <summary>
	/// Execute a mapping action after mapping, resolved from the DI container or created directly.
	/// </summary>
	IMappingExpression<TSource, TDestination> AfterMap<TMappingAction>()
		where TMappingAction : IMappingAction<TSource, TDestination>, new();

	/// <summary>
	/// Apply a configuration action to all destination members.
	/// </summary>
	IMappingExpression<TSource, TDestination> ForAllMembers(
		Action<IMemberConfigurationExpression<TSource, TDestination, object>> memberOptions);
}