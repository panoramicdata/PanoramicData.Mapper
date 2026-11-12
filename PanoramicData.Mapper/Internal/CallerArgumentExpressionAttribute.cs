#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices;

/// <summary>
/// Lets CallerArgumentExpression compile on targets that predate it.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
internal sealed class CallerArgumentExpressionAttribute(string parameterName) : Attribute
{
	public string ParameterName { get; } = parameterName;
}
#endif
