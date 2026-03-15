using PanoramicData.Mapper.Configuration.Annotations;
using System.Collections;
using System.Reflection;

namespace PanoramicData.Mapper.Internal;

/// <summary>
/// Represents a compiled mapping plan between a source and destination type.
/// </summary>
public sealed class TypeMap
{
	/// <summary>
	/// The source type.
	/// </summary>
	public Type SourceType { get; }

	/// <summary>
	/// The destination type.
	/// </summary>
	public Type DestinationType { get; }

	internal Dictionary<string, PropertyMapping> PropertyMappings { get; } = new(StringComparer.Ordinal);

	internal HashSet<string> IgnoredMembers { get; } = new(StringComparer.Ordinal);

	internal bool AllMembersIgnored { get; set; }

	internal List<Delegate> AfterMapActions { get; } = [];

	internal List<Type> AfterMapActionTypes { get; } = [];

	/// <summary>
	/// Resolver function to find other TypeMaps for nested/collection mapping.
	/// Set by MapperConfiguration after all TypeMaps are collected.
	/// </summary>
	internal Func<Type, Type, TypeMap?>? TypeMapResolver { get; set; }

	private Func<object, object, object>? _compiledMapper;

	public TypeMap(Type sourceType, Type destinationType)
	{
		SourceType = sourceType;
		DestinationType = destinationType;
	}

	/// <summary>
	/// Execute the mapping from source to a new destination object.
	/// </summary>
	public object Map(object source)
	{
		var destination = Activator.CreateInstance(DestinationType)
			?? throw new InvalidOperationException($"Cannot create instance of {DestinationType.FullName}. Ensure it has a parameterless constructor.");
		return MapToExisting(source, destination);
	}

	/// <summary>
	/// Execute the mapping from source to an existing destination object.
	/// </summary>
	public object MapToExisting(object source, object destination)
	{
		if (_compiledMapper is null)
		{
			_compiledMapper = CompileMapper();
		}

		_compiledMapper(source, destination);
		ExecuteAfterMapActions(source, destination);
		return destination;
	}

	private Func<object, object, object> CompileMapper()
	{
		var sourceProperties = SourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanRead)
			.ToDictionary(p => p.Name, StringComparer.Ordinal);

