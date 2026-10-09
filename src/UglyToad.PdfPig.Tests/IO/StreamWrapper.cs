namespace UglyToad.PdfPig.IO
{
    using System;
    using System.IO;

    /// <summary>Forwards stream operations for the unchanged master CCITT decoder used by tests.</summary>
    /// <remarks>
    /// <para>Forwarding implementation from the Apache-2.0 PdfPig
    /// <see href="https://github.com/UglyToad/PdfPig/blob/bdbc5f47fdbca11542db7ee876426ee601374427/src/UglyToad.PdfPig/IO/StreamWrapper.cs">pinned master</see>.</para>
    /// <para>Read forwards to the wrapped input unless a derived decoder overrides that overload.
    /// On modern targets Span Read forwards directly, so master comparisons must call the decoder's
    /// array Read overload.</para>
    /// </remarks>
    internal class StreamWrapper : Stream
    {
        protected readonly Stream Stream;

        public StreamWrapper(Stream stream)
        {
            Stream = stream;
        }

        public override void Flush()
        {
            Stream.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return Stream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            Stream.SetLength(value);
        }

#if NET
        public override int Read(Span<byte> buffer)
        {
            return Stream.Read(buffer);
        }
#endif

        public override int Read(byte[] buffer, int offset, int count)
        {
            return Stream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Stream.Write(buffer, offset, count);
        }

#if NET
        public override void Write(ReadOnlySpan<byte> buffer)
        {
            Stream.Write(buffer);
        }
#endif

        public override bool CanRead => Stream.CanRead;

        public override bool CanSeek => Stream.CanSeek;

        public override bool CanWrite => Stream.CanWrite;

        public override long Length => Stream.Length;

        public override long Position
        {
            get => Stream.Position;
            set => Stream.Position = value;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            Stream?.Dispose();
        }
    }
}
