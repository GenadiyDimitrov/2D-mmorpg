// Polyfills needed only when compiling the netstandard2.1 target (for Unity). The record
// `init` accessors used throughout the DTOs require System.Runtime.CompilerServices.IsExternalInit,
// which exists in net5+ but NOT in netstandard2.1. This tiny shim supplies it there. On net8.0
// the real type is used and this file compiles to nothing.
#if NETSTANDARD2_1
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
#endif
