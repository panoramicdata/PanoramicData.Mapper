// Polyfill for netstandard2.0: required to use 'record' and 'record struct' types with C# 9+
#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices;

internal static class IsExternalInit { }
#endif
