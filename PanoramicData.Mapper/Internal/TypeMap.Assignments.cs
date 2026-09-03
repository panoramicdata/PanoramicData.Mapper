using PanoramicData.Mapper.Configuration.Annotations;
using System.Collections;
using System.Reflection;

namespace PanoramicData.Mapper.Internal;

/// <summary>
/// Builds the delegate that assigns a single destination member, with one method per way a
/// member can be sourced: an explicit configuration, a naming convention, a nested map, a
/// collection, a flattened path, or a ForPath expression.
/// </summary>
public sealed partial class TypeMap
{
	private Action<object, object>? TryBuildPropertyAssignment(
		PropertyInfo destProp,
		Dictionary<string, PropertyInfo> sourceProperties)
	{
		if (AllMembersIgnored)
		{
			return null;
		}

		if (destProp.GetCustomAttribute<IgnoreAttribute>() is not null)
		{
			return null;
		}

		if (IgnoredMembers.Contains(destProp.Name))
		{
			return null;
		}

		if (PropertyMappings.TryGetValue(destProp.Name, out var mapping))
		{
			return BuildMappingAssignment(mapping, destProp);
		}

		if (sourceProperties.TryGetValue(destProp.Name, out var sourceProp))
		{
			return TryBuildConventionAssignment(sourceProp, destProp);
		}

		return TryBuildFlattenedAssignment(destProp);
	}

	private Action<object, object>? TryBuildConventionAssignment(PropertyInfo sourceProp, PropertyInfo destProp)
	{
		if (!IsAssignableOrConvertible(sourceProp.PropertyType, destProp.PropertyType))
		{
			if (TypeMapResolver is not null)
			{
				var nested = TryBuildNestedAssignment(sourceProp, destProp);
				if (nested is not null)
				{
					return nested;
				}

				return TryBuildCollectionPropertyAssignment(sourceProp, destProp);
			}

			return null;
		}

		var srcGetter = CreateGetter(sourceProp);
		var destSetter = CreateSetter(destProp);
		var destType = destProp.PropertyType;

		// Direct assignment when types are directly compatible (no conversion overhead)
		if (destType.IsAssignableFrom(sourceProp.PropertyType))
		{
			return (src, dest) =>
			{
				var value = srcGetter(src);
				value = ApplyValueTransformers(value, destType);
				destSetter(dest, value);
			};
		}

		// Type conversion needed (enum↔integral, numeric widening/narrowing,
		// primitive↔string, string↔enum, nullable unwrapping, etc.)
		return (src, dest) =>
		{
			var value = srcGetter(src);
			value = ConvertValue(value, destType);
			value = ApplyValueTransformers(value, destType);
			destSetter(dest, value);
		};
	}

	private Action<object, object>? TryBuildNestedAssignment(PropertyInfo sourceProp, PropertyInfo destProp)
	{
		var nestedMap = TypeMapResolver!(sourceProp.PropertyType, destProp.PropertyType);
		if (nestedMap is null)
		{
			return null;
		}

		var srcGetter = CreateGetter(sourceProp);
		var destSetter = CreateSetter(destProp);
		return (src, dest) =>
		{
			var value = srcGetter(src);
			if (value is not null)
			{
				destSetter(dest, nestedMap.Map(value));
			}
		};
	}

	private Action<object, object>? TryBuildCollectionPropertyAssignment(PropertyInfo sourceProp, PropertyInfo destProp)
	{
		if (!TryGetCollectionElementType(sourceProp.PropertyType, out var srcElemType) ||
			!TryGetCollectionElementType(destProp.PropertyType, out var destElemType))
		{
			return null;
		}

		var elemMap = TypeMapResolver!(srcElemType, destElemType);
		if (elemMap is null)
		{
			return null;
		}

		var srcGetter = CreateGetter(sourceProp);
		var destSetter = CreateSetter(destProp);
		var destCollType = destProp.PropertyType;
		return (src, dest) =>
		{
			if (srcGetter(src) is IEnumerable value)
			{
				destSetter(dest, MapCollection(value, elemMap, destCollType, destElemType));
			}
		};
	}

