using PanoramicData.Mapper.Configuration.Annotations;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace PanoramicData.Mapper.Internal;

/// <summary>
/// Represents a compiled mapping plan between a source and destination type.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="TypeMap"/> class.
/// </remarks>
/// <param name="sourceType">The source type for the mapping.</param>
/// <param name="destinationType">The destination type for the mapping.</param>
public sealed class TypeMap(Type sourceType, Type destinationType)
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

		t_currentDepth++;
		try
		{
			return MapCore(source);
		}
		finally
		{
			t_currentDepth--;
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
			var value = srcGetter(src);
			if (value is not null)
			{
				destSetter(dest, MapCollection((IEnumerable)value, elemMap, destCollType, destElemType));
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

		return (src, dest) =>
		{
			if (mapping.PreCondition is not null && mapping.PreCondition.DynamicInvoke(src) is false)
			{
				return;
			}

			var value = ResolveValue(mapping, src, dest, destGetter);
			if (value is null && !mapping.HasNullSubstitute && mapping.SourceExpression is null && mapping.ValueResolverType is null)
			{
				return;
			}

			if (mapping.Condition is not null && mapping.Condition.DynamicInvoke(src, dest, value) is false)
			{
				return;
			}

			if (value is null && mapping.HasNullSubstitute)
			{
				value = mapping.NullSubstitute;
			}

			// If the destination property is an interface or abstract collection type and the
			// resolved value's type is not directly assignable (e.g. List<TSource> -> IList<TDest>),
			// attempt to map it as a collection via the type map resolver.
			if (value is not null && TypeMapResolver is not null
				&& destPropType is { IsInterface: true } or { IsAbstract: true }
				&& !destPropType.IsAssignableFrom(value.GetType())
				&& value is IEnumerable sourceEnumerable
				&& TryGetCollectionElementType(value.GetType(), out var srcElemType)
				&& TryGetCollectionElementType(destPropType, out var destElemType))
			{
				var elemMap = TypeMapResolver(srcElemType, destElemType);
				if (elemMap is not null)
				{
					value = MapCollection(sourceEnumerable, elemMap, destPropType, destElemType);
				}
			}

			value = ApplyValueTransformers(value, destPropType);
			destSetter(dest, value);
		};
	}

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

		var srcCore = underlyingSource ?? sourceType;
		var dstCore = underlyingDest ?? destType;

		// String -> enum (via Enum.Parse)
		if (srcCore == typeof(string) && dstCore.IsEnum)
		{
			return true;
		}

		// IConvertible conversions: numeric widening/narrowing, enum to/from integral,
		// primitive to/from string, enum to string, etc.
		if (typeof(IConvertible).IsAssignableFrom(srcCore) && typeof(IConvertible).IsAssignableFrom(dstCore))
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Converts a source value to the destination type, handling null, enum, numeric,
	/// and string conversions that PropertyInfo.SetValue cannot perform implicitly.
	/// </summary>
	private static object? ConvertValue(object? value, Type destType)
	{
		var dstCore = Nullable.GetUnderlyingType(destType) ?? destType;

		if (value is null)
		{
			// Nullable or reference dest: keep null; non-nullable value type: default(T)
			if (Nullable.GetUnderlyingType(destType) is not null || !destType.IsValueType)
			{
				return null;
			}

			return Activator.CreateInstance(destType);
		}

		var valueType = value.GetType();

		// Already the right type
		if (dstCore.IsAssignableFrom(valueType))
		{
			return value;
		}

		// Any -> string
		if (dstCore == typeof(string))
		{
			return value.ToString();
		}

		// String -> enum
		if (valueType == typeof(string) && dstCore.IsEnum)
		{
			var str = (string)value;
			if (str.Length > 0 && Enum.TryParse(dstCore, str, out var parsed))
			{
				return parsed;
			}

			// Unparseable string - return default for the enum
			return Activator.CreateInstance(destType) ?? Activator.CreateInstance(dstCore)!;
		}

		// Integral -> enum (Convert.ChangeType cannot handle this)
		if (dstCore.IsEnum)
		{
			return Enum.ToObject(dstCore, value);
		}

		// Enum -> integral, numeric widening/narrowing, string -> numeric, etc.
		if (value is IConvertible)
		{
			try
			{
				return Convert.ChangeType(value, dstCore);
			}
			catch (Exception ex) when (ex is FormatException or InvalidCastException or OverflowException)
			{
				// Conversion failed (e.g. empty string -> int) - return default
				return destType.IsValueType ? Activator.CreateInstance(destType) : null;
			}
		}

		return value;
	}

	/// <summary>
	/// Validates that all destination properties are either mapped or explicitly ignored.
	/// Respects the MemberList setting to determine which members to validate.
	/// </summary>
	internal List<string> GetUnmappedDestinationMembers()
	{
		if (AllMembersIgnored || MemberListValidation == MemberList.None)
		{
			return [];
		}

		if (MemberListValidation == MemberList.Source)
		{
			return GetUnmappedSourceMembers();
		}

		var sourceProperties = SourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanRead)
			.ToDictionary(p => p.Name, StringComparer.Ordinal);

		var unmapped = new List<string>();

		foreach (var destProp in DestinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanWrite))
		{
			if (!IsMemberMapped(destProp, sourceProperties))
			{
				unmapped.Add(destProp.Name);
			}
		}

		return unmapped;
	}

	private List<string> GetUnmappedSourceMembers()
	{
		var destProperties = DestinationType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanWrite)
			.ToDictionary(p => p.Name, StringComparer.Ordinal);

		var unmapped = new List<string>();

		foreach (var srcProp in SourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead))
		{
			if (IgnoredSourceMembers.Contains(srcProp.Name))
			{
				continue;
			}

			if (destProperties.ContainsKey(srcProp.Name))
			{
				continue;
			}

			if (PropertyMappings.Values.Any(pm => pm.SourceExpression is not null && ExpressionReferencesProperty(pm.SourceExpression, srcProp.Name)))
			{
				continue;
			}

			unmapped.Add(srcProp.Name);
		}

		return unmapped;
	}

	private static bool ExpressionReferencesProperty(LambdaExpression expression, string propertyName)
	{
		var body = expression.Body;
		if (body is UnaryExpression { Operand: MemberExpression unaryMember })
		{
			return unaryMember.Member.Name == propertyName;
		}

		if (body is MemberExpression memberExpr)
		{
			return memberExpr.Member.Name == propertyName;
		}

		return false;
	}

	private bool IsMemberMapped(PropertyInfo destProp, Dictionary<string, PropertyInfo> sourceProperties)
	{
		if (destProp.GetCustomAttribute<IgnoreAttribute>() is not null)
		{
			return true;
		}

		if (IgnoredMembers.Contains(destProp.Name))
		{
			return true;
		}

		if (PropertyMappings.ContainsKey(destProp.Name))
		{
			return true;
		}

		if (PathMappings.Values.Any(pm => pm.PathSegments is not null && pm.PathSegments[0] == destProp.Name))
		{
			return true;
		}

		if (sourceProperties.ContainsKey(destProp.Name))
		{
			return true;
		}

		var flattenedGetter = TryBuildFlattenedGetter(destProp.Name, SourceType);
		return flattenedGetter is not null && IsAssignableOrConvertible(flattenedGetter.Value.ReturnType, destProp.PropertyType);
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