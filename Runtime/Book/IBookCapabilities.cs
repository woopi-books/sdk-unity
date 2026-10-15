using System.Collections.Generic;

namespace Woopi.Core.Book
{
    /// <summary>
    /// Optional. An adapter implements this next to <see cref="IBookAdapter"/> to tell the host page
    /// about book-specific abilities (for example "dark-mode"). They are added to the
    /// <c>capabilities</c> list of the <c>ready</c> event. <see cref="IBookAdapter"/> itself stays
    /// small and stable; new optional abilities arrive as separate interfaces like this one, so
    /// existing adapters keep compiling.
    /// </summary>
    public interface IBookCapabilities
    {
        IReadOnlyList<string> Capabilities { get; }
    }
}