	private Action<object, object>? TryBuildFlattenedAssignment(PropertyInfo destProp)
	{
		var flattenedGetter = TryBuildFlattenedGetter(destProp.Name, SourceType);
		if (flattenedGetter is null || !IsAssignableOrConvertible(flattenedGetter.Value.ReturnType, destProp.PropertyType))
		{
			return null;
		}

		var getter = flattenedGetter.Value.Getter;
		var destSetter = CreateSetter(destProp);
		var destType = destProp.PropertyType;

		if (destType.IsAssignableFrom(flattenedGetter.Value.ReturnType))
		{
			return (src, dest) =>
			{
				var value = getter(src);
				value = ApplyValueTransformers(value, destType);
				destSetter(dest, value);
			};
		}

		return (src, dest) =>
		{
			var value = getter(src);
			value = ConvertValue(value, destType);
			value = ApplyValueTransformers(value, destType);
			destSetter(dest, value);
		};
	}

	private Action<object, object> BuildMappingAssignment(PropertyMapping mapping, PropertyInfo destProp)
	{
		var destSetter = CreateSetter(destProp);
		var destGetter = CreateGetter(destProp);
		var destPropType = destProp.PropertyType;

		return (src, dest) => ApplyMapping(mapping, src, dest, destPropType, destGetter, destSetter);
	}

	private void ApplyMapping(
		PropertyMapping mapping,
		object src,
		object dest,
		Type destPropType,
		Func<object, object?> destGetter,
		Action<object, object?> destSetter)
	{
		if (mapping.PreCondition is not null && mapping.PreCondition.DynamicInvoke(src) is false)
		{
			return;
		}

		var value = ResolveValue(mapping, src, dest, destGetter);
		if (!ShouldAssign(mapping, src, dest, value))
		{
			return;
		}

		if (value is null && mapping.HasNullSubstitute)
		{
			value = mapping.NullSubstitute;
		}

		if (value is not null)
		{
			value = MapCollectionIfNeeded(value, destPropType);
		}

		destSetter(dest, ApplyValueTransformers(value, destPropType));
	}

	/// <summary>
	/// Whether the resolved value should be written to the destination member. A null with no
	/// substitute and no explicit source is left alone rather than overwriting whatever the
	/// destination already holds, and any configured condition gets the final say.
	/// </summary>
	private static bool ShouldAssign(PropertyMapping mapping, object src, object dest, object? value)
	{
		if (value is null
			&& !mapping.HasNullSubstitute
			&& mapping.SourceExpression is null
			&& mapping.ValueResolverType is null)
		{
			return false;
		}

		return mapping.Condition is null || mapping.Condition.DynamicInvoke(src, dest, value) is not false;
	}

	/// <summary>
	/// Maps a collection value element-by-element where the destination property's type cannot
	/// take the source collection directly. Returns the value unchanged where that does not apply.
	/// </summary>
	private object MapCollectionIfNeeded(object value, Type destPropType)
	{
		if (TypeMapResolver is null
			|| value is not IEnumerable sourceEnumerable
			|| !NeedsElementWiseMapping(value.GetType(), destPropType))
		{
			return value;
		}

		if (!TryGetCollectionElementType(value.GetType(), out var srcElemType)
			|| !TryGetCollectionElementType(destPropType, out var destElemType))
		{
			return value;
		}

		var elemMap = TypeMapResolver(srcElemType, destElemType);
		return elemMap is null
			? value
			: MapCollection(sourceEnumerable, elemMap, destPropType, destElemType);
	}

	/// <summary>
	/// A destination property typed as an interface or abstract collection cannot take a concrete
	/// source collection directly (e.g. IList&lt;TDest&gt; from a List&lt;TSource&gt;), so its
	/// elements have to be mapped one at a time.
	/// </summary>
	private static bool NeedsElementWiseMapping(Type valueType, Type destPropType)
		=> (destPropType.IsInterface || destPropType.IsAbstract)
			&& !destPropType.IsAssignableFrom(valueType);

