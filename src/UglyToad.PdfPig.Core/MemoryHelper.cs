namespace UglyToad.PdfPig.Core
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Memory extensions.
    /// </summary>
    public static class MemoryHelper
    {
        /// <summary>
        /// Gets a read-only <see cref="MemoryStream"/> from a ReadOnlyMemory&lt;byte&gt;, attempting without memory allocation.
        /// </summary>
        public static MemoryStream AsReadOnlyMemoryStream(this ReadOnlyMemory<byte> memory)
        {
            if (MemoryMarshal.TryGetArray(memory, out ArraySegment<byte> array))
            {
                return new MemoryStream(array.Array!, array.Offset, array.Count, false);
            }

            return new MemoryStream(memory.ToArray(), false);
        }

        /// <summary>
        /// Gets the Memory&lt;byte&gt; from a <see cref="MemoryStream"/>, attempting without memory allocation.
        /// </summary>
        public static Memory<byte> AsMemory(this MemoryStream stream)
        {
            if (stream.TryGetBuffer(out ArraySegment<byte> segment))
            {
                return segment.AsMemory();
            }

            return stream.ToArray();
        }
    }
}
