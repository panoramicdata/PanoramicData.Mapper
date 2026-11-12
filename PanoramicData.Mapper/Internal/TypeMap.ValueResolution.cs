using System.Collections;

namespace PanoramicData.Mapper.Internal;

/// <summary>
/// Resolves the value for a configured member from a value resolver or source expression, and maps collection values.
/// </summary>
public sealed partial class TypeMap
{
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
}