	private static object? ResolveValue(PropertyMapping mapping, object src, object dest, Func<object, object?> destGetter)
	{
		if (mapping.ValueResolverType is not null)
		{
			var resolver = mapping.ValueResolverInstance
				?? Activator.CreateInstance(mapping.ValueResolverType)
				?? throw new InvalidOperationException($"Cannot create resolver {mapping.ValueResolverType.FullName}");
			var resolveMethod = mapping.ValueResolverType.GetMethod("Resolve")
				?? throw new InvalidOperationException($"Resolver {mapping.ValueResolverType.FullName} does not have a Resolve method");
			var currentDestValue = destGetter(dest);
			return resolveMethod.Invoke(resolver, [src, dest, currentDestValue, new ResolutionContext()]);
		}

		if (mapping.SourceExpression is not null)
		{
			var compiledGetter = mapping.SourceExpression.Compile();
			return compiledGetter.DynamicInvoke(src);
		}

		return null;
	}

	private Action<object, object> BuildForPathAssignment(PropertyMapping pathMapping)
	{
		var segments = pathMapping.PathSegments!;

		return (src, dest) =>
		{
			if (pathMapping.PreCondition is not null && pathMapping.PreCondition.DynamicInvoke(src) is false)
			{
				return;
			}

			object? value = null;
			if (pathMapping.SourceExpression is not null)
			{
				var compiled = pathMapping.SourceExpression.Compile();
				value = compiled.DynamicInvoke(src);
			}

			SetNestedValue(dest, DestinationType, segments, value);
		};
	}

	private static void SetNestedValue(object target, Type targetType, string[] segments, object? value)
	{
		var current = target;
		var currentType = targetType;

		for (var i = 0; i < segments.Length - 1; i++)
		{
			var prop = currentType.GetProperty(segments[i], BindingFlags.Public | BindingFlags.Instance);
			if (prop is null)
			{
				return;
			}

			var next = prop.GetValue(current);
			if (next is null)
			{
				next = Activator.CreateInstance(prop.PropertyType);
				prop.SetValue(current, next);
			}

			current = next;
			currentType = prop.PropertyType;
		}

		var leafProp = currentType.GetProperty(segments[^1], BindingFlags.Public | BindingFlags.Instance);
		leafProp?.SetValue(current, value);
	}

	private object? ApplyValueTransformers(object? value, Type destType)
	{
		if (value is null || ValueTransformers.Count == 0)
		{
			return value;
		}

		foreach (var (valueType, transform) in ValueTransformers)
		{
			if (valueType.IsAssignableFrom(destType))
			{
				value = transform.DynamicInvoke(value);
			}
		}

		return value;
	}

	private void ExecuteBeforeMapActions(object source, object destination)
	{
		foreach (var action in BeforeMapActions)
		{
			action.DynamicInvoke(source, destination);
		}

		var context = new ResolutionContext();
		foreach (var actionType in BeforeMapActionTypes)
		{
			var instance = Activator.CreateInstance(actionType)
				?? throw new InvalidOperationException($"Cannot create instance of mapping action {actionType.FullName}");
			var processMethod = actionType.GetMethod("Process")
				?? throw new InvalidOperationException($"Mapping action {actionType.FullName} does not have a Process method");
			processMethod.Invoke(instance, [source, destination, context]);
		}
	}

	private void ExecuteAfterMapActions(object source, object destination)
	{
		var context = new ResolutionContext();

		foreach (var action in AfterMapActions)
		{
			action.DynamicInvoke(source, destination);
		}

		foreach (var actionType in AfterMapActionTypes)
		{
			var instance = Activator.CreateInstance(actionType)
				?? throw new InvalidOperationException($"Cannot create instance of mapping action {actionType.FullName}");

			// Find and invoke the Process method
			var processMethod = actionType.GetMethod("Process")
				?? throw new InvalidOperationException($"Mapping action {actionType.FullName} does not have a Process method");

			processMethod.Invoke(instance, [source, destination, context]);
		}
	}
}
