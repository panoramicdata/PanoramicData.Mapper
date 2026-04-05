using PanoramicData.Mapper.Configuration.Annotations;
using PanoramicData.Mapper.Internal;
using System.Linq.Expressions;
using System.Reflection;

namespace PanoramicData.Mapper.QueryableExtensions;

/// <summary>
/// Extension methods for IQueryable to support projection.
/// </summary>
public static class Extensions
{
	/// <summary>
	/// Projects the source queryable to the destination type using the mapper configuration.
	/// This produces an Expression that can be translated by EF Core to SQL.
	/// </summary>
	public static IQueryable<TDestination> ProjectTo<TDestination>(
		this IQueryable source,
		IConfigurationProvider configurationProvider)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(configurationProvider);

		var sourceType = source.ElementType;
		var destType = typeof(TDestination);

		var typeMap = configurationProvider.FindTypeMap(sourceType, destType);

		// Build a Select expression: source.Select(s => new TDestination { Prop1 = s.Prop1, ... })
		var selectExpression = BuildProjectionExpression<TDestination>(sourceType, typeMap);

		// Call Queryable.Select with the expression
		var selectMethod = typeof(Queryable)
			.GetMethods()
			.First(m => m.Name == nameof(Queryable.Select) && m.GetParameters().Length == 2)
			.MakeGenericMethod(sourceType, destType);

		var result = selectMethod.Invoke(null, [source, selectExpression])
			?? throw new InvalidOperationException("Failed to create projected queryable.");

		return (IQueryable<TDestination>)result;
	}

	private static LambdaExpression BuildProjectionExpression<TDestination>(
		Type sourceType,
		TypeMap? typeMap)
	{
		var destType = typeof(TDestination);
		var sourceParam = Expression.Parameter(sourceType, "src");

		var sourceProperties = sourceType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanRead)
			.ToDictionary(p => p.Name, StringComparer.Ordinal);

		var destProperties = destType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
			.Where(p => p.CanWrite);

		var bindings = new List<MemberBinding>();

		foreach (var destProp in destProperties)
		{
			// Check for [Ignore] attribute
			if (destProp.GetCustomAttribute<IgnoreAttribute>() is not null)
			{
				continue;
			}

			// Check if explicitly ignored in the type map
			if (typeMap is not null && typeMap.AllMembersIgnored)
			{
				continue;
			}

			if (typeMap is not null && typeMap.IgnoredMembers.Contains(destProp.Name))
			{
				continue;
			}

			// Check for custom MapFrom expression
			if (typeMap is not null && typeMap.PropertyMappings.TryGetValue(destProp.Name, out var mapping) && mapping.SourceExpression is not null)
			{
				// Rebind the expression to use our parameter
				var rebound = RebindExpression(mapping.SourceExpression, sourceParam);
				// Ensure type compatibility
				var convertedExpression = EnsureTypeCompatibility(rebound, destProp.PropertyType);
				bindings.Add(Expression.Bind(destProp, convertedExpression));
				continue;
			}

			// Convention-based: match by name
			if (sourceProperties.TryGetValue(destProp.Name, out var sourceProp))
			{
				var sourceAccess = Expression.Property(sourceParam, sourceProp);
				var convertedAccess = EnsureTypeCompatibility(sourceAccess, destProp.PropertyType);
				bindings.Add(Expression.Bind(destProp, convertedAccess));
			}
		}

		var memberInit = Expression.MemberInit(Expression.New(destType), bindings);
		var lambda = Expression.Lambda(memberInit, sourceParam);
		return lambda;
	}

	private static Expression RebindExpression(LambdaExpression sourceExpression, ParameterExpression newParam)
	{
		var oldParam = sourceExpression.Parameters[0];
		return new ParameterReplacer(oldParam, newParam).Visit(sourceExpression.Body);
	}

	private static Expression EnsureTypeCompatibility(Expression expression, Type targetType)
	{
		if (expression.Type == targetType)
		{
			return expression;
		}

		var sourceType = expression.Type;
		var sourceCore = Nullable.GetUnderlyingType(sourceType) ?? sourceType;
		var targetCore = Nullable.GetUnderlyingType(targetType) ?? targetType;

		// Any -> string: use ToString() (handles nullable, numeric, enum, etc.)
		if (targetCore == typeof(string))
		{
			// For nullable source, coalesce to empty string to avoid NullReferenceException
			if (Nullable.GetUnderlyingType(sourceType) is not null)
			{
				// (src.Prop == null) ? null : src.Prop.Value.ToString()
				var hasValue = Expression.Property(expression, "HasValue");
				var value = Expression.Property(expression, "Value");
				var toString = Expression.Call(value, nameof(object.ToString), Type.EmptyTypes);
				return Expression.Condition(hasValue, toString, Expression.Constant(null, typeof(string)));
			}

			return Expression.Call(expression, nameof(object.ToString), Type.EmptyTypes);
		}

		// Nullable<T> -> non-nullable value type: coalesce to default(T) so EF Core
		// generates COALESCE in SQL instead of throwing on NULL materialization
		if (sourceCore != sourceType && targetCore == targetType && targetType.IsValueType)
		{
			var coalesced = Expression.Coalesce(expression, Expression.Default(sourceCore));
			if (sourceCore == targetType)
			{
				return coalesced;
			}

			// Different value type (e.g. int? -> double): coalesce then convert
			try
			{
				return Expression.Convert(coalesced, targetType);
			}
			catch (InvalidOperationException)
			{
				return Expression.Default(targetType);
			}
		}

		// Nullable<T> -> T or T -> Nullable<T> where T is the same core type
		// or numeric/enum conversions where Expression.Convert has a CLR operator
		try
		{
			return Expression.Convert(expression, targetType);
		}
		catch (InvalidOperationException)
		{
			// No coercion operator exists (e.g. string -> double?) - skip this binding
			// by returning a default value expression for the target type
			return Expression.Default(targetType);
		}
	}

	private sealed class ParameterReplacer(ParameterExpression oldParam, ParameterExpression newParam) : ExpressionVisitor
	{
		protected override Expression VisitParameter(ParameterExpression node)
			=> node == oldParam ? newParam : base.VisitParameter(node);
	}
}