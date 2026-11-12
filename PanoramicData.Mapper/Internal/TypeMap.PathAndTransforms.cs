using System.Reflection;

namespace PanoramicData.Mapper.Internal;

/// <summary>
/// ForPath assignment into nested members, and value transformers.
/// </summary>
public sealed partial class TypeMap
{
	private Action<object, object> BuildForPathAssignment(PropertyMapping pathMapping)
	{
		var segments = pathMapping.PathSegments!;

		return (src, dest) =>
		{
			if (PreConditionFails(pathMapping, src))
			{
				return;
			}

			var value = pathMapping.SourceExpression?.Compile().DynamicInvoke(src);
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
}