		var destProperties = DestinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanWrite);

		// Build the property assignments
		var assignments = new List<Action<object, object>>();

		foreach (var destProp in destProperties)
		{
			// If all members are ignored, skip everything
			if (AllMembersIgnored)
			{
				continue;
			}

			// Check for [Ignore] attribute on destination property
			if (destProp.GetCustomAttribute<IgnoreAttribute>() is not null)
			{
				continue;
			}

			// Check if this member is explicitly ignored
			if (IgnoredMembers.Contains(destProp.Name))
			{
				continue;
			}

			// Check if there's a custom MapFrom expression
			if (PropertyMappings.TryGetValue(destProp.Name, out var mapping))
			{
				var compiledGetter = mapping.SourceExpression!.Compile();
				var destSetter = CreateSetter(destProp);
				assignments.Add((src, dest) =>
				{
					var value = compiledGetter.DynamicInvoke(src);
					destSetter(dest, value);
				});
				continue;
			}

			// Convention-based: match by name and compatible type
			if (sourceProperties.TryGetValue(destProp.Name, out var sourceProp))
			{
				if (IsAssignableOrConvertible(sourceProp.PropertyType, destProp.PropertyType))
				{
					var srcGetter = CreateGetter(sourceProp);
					var destSetter = CreateSetter(destProp);
					assignments.Add((src, dest) =>
					{
						var value = srcGetter(src);
						destSetter(dest, value);
					});
					continue;
				}

				// Nested mapping: source and dest property types differ but a TypeMap exists
				if (TypeMapResolver is not null)
				{
					var nestedMap = TypeMapResolver(sourceProp.PropertyType, destProp.PropertyType);
					if (nestedMap is not null)
					{
						var srcGetter = CreateGetter(sourceProp);
						var destSetter = CreateSetter(destProp);
						assignments.Add((src, dest) =>
						{
							var value = srcGetter(src);
							if (value is not null)
							{
								destSetter(dest, nestedMap.Map(value));
							}
						});
						continue;
					}

					// Collection property mapping: both are collections and element types have a map
					if (TryGetCollectionElementType(sourceProp.PropertyType, out var srcElemType) &&
						TryGetCollectionElementType(destProp.PropertyType, out var destElemType))
					{
						var elemMap = TypeMapResolver(srcElemType, destElemType);
						if (elemMap is not null)
						{
							var srcGetter = CreateGetter(sourceProp);
							var destSetter = CreateSetter(destProp);
							var destCollType = destProp.PropertyType;
							assignments.Add((src, dest) =>
							{
								var value = srcGetter(src);
								if (value is not null)
								{
									destSetter(dest, MapCollection((IEnumerable)value, elemMap, destCollType, destElemType));
								}
							});
							continue;
						}
					}
				}

				continue;
			}

			// Flattening: split PascalCase destination name and traverse source graph
			var flattenedGetter = TryBuildFlattenedGetter(destProp.Name, SourceType);
			if (flattenedGetter is not null && IsAssignableOrConvertible(flattenedGetter.Value.ReturnType, destProp.PropertyType))
			{
				var getter = flattenedGetter.Value.Getter;
				var destSetter = CreateSetter(destProp);
				assignments.Add((src, dest) =>
				{
					var value = getter(src);
					destSetter(dest, value);
				});
			}
		}

		return (src, dest) =>
		{
			foreach (var assignment in assignments)
			{
				assignment(src, dest);
			}

			return dest;
		};
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

	private static Func<object, object?> CreateGetter(PropertyInfo prop)
	{
		return obj => prop.GetValue(obj);
	}

	private static Action<object, object?> CreateSetter(PropertyInfo prop)
	{
		return (obj, value) => prop.SetValue(obj, value);
	}

	private static bool IsAssignableOrConvertible(Type sourceType, Type destType)
	{
		if (destType.IsAssignableFrom(sourceType))
		{
			return true;
		}

		// Handle nullable destination
		var underlyingDest = Nullable.GetUnderlyingType(destType);
		if (underlyingDest is not null && underlyingDest.IsAssignableFrom(sourceType))
		{
			return true;
		}

		// Handle nullable source
		var underlyingSource = Nullable.GetUnderlyingType(sourceType);
		if (underlyingSource is not null && destType.IsAssignableFrom(underlyingSource))
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Validates that all destination properties are either mapped or explicitly ignored.
	/// </summary>
	internal List<string> GetUnmappedDestinationMembers()
	{
		if (AllMembersIgnored)
		{
			return [];
		}

		var sourceProperties = SourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanRead)
			.ToDictionary(p => p.Name, StringComparer.Ordinal);

		var unmapped = new List<string>();

		foreach (var destProp in DestinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
		{
			// Skip if ignored via attribute
			if (destProp.GetCustomAttribute<IgnoreAttribute>() is not null)
			{
				continue;
			}

			// Skip if explicitly ignored
			if (IgnoredMembers.Contains(destProp.Name))
			{
				continue;
			}

			// Skip if has custom mapping
			if (PropertyMappings.ContainsKey(destProp.Name))
			{
				continue;
			}

			// Skip if convention-matched by name
			if (sourceProperties.ContainsKey(destProp.Name))
			{
				continue;
			}

			// Skip if resolvable via flattening
			var flattenedGetter = TryBuildFlattenedGetter(destProp.Name, SourceType);
			if (flattenedGetter is not null && IsAssignableOrConvertible(flattenedGetter.Value.ReturnType, destProp.PropertyType))
			{
				continue;
			}

			unmapped.Add(destProp.Name);
		}

		return unmapped;
	}

	/// <summary>
	/// Resets the compiled mapper so it will be recompiled on next use.
	/// Called after configuration changes.
	/// </summary>
	internal void ResetCompiledMapper()
	{
		_compiledMapper = null;
	}

	#region Flattening

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

		// Try increasingly long prefixes of remaining segments
		for (var length = 1; length <= segments.Count - startIndex; length++)
		{
			var prefix = string.Concat(segments.Skip(startIndex).Take(length));

			// Try property match
			if (props.TryGetValue(prefix, out var prop))
			{
				if (startIndex + length == segments.Count)
				{
					// All segments consumed — this is the final value
					return new FlattenedAccessor(obj => prop.GetValue(obj), prop.PropertyType);
				}

				// More segments remain — recurse into this property's type
				var nested = TryBuildAccessorChain(segments, startIndex + length, prop.PropertyType);
				if (nested is not null)
				{
					var nestedGetter = nested.Value.Getter;
					return new FlattenedAccessor(
						obj =>
						{
							var intermediate = prop.GetValue(obj);
							return intermediate is null ? null : nestedGetter(intermediate);
						},
						nested.Value.ReturnType);
				}
			}

			// Try GetX() method match
			var method = currentType.GetMethod($"Get{prefix}", BindingFlags.Public | BindingFlags.Instance, Type.EmptyTypes);
			if (method is not null && method.ReturnType != typeof(void))
			{
				if (startIndex + length == segments.Count)
				{
					return new FlattenedAccessor(obj => method.Invoke(obj, null), method.ReturnType);
				}

				var nested = TryBuildAccessorChain(segments, startIndex + length, method.ReturnType);
				if (nested is not null)
				{
					var nestedGetter = nested.Value.Getter;
					return new FlattenedAccessor(
						obj =>
						{
							var intermediate = method.Invoke(obj, null);
							return intermediate is null ? null : nestedGetter(intermediate);
						},
						nested.Value.ReturnType);
				}
			}
		}

		return null;
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

	#endregion

	#region Collection helpers

	internal static bool TryGetCollectionElementType(Type type, out Type elementType)
	{
		// Arrays
		if (type.IsArray)
		{
			elementType = type.GetElementType()!;
			return true;
		}

		// IEnumerable<T> implemented by the type
		var enumerableInterface = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>)
			? type
			: type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

		if (enumerableInterface is not null)
		{
			elementType = enumerableInterface.GetGenericArguments()[0];
			return true;
		}

		elementType = default!;
		return false;
	}

	internal static object MapCollection(IEnumerable source, TypeMap elementTypeMap, Type destCollectionType, Type destElementType)
	{
		// Array destination
		if (destCollectionType.IsArray)
		{
			var items = source.Cast<object>().Select(item => elementTypeMap.Map(item)).ToList();
			var array = Array.CreateInstance(destElementType, items.Count);
			for (var i = 0; i < items.Count; i++)
			{
				array.SetValue(items[i], i);
			}

			return array;
		}

		// List<T> or any interface assignable from List<T>
		var listType = typeof(List<>).MakeGenericType(destElementType);
		var list = (IList)Activator.CreateInstance(listType)!;
		foreach (var item in source)
		{
			list.Add(elementTypeMap.Map(item));
		}

		return list;
	}

	#endregion
}