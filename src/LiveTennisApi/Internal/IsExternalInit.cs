// The `init` accessor and record types (C# 9) require the
// System.Runtime.CompilerServices.IsExternalInit marker type, which ships in
// net5.0+ but not in netstandard2.0. Provide it there so the same records
// compile against the older target.
#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}
#endif
