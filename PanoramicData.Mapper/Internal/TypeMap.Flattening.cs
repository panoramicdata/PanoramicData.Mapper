using System.Reflection;

namespace PanoramicData.Mapper.Internal;

/// <summary>
/// Resolves a flattened destination member name (e.g. <c>CustomerName</c>) to a chain of
/// source property accesses (<c>Customer.Name</c>).
/// </summary>
public sealed partial class TypeMap
{
	private readonly record struct FlattenedAccessor(Func<object, object?> Getter, Type ReturnType);

	private static FlattenedAccessor? TryBuildFlattenedGetter(string destPropName, Type sourceType)
	{
		var segments = SplitPascalCase(destPropName);
		return TryBuildAccessorChain(segments, 0, sourceType);
	}

	private static FlattenedAccessor? TryBuildAccessorChain(List<string> segments, int startIndex, Type currentType)
	{
		if (startIndex >= segments.Count)
		{
			return null;
		}

		var props = currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanRead)
			.ToDictionary(p => p.Name, StringComparer.Ordinal);

		for (var length = 1; length <= segments.Count - startIndex; length++)
		{
			var prefix = string.Concat(segments.Skip(startIndex).Take(length));
			var remainingConsumed = startIndex + length == segments.Count;

			if (props.TryGetValue(prefix, out var prop))
			{
				var result = BuildPropertyAccessor(segments, startIndex + length, remainingConsumed, obj => prop.GetValue(obj), prop.PropertyType);
				if (result is not null)
				{
					return result;
				}
			}

			var method = currentType.GetMethod($"Get{prefix}", BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes);
			if (method is not null && method.ReturnType != typeof(void))
			{
				var result = BuildPropertyAccessor(segments, startIndex + length, remainingConsumed, obj => method.Invoke(obj, null), method.ReturnType);
				if (result is not null)
				{
					return result;
				}
			}
		}

		return null;
	}

	private static FlattenedAccessor? BuildPropertyAccessor(
		List<string> segments,
		int nextIndex,
		bool allConsumed,
		Func<object, object?> getter,
		Type returnType)
	{
		if (allConsumed)
		{
			return new FlattenedAccessor(getter, returnType);
		}

		var nested = TryBuildAccessorChain(segments, nextIndex, returnType);
		if (nested is null)
		{
			return null;
		}

		var nestedGetter = nested.Value.Getter;
		return new FlattenedAccessor(
			obj =>
			{
				var intermediate = getter(obj);
				return intermediate is null ? null : nestedGetter(intermediate);
			},
			nested.Value.ReturnType);
	}

	private static List<string> SplitPascalCase(string name)
	{
		var segments = new List<string>();
		var start = 0;
		for (var i = 1; i < name.Length; i++)
		{
			if (char.IsUpper(name[i]))
			{
				segments.Add(name[start..i]);
				start = i;
			}
		}

		segments.Add(name[start..]);
		return segments;
	}
}
