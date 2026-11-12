using System.Linq.Expressions;
using System.Reflection;

namespace PanoramicData.Mapper.QueryableExtensions;

/// <summary>
/// Extension methods for IQueryable to support projection.
/// </summary>
public static partial class Extensions
{
	private static readonly MethodInfo SelectMethod = typeof(Enumerable).GetMethods()
		.Single(m => m.Name == nameof(Enumerable.Select)
			&& m.GetParameters() is { Length: 2 } parameters
			&& parameters[1].ParameterType.IsGenericType
			&& parameters[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>));

	private static readonly MethodInfo ToListMethod = typeof(Enumerable).GetMethods()
		.Single(m => m.Name == nameof(Enumerable.ToList) && m.GetParameters().Length == 1);

	private static readonly MethodInfo ToArrayMethod = typeof(Enumerable).GetMethods()
		.Single(m => m.Name == nameof(Enumerable.ToArray) && m.GetParameters().Length == 1);

	private static readonly IReadOnlyCollection<string> NoMembers = [];

	/// <summary>
	/// Non-primitive types that a projection nevertheless treats as scalar values rather than as
	/// nested objects to expand member-by-member.
	/// </summary>
	private static readonly HashSet<Type> ScalarValueTypes =
	[
		typeof(string),
		typeof(decimal),
		typeof(DateTime),
		typeof(DateTimeOffset),
		typeof(TimeSpan),
		typeof(Guid),
		typeof(DateOnly),
		typeof(TimeOnly),
	];

	/// <summary>
	/// Projects the source queryable to the destination type using the mapper configuration.
	/// This produces an Expression that can be translated by EF Core to SQL.
	/// Nested complex members and collections of complex members are projected recursively
	/// (element-by-element) whenever a type map exists for them.
	/// </summary>
	public static IQueryable<TDestination> ProjectTo<TDestination>(
		this IQueryable source,
		IConfigurationProvider configurationProvider)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(configurationProvider);

		return ProjectToInternal<TDestination>(source, configurationProvider, NoMembers);
	}

	/// <summary>
	/// Projects the source queryable to the destination type, additionally expanding the specified
	/// members that were configured with <c>ExplicitExpansion()</c>. Members marked with
	/// <c>ExplicitExpansion()</c> are otherwise excluded from the projection.
	/// </summary>
	public static IQueryable<TDestination> ProjectTo<TDestination>(
		this IQueryable source,
		IConfigurationProvider configurationProvider,
		params Expression<Func<TDestination, object?>>[] membersToExpand)
	{
		ArgumentNullException.ThrowIfNull(source);
		ArgumentNullException.ThrowIfNull(configurationProvider);
		ArgumentNullException.ThrowIfNull(membersToExpand);

		var names = new HashSet<string>(StringComparer.Ordinal);
		foreach (var member in membersToExpand)
		{
			names.Add(GetExpandedMemberName(member));
		}

		return ProjectToInternal<TDestination>(source, configurationProvider, names);
	}

	private static IQueryable<TDestination> ProjectToInternal<TDestination>(
		IQueryable source,
		IConfigurationProvider configurationProvider,
		IReadOnlyCollection<string> membersToExpand)
	{
		var sourceType = source.ElementType;
		var destType = typeof(TDestination);

		// Build a Select expression: source.Select(s => new TDestination { Prop1 = s.Prop1, ... })
		var selectExpression = BuildMemberInitLambda(sourceType, destType, configurationProvider, [], membersToExpand);

		// Call Queryable.Select with the expression
		var selectMethod = typeof(Queryable)
			.GetMethods()
			.First(m => m.Name == nameof(Queryable.Select) && m.GetParameters().Length == 2)
			.MakeGenericMethod(sourceType, destType);

		var result = selectMethod.Invoke(null, [source, selectExpression])
			?? throw new InvalidOperationException("Failed to create projected queryable.");

		return (IQueryable<TDestination>)result;
	}

	private static string GetExpandedMemberName<TDestination>(Expression<Func<TDestination, object?>> expression)
	{
		var body = expression.Body;
		if (body is UnaryExpression { NodeType: ExpressionType.Convert, Operand: MemberExpression convertMember })
		{
			return convertMember.Member.Name;
		}

		if (body is MemberExpression member)
		{
			return member.Member.Name;
		}

		throw new ArgumentException(
			$"Expression '{expression}' must be a simple member access (e.g. d => d.Entitlements).",
			nameof(expression));
	}
}
