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
	/// Configure a nested destination member via a property path expression.
	/// </summary>
	IMappingExpression<TSource, TDestination> ForPath<TMember>(
		Expression<Func<TDestination, TMember>> destinationPath,
		Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions);

	/// <summary>
	/// Configure a constructor parameter mapping.
	/// </summary>
	IMappingExpression<TSource, TDestination> ForCtorParam(
		string ctorParamName,
		Action<ICtorParamConfigurationExpression<TSource>> paramOptions);

	/// <summary>
	/// Execute an action before mapping.
	/// </summary>
	IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination> beforeFunction);

	/// <summary>
	/// Execute a mapping action before mapping, resolved from the DI container or created directly.
	/// </summary>
	IMappingExpression<TSource, TDestination> BeforeMap<TMappingAction>()
		where TMappingAction : IMappingAction<TSource, TDestination>, new();

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

	/// <summary>
	/// Create a reverse mapping (destination to source).
	/// </summary>
	IMappingExpression<TDestination, TSource> ReverseMap();

	/// <summary>
	/// Use a custom conversion function instead of member-by-member mapping.
	/// </summary>
	void ConvertUsing(Func<TSource, TDestination> converter);

	/// <summary>
	/// Use a custom type converter instead of member-by-member mapping.
	/// </summary>
	void ConvertUsing<TTypeConverter>() where TTypeConverter : ITypeConverter<TSource, TDestination>, new();

	/// <summary>
	/// Use a custom type converter instance instead of member-by-member mapping.
	/// </summary>
	void ConvertUsing(ITypeConverter<TSource, TDestination> converter);

	/// <summary>
	/// Provide a custom construction function for the destination type.
	/// </summary>
	IMappingExpression<TSource, TDestination> ConstructUsing(Func<TSource, TDestination> ctor);

	/// <summary>
	/// Include a derived type mapping. Configuration from this map is inherited.
	/// </summary>
	IMappingExpression<TSource, TDestination> Include<TDerivedSource, TDerivedDest>()
		where TDerivedSource : TSource
		where TDerivedDest : TDestination;

	/// <summary>
	/// Inherit configuration from a base type mapping.
	/// </summary>
	IMappingExpression<TSource, TDestination> IncludeBase<TBaseSource, TBaseDest>();

	/// <summary>
	/// Automatically use this map for all derived source types that don't have their own map.
	/// </summary>
	IMappingExpression<TSource, TDestination> IncludeAllDerived();

	/// <summary>
	/// Limit the recursion depth for nested mappings.
	/// </summary>
	IMappingExpression<TSource, TDestination> MaxDepth(int depth);

	/// <summary>
	/// Add a value transformer for a specific type.
	/// </summary>
	IMappingExpression<TSource, TDestination> AddTransform<TValue>(Expression<Func<TValue, TValue>> transformer);

	/// <summary>
	/// Configure a specific source member for validation purposes.
	/// </summary>
	IMappingExpression<TSource, TDestination> ForSourceMember<TMember>(
		Expression<Func<TSource, TMember>> sourceMember,
		Action<ISourceMemberConfigurationExpression> memberOptions);
}