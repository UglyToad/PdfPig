namespace UglyToad.PdfPig.IO
{
    using System;

    /// <inheritdoc />
    /// <summary>
    /// The input bytes for a PDF document.
    /// </summary>
    public interface IInputBytes : IDisposable
    {
        /// <summary>
        /// The current offset in bytes.
        /// </summary>
        long CurrentOffset { get; }

        /// <summary>
        /// Moves to the next byte if available.
        /// </summary>
        bool MoveNext();

        /// <summary>
        /// The current byte.
        /// </summary>
        byte CurrentByte { get; }

        /// <summary>
        /// The length of the data in bytes.
        /// </summary>
        long Length { get; }

        /// <summary>
        /// Returns the next byte if available.
        /// </summary>
        byte? Peek();
        
        /// <summary>
        /// Whether we are at the end of the available data.
        /// </summary>
        bool IsAtEnd();

        /// <summary>
        /// Move to a given position.
        /// </summary>
        void Seek(long position);
    }
}