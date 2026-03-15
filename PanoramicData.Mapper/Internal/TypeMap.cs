using PanoramicData.Mapper.Configuration.Annotations;
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
				}
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

		var sourceProperties = new HashSet<string>(
			SourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
				.Where(p => p.CanRead)
				.Select(p => p.Name),
			StringComparer.Ordinal);

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

			// Skip if convention-matched
			if (sourceProperties.Contains(destProp.Name))
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
}