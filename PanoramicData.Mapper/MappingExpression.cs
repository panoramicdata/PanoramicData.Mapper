using PanoramicData.Mapper.Internal;
using System.Linq.Expressions;

namespace PanoramicData.Mapper;

/// <summary>
/// Fluent configuration for a mapping between source and destination types.
/// </summary>
internal sealed class MappingExpression<TSource, TDestination>(TypeMap typeMap, Action<TypeMap> registerTypeMap) : IMappingExpression<TSource, TDestination>
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

	public IMappingExpression<TSource, TDestination> ForPath<TMember>(
		Expression<Func<TDestination, TMember>> destinationPath,
		Action<IMemberConfigurationExpression<TSource, TDestination, TMember>> memberOptions)
	{
		var pathSegments = GetPathSegments(destinationPath);
		var pathKey = string.Join(".", pathSegments);
		var config = new MemberConfigurationExpression<TSource, TDestination, TMember>(pathKey);
		memberOptions(config);

		var mapping = new PropertyMapping(pathKey)
		{
			PathSegments = pathSegments,
			SourceExpression = config.SourceExpression
		};

		typeMap.PathMappings[pathKey] = mapping;
		typeMap.ResetCompiledMapper();
		return this;
	}

	public IMappingExpression<TSource, TDestination> ForCtorParam(
		string ctorParamName,
		Action<ICtorParamConfigurationExpression<TSource>> paramOptions)
	{
		var config = new CtorParamConfigurationExpression<TSource>(ctorParamName);
		paramOptions(config);

		if (config.SourceExpression is not null)
		{
			typeMap.CtorParamMappings[ctorParamName] = config.SourceExpression;
		}

		typeMap.ResetCompiledMapper();
		return this;
	}

	public IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination> beforeFunction)
	{
		typeMap.BeforeMapActions.Add(beforeFunction);
		return this;
	}

	public IMappingExpression<TSource, TDestination> BeforeMap<TMappingAction>()
		where TMappingAction : IMappingAction<TSource, TDestination>, new()
	{
		typeMap.BeforeMapActionTypes.Add(typeof(TMappingAction));
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

	public IMappingExpression<TDestination, TSource> ReverseMap()
	{
		var reverseTypeMap = new TypeMap(typeof(TDestination), typeof(TSource));
		registerTypeMap(reverseTypeMap);
		return new MappingExpression<TDestination, TSource>(reverseTypeMap, registerTypeMap);
	}

	public void ConvertUsing(Func<TSource, TDestination> converter)
	{
		typeMap.ConverterFunc = converter;
	}

	public void ConvertUsing<TTypeConverter>() where TTypeConverter : ITypeConverter<TSource, TDestination>, new()
	{
		typeMap.ConverterType = typeof(TTypeConverter);
	}

	public void ConvertUsing(ITypeConverter<TSource, TDestination> converter)
	{
		typeMap.ConverterFunc = new Func<TSource, TDestination>(src =>
			converter.Convert(src, default!, new ResolutionContext()));
	}

	public IMappingExpression<TSource, TDestination> ConstructUsing(Func<TSource, TDestination> ctor)
	{
		typeMap.ConstructorFunc = ctor;
		return this;
	}

	public IMappingExpression<TSource, TDestination> Include<TDerivedSource, TDerivedDest>()
		where TDerivedSource : TSource
		where TDerivedDest : TDestination
	{
		typeMap.IncludedDerivedTypes.Add((typeof(TDerivedSource), typeof(TDerivedDest)));
		return this;
	}

	public IMappingExpression<TSource, TDestination> IncludeBase<TBaseSource, TBaseDest>()
	{
		typeMap.IncludedBaseTypes = (typeof(TBaseSource), typeof(TBaseDest));
		return this;
	}

	public IMappingExpression<TSource, TDestination> IncludeAllDerived()
	{
		typeMap.IncludeAllDerivedFlag = true;
		return this;
	}

	public IMappingExpression<TSource, TDestination> MaxDepth(int depth)
	{
		typeMap.MaxDepthValue = depth;
		return this;
	}

	public IMappingExpression<TSource, TDestination> AddTransform<TValue>(Expression<Func<TValue, TValue>> transformer)
	{
		typeMap.ValueTransformers.Add((typeof(TValue), transformer.Compile()));
		typeMap.ResetCompiledMapper();
		return this;
	}

	public IMappingExpression<TSource, TDestination> ForSourceMember<TMember>(
		Expression<Func<TSource, TMember>> sourceMember,
		Action<ISourceMemberConfigurationExpression> memberOptions)
	{
		var memberName = GetSourceMemberName(sourceMember);
		var config = new SourceMemberConfigurationExpression();
		memberOptions(config);

		if (config.IsDoNotValidate)
		{
			typeMap.IgnoredSourceMembers.Add(memberName);
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

	private static string GetSourceMemberName<TMember>(Expression<Func<TSource, TMember>> expression)
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

	private static string[] GetPathSegments<TMember>(Expression<Func<TDestination, TMember>> expression)
	{
		var segments = new List<string>();
		var current = expression.Body;

		// Unwrap Convert nodes
		if (current is UnaryExpression { NodeType: ExpressionType.Convert } unary)
		{
			current = unary.Operand;
		}

		while (current is MemberExpression member)
		{
			segments.Add(member.Member.Name);
			current = member.Expression;
		}

		segments.Reverse();
		return [.. segments];
	}

	private void ApplyMemberConfig<TMember>(MemberConfigurationExpression<TSource, TDestination, TMember> config)
	{
		if (config.IsIgnored)
		{
			typeMap.IgnoredMembers.Add(config.MemberName);
			typeMap.PropertyMappings.Remove(config.MemberName);
		}
		else
		{
			var mapping = new PropertyMapping(config.MemberName)
			{
				SourceExpression = config.SourceExpression,
				Condition = config.ConditionDelegate,
				PreCondition = config.PreConditionDelegate,
				NullSubstitute = config.NullSubstituteValue,
				HasNullSubstitute = config.HasNullSubstitute,
				ValueResolverType = config.ValueResolverType,
				ValueResolverInstance = config.ValueResolverInstance,
				UseDestinationValue = config.UseDestValue
			};

			// Only store if there's actually something configured beyond just the name
			if (config.SourceExpression is not null ||
				config.ValueResolverType is not null ||
				config.ConditionDelegate is not null ||
				config.PreConditionDelegate is not null ||
				config.HasNullSubstitute ||
				config.UseDestValue)
			{
				typeMap.PropertyMappings[config.MemberName] = mapping;
			}
		}
	}
}