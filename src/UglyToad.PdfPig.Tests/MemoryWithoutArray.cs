namespace UglyToad.PdfPig.Tests
{
    using System.Buffers;

    /// <summary>
    /// Memory that gives out no array, as native memory does.
    /// </summary>
    internal sealed class MemoryWithoutArray(byte[] bytes) : MemoryManager<byte>
    {
        public override Span<byte> GetSpan() => bytes;

        public override MemoryHandle Pin(int elementIndex = 0) => throw new NotSupportedException();

        public override void Unpin()
        {
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}
