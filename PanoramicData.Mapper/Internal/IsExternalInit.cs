#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices;

/// <summary>
/// Lets init accessors and record structs compile on targets that predate them.
/// </summary>
internal static class IsExternalInit
{
}
#endif
