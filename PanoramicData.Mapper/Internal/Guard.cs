using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace PanoramicData.Mapper.Internal;

/// <summary>
/// Argument checks that behave the same on every target framework.
/// </summary>
internal static class Guard
{
	internal static void NotNull([NotNull] object? value, [CallerArgumentExpression(nameof(value))] string? parameterName = null)
	{
		if (value is null)
		{
			throw new ArgumentNullException(parameterName);
		}
	}
}
