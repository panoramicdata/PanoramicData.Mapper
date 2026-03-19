using PanoramicData.Mapper.Configuration.Annotations;
using System.Collections;
using System.Linq.Expressions;
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
		if (_compiledMapper is null)
		{
			_compiledMapper = CompileMapper();
		}

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
		if (IsAssignableOrConvertible(sourceProp.PropertyType, destProp.PropertyType))
		{
			var srcGetter = CreateGetter(sourceProp);
			var destSetter = CreateSetter(destProp);

			// When one side is an enum and the other is its underlying integral type,
			// we need an explicit conversion step (PropertyInfo.SetValue won't box-convert automatically).
			if (IsEnumIntegralPair(sourceProp.PropertyType, destProp.PropertyType))
			{
				return BuildEnumIntegralAssignment(srcGetter, destSetter, sourceProp.PropertyType, destProp.PropertyType);
			}

			return (src, dest) =>
			{
				var value = srcGetter(src);
				value = ApplyValueTransformers(value, destProp.PropertyType);
				destSetter(dest, value);
			};
		}

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

	private Action<object, object> BuildEnumIntegralAssignment(
		Func<object, object?> srcGetter,
		Action<object, object?> destSetter,
		Type sourceType,
		Type destType)
	{
		var srcUnderlying = Nullable.GetUnderlyingType(sourceType);
		var dstUnderlying = Nullable.GetUnderlyingType(destType);
		var srcCore = srcUnderlying ?? sourceType;
		var dstCore = dstUnderlying ?? destType;
		var destIsNullable = dstUnderlying is not null;

		return (src, dest) =>
		{
			var value = srcGetter(src);

			if (value is null)
			{
				// Nullable source with null value
				if (destIsNullable)
				{
					destSetter(dest, null);
				}
				else
				{
					// Nullable source -> non-nullable dest: use default(0)
					destSetter(dest, Activator.CreateInstance(destType));
				}

				return;
			}

			object converted;
			if (srcCore.IsEnum && !dstCore.IsEnum)
			{
				// enum -> integral
				converted = Convert.ChangeType(value, dstCore);
			}
			else
			{
				// integral -> enum
				converted = Enum.ToObject(dstCore, value);
			}

			converted = ApplyValueTransformers(converted, destType)!;
			destSetter(dest, converted);
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
		return (src, dest) =>
		{
			var value = getter(src);
			value = ApplyValueTransformers(value, destProp.PropertyType);
			destSetter(dest, value);
		};
	}

	private Action<object, object> BuildMappingAssignment(PropertyMapping mapping, PropertyInfo destProp)
	{
		var destSetter = CreateSetter(destProp);
		var destGetter = CreateGetter(destProp);

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

			value = ApplyValueTransformers(value, destProp.PropertyType);
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

		// Handle enum <-> integral type conversions (including nullable variants)
		if (IsEnumIntegralPair(sourceType, destType))
		{
			return true;
		}

		return false;
	}

	/// <summary>
	/// Returns true when one type is an enum and the other is its underlying integral type,
	/// including Nullable&lt;T&gt; wrappers on either or both sides.
	/// </summary>
	private static bool IsEnumIntegralPair(Type sourceType, Type destType)
	{
		var srcUnderlying = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
		var dstUnderlying = Nullable.GetUnderlyingType(destType) ?? destType;

		// enum -> integral
		if (srcUnderlying.IsEnum && Enum.GetUnderlyingType(srcUnderlying) == dstUnderlying)
		{
			return true;
		}

		// integral -> enum
		if (dstUnderlying.IsEnum && Enum.GetUnderlyingType(dstUnderlying) == srcUnderlying)
		{
			return true;
		}

		return false;
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