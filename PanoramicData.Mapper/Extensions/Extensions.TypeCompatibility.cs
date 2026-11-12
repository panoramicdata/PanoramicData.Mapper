using System.Linq.Expressions;

namespace PanoramicData.Mapper.QueryableExtensions;

/// <summary>
/// Coerces projected scalar values to the destination member type.
/// </summary>
public static partial class Extensions
{
	private static Expression RebindExpression(LambdaExpression sourceExpression, Expression replacement)
	{
		var oldParam = sourceExpression.Parameters[0];
		return new ParameterReplacer(oldParam, replacement).Visit(sourceExpression.Body);
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
			return ConvertToString(expression, sourceType);
		}

		// Nullable<T> -> non-nullable value type: coalesce to default(T) so EF Core
		// generates COALESCE in SQL instead of throwing on NULL materialization
		if (NeedsNullCoalescing(sourceType, sourceCore, targetType, targetCore))
		{
			return CoalesceNullableSource(expression, sourceCore, targetType);
		}

		// An interface target that the source does not implement would compile to a reference cast
		// that throws InvalidCastException at materialization. Leave the member at its default
		// instead (complex collections/objects are handled earlier via TryBuildComplexBinding).
		if (IsUnimplementedInterface(sourceType, targetType))
		{
			return Expression.Default(targetType);
		}

		// Nullable<T> -> T or T -> Nullable<T> where T is the same core type
		// or numeric/enum conversions where Expression.Convert has a CLR operator
		return TryConvert(expression, targetType);
	}

	/// <summary>
	/// A Nullable&lt;T&gt; source feeding a non-nullable value-type target has to be coalesced, or
	/// a NULL row throws on materialization instead of yielding default(T).
	/// </summary>
	private static bool NeedsNullCoalescing(Type sourceType, Type sourceCore, Type targetType, Type targetCore)
		=> sourceCore != sourceType && targetCore == targetType && targetType.IsValueType;

	/// <summary>
	/// An interface target the source type does not implement, which would otherwise compile to a
	/// reference cast that only fails once EF Core materializes the row.
	/// </summary>
	private static bool IsUnimplementedInterface(Type sourceType, Type targetType)
		=> targetType.IsInterface && !targetType.IsAssignableFrom(sourceType);

	private static Expression ConvertToString(Expression expression, Type sourceType)
	{
		if (Nullable.GetUnderlyingType(sourceType) is null)
		{
			return Expression.Call(expression, nameof(object.ToString), Type.EmptyTypes);
		}

		// A nullable source needs the ToString() call guarded, or a NULL materialises as a
		// NullReferenceException: (src.Prop == null) ? null : src.Prop.Value.ToString()
		var hasValue = Expression.Property(expression, "HasValue");
		var value = Expression.Property(expression, "Value");
		var toString = Expression.Call(value, nameof(object.ToString), Type.EmptyTypes);
		return Expression.Condition(hasValue, toString, Expression.Constant(null, typeof(string)));
	}

	private static Expression CoalesceNullableSource(Expression expression, Type sourceCore, Type targetType)
	{
		var coalesced = Expression.Coalesce(expression, Expression.Default(sourceCore));

		// A different value type (e.g. int? -> double) still needs converting after the coalesce.
		return sourceCore == targetType
			? coalesced
			: TryConvert(coalesced, targetType);
	}

	/// <summary>
	/// Converts where a CLR coercion operator exists. Where none does (e.g. string -&gt; double?)
	/// the binding is skipped by falling back to the target type's default.
	/// </summary>
	private static Expression TryConvert(Expression expression, Type targetType)
	{
		try
		{
			return Expression.Convert(expression, targetType);
		}
		catch (InvalidOperationException)
		{
			return Expression.Default(targetType);
		}
	}

	private sealed class ParameterReplacer(ParameterExpression oldParam, Expression replacement) : ExpressionVisitor
	{
		protected override Expression VisitParameter(ParameterExpression node)
			=> node == oldParam ? replacement : base.VisitParameter(node);
	}
}
