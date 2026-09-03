using PanoramicData.Mapper.Configuration.Annotations;
using System.Linq.Expressions;
using System.Reflection;

namespace PanoramicData.Mapper.Internal;

/// <summary>
/// Reports members that the configuration leaves unmapped, backing
/// <c>AssertConfigurationIsValid()</c>.
/// </summary>
public sealed partial class TypeMap
{
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
}
