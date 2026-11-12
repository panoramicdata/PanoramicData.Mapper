using static PanoramicData.Mapper.Internal.TypeMapConversion;
using System.Reflection;

namespace PanoramicData.Mapper.Internal;

/// <summary>
/// Applies an explicitly configured member mapping: pre-condition, source resolution, null substitution and conditions.
/// </summary>
public sealed partial class TypeMap
{
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
		if (PreConditionFails(mapping, src))
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

	private static bool PreConditionFails(PropertyMapping mapping, object src)
		=> mapping.PreCondition is not null && mapping.PreCondition.DynamicInvoke(src) is false;
}
