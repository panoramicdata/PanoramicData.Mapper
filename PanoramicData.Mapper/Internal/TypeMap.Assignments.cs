#pragma warning disable S2333 // partial is required: class is split across multiple files
using static PanoramicData.Mapper.Internal.TypeMapConversion;
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
		if (IsMemberIgnored(destProp))
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

	private bool IsMemberIgnored(PropertyInfo destProp)
		=> AllMembersIgnored
			|| destProp.GetCustomAttribute<IgnoreAttribute>() is not null
			|| IgnoredMembers.Contains(destProp.Name);

	/// <summary>
	/// Reads the source value, converts it only when the types are not directly assignable,
	/// applies value transformers and writes the destination member.
	/// </summary>
	private Action<object, object> BuildValueAssignment(
		Func<object, object?> getter,
		Type sourceType,
		PropertyInfo destProp)
	{
		var destSetter = CreateSetter(destProp);
		var destType = destProp.PropertyType;

		if (destType.IsAssignableFrom(sourceType))
		{
			return (src, dest) =>
				destSetter(dest, ApplyValueTransformers(getter(src), destType));
		}

		return (src, dest) =>
			destSetter(dest, ApplyValueTransformers(ConvertValue(getter(src), destType), destType));
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

		return BuildValueAssignment(CreateGetter(sourceProp), sourceProp.PropertyType, destProp);
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

		return BuildValueAssignment(flattenedGetter.Value.Getter, flattenedGetter.Value.ReturnType, destProp);
	}
}
