using PanoramicData.Mapper.Configuration.Annotations;
using System.Linq.Expressions;
using System.Reflection;

namespace PanoramicData.Mapper.Internal;

/// <summary>
/// Represents a compiled mapping plan between a source and destination type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TypeMap"/> class. The class is split across
/// partial files: the configuration surface and mapping entry points here, the per-member
/// assignment builders in TypeMap.Assignments.cs, value conversion in TypeMap.Conversion.cs,
/// configuration validation in TypeMap.Validation.cs, and convention-based flattening in
/// TypeMap.Flattening.cs.
/// </remarks>
/// <param name="sourceType">The source type for the mapping.</param>
/// <param name="destinationType">The destination type for the mapping.</param>
public sealed partial class TypeMap(Type sourceType, Type destinationType)
{
	/// <summary>
	/// The source type.
	/// </summary>
	public Type SourceType { get; } = sourceType;

	/// <summary>
	/// The destination type.
	/// </summary>
	public Type DestinationType { get; } = destinationType;

	internal Dictionary<string, PropertyMapping> PropertyMappings { get; } = new(StringComparer.Ordinal);

	internal HashSet<string> IgnoredMembers { get; } = new(StringComparer.Ordinal);

	internal HashSet<string> IgnoredSourceMembers { get; } = new(StringComparer.Ordinal);

	/// <summary>
	/// Destination members marked with ExplicitExpansion: excluded from ProjectTo projections unless
	/// explicitly requested. Has no effect on the in-memory Map path.
	/// </summary>
	internal HashSet<string> ExplicitExpansionMembers { get; } = new(StringComparer.Ordinal);

	internal MemberList MemberListValidation { get; set; } = MemberList.Destination;

	internal bool AllMembersIgnored { get; set; }

	internal List<Delegate> BeforeMapActions { get; } = [];

	internal List<Type> BeforeMapActionTypes { get; } = [];

	internal List<Delegate> AfterMapActions { get; } = [];

	internal List<Type> AfterMapActionTypes { get; } = [];

	/// <summary>
	/// Resolver function to find other TypeMaps for nested/collection mapping.
	/// Set by MapperConfiguration after all TypeMaps are collected.
	/// </summary>
	internal Func<Type, Type, TypeMap?>? TypeMapResolver { get; set; }

	/// <summary>
	/// Custom converter function (for ConvertUsing with lambda).
	/// </summary>
	internal Delegate? ConverterFunc { get; set; }

	/// <summary>
	/// Custom converter type (for ConvertUsing with ITypeConverter).
	/// </summary>
	internal Type? ConverterType { get; set; }

	/// <summary>
	/// Custom constructor function (for ConstructUsing).
	/// </summary>
	internal Delegate? ConstructorFunc { get; set; }

	/// <summary>
	/// Constructor parameter mappings (for ForCtorParam).
	/// </summary>
	internal Dictionary<string, LambdaExpression> CtorParamMappings { get; } = new(StringComparer.Ordinal);

	/// <summary>
	/// Maximum recursion depth for nested mappings.
	/// </summary>
	internal int? MaxDepthValue { get; set; }

	/// <summary>
	/// Value transformers keyed by the value type they apply to.
	/// </summary>
	internal List<(Type ValueType, Delegate Transform)> ValueTransformers { get; } = [];

	/// <summary>
	/// Derived type pairs registered via Include.
	/// </summary>
	internal List<(Type DerivedSourceType, Type DerivedDestType)> IncludedDerivedTypes { get; } = [];

	/// <summary>
	/// When true, this map is used for any derived source type that doesn't have its own map.
	/// </summary>
	internal bool IncludeAllDerivedFlag { get; set; }

	/// <summary>
	/// Base type pair registered via IncludeBase.
	/// </summary>
	internal (Type BaseSourceType, Type BaseDestType)? IncludedBaseTypes { get; set; }

	/// <summary>
	/// ForPath mappings: key is the full path expression string, value is the mapping.
	/// </summary>
	internal Dictionary<string, PropertyMapping> PathMappings { get; } = new(StringComparer.Ordinal);

	[ThreadStatic]
	private static int t_currentDepth;

	// The counter tracks recursion depth across the whole object graph, which spans TypeMap
	// instances, so it cannot live on the instance. [ThreadStatic] keeps concurrent map
	// operations from seeing each other's depth.
	private static void EnterDepth() => t_currentDepth++;

	private static void ExitDepth() => t_currentDepth--;

	private Func<object, object, object>? _compiledMapper;

	/// <summary>
	/// Execute the mapping from source to a new destination object.
	/// </summary>
	public object Map(object source)
	{
		if (ConverterFunc is not null)
		{
			return ConverterFunc.DynamicInvoke(source)
				?? throw new InvalidOperationException("Converter returned null.");
		}

		if (ConverterType is not null)
		{
			return MapWithConverterType(source);
		}

		if (MaxDepthValue.HasValue)
		{
			return MapWithDepthTracking(source);
		}

		return MapCore(source);
	}

	private object MapWithConverterType(object source)
	{
		var converter = Activator.CreateInstance(ConverterType!)
			?? throw new InvalidOperationException($"Cannot create instance of converter {ConverterType!.FullName}");
		var convertMethod = ConverterType!.GetMethod("Convert")
			?? throw new InvalidOperationException($"Converter {ConverterType!.FullName} does not have a Convert method");
		var destDefault = DestinationType.IsValueType ? Activator.CreateInstance(DestinationType) : null;
		return convertMethod.Invoke(converter, [source, destDefault, new ResolutionContext()])
			?? throw new InvalidOperationException("Converter returned null.");
	}

	private object MapWithDepthTracking(object source)
	{
		if (t_currentDepth >= MaxDepthValue!.Value)
		{
			return Activator.CreateInstance(DestinationType)
				?? throw new InvalidOperationException($"Cannot create instance of {DestinationType.FullName}.");
		}

		EnterDepth();
		try
		{
			return MapCore(source);
		}
		finally
		{
			ExitDepth();
		}
	}

	private object MapCore(object source)
	{
		var destination = CreateDestination(source);
		return MapToExisting(source, destination);
	}

	/// <summary>
	/// Create a new destination instance without applying property mappings.
	/// </summary>
	internal object CreateDestination(object source)
	{
		if (ConstructorFunc is not null)
		{
			return ConstructorFunc.DynamicInvoke(source)
				?? throw new InvalidOperationException("ConstructUsing returned null.");
		}

		if (CtorParamMappings.Count > 0)
		{
			return ConstructWithParams(source);
		}

		return Activator.CreateInstance(DestinationType)
			?? throw new InvalidOperationException($"Cannot create instance of {DestinationType.FullName}. Ensure it has a parameterless constructor.");
	}

	private object ConstructWithParams(object source)
	{
		var constructors = DestinationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
		foreach (var ctor in constructors.OrderByDescending(c => c.GetParameters().Length))
		{
			var parameters = ctor.GetParameters();
			var allMapped = parameters.All(p => CtorParamMappings.ContainsKey(p.Name!));
			if (!allMapped)
			{
				continue;
			}

			var args = new object?[parameters.Length];
			for (var i = 0; i < parameters.Length; i++)
			{
				var expr = CtorParamMappings[parameters[i].Name!];
				var compiled = expr.Compile();
				args[i] = compiled.DynamicInvoke(source);
			}

			return ctor.Invoke(args);
		}

		// Fallback to parameterless
		return Activator.CreateInstance(DestinationType)
			?? throw new InvalidOperationException($"Cannot create instance of {DestinationType.FullName}. No matching constructor found.");
	}

	/// <summary>
	/// Execute the mapping from source to an existing destination object.
	/// </summary>
	public object MapToExisting(object source, object destination)
	{
		_compiledMapper ??= CompileMapper();

		ExecuteBeforeMapActions(source, destination);
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
			var assignment = TryBuildPropertyAssignment(destProp, sourceProperties);
			if (assignment is not null)
			{
				assignments.Add(assignment);
			}
		}

		// ForPath assignments
		foreach (var kvp in PathMappings)
		{
			var pathMapping = kvp.Value;
			if (pathMapping.PathSegments is not null && pathMapping.PathSegments.Length > 0)
			{
				assignments.Add(BuildForPathAssignment(pathMapping));
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

	/// <summary>
	/// Resets the compiled mapper so it will be recompiled on next use.
	/// Called after configuration changes.
	/// </summary>
	internal void ResetCompiledMapper()
	{
		_compiledMapper = null;
	}

	/// <summary>
	/// Copies configuration from this base TypeMap to a derived TypeMap.
	/// </summary>
	internal void CopyConfigurationTo(TypeMap derived)
	{
		foreach (var kvp in PropertyMappings)
		{
			derived.PropertyMappings.TryAdd(kvp.Key, kvp.Value);
		}

		foreach (var ignored in IgnoredMembers)
		{
			derived.IgnoredMembers.Add(ignored);
		}

		foreach (var member in ExplicitExpansionMembers)
		{
			derived.ExplicitExpansionMembers.Add(member);
		}

		foreach (var action in BeforeMapActions)
		{
			derived.BeforeMapActions.Add(action);
		}

		foreach (var action in AfterMapActions)
		{
			derived.AfterMapActions.Add(action);
		}

		foreach (var (valueType, transform) in ValueTransformers)
		{
			derived.ValueTransformers.Add((valueType, transform));
		}
	}
}
