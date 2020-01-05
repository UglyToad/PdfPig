namespace UglyToad.PdfPig.Core
{
    using System.IO;

    /// <summary>
    /// Indicates that a data structure can be written to an output stream.
    /// </summary>
    public interface IWriteable
    {
        /// <summary>
        /// Write the data to the output stream.
        /// </summary>
        void Write(Stream stream);
    }
}
