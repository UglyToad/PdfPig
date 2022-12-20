namespace UglyToad.PdfPig.Writer;

using Core;
using System.Collections.Generic;
using System.IO;
using Tokens;

/// <summary>
/// Writes any type of <see cref="IToken"/> to the corresponding PDF document format output.
/// </summary>
public interface ITokenWriter
{
    /// <summary>
    /// Writes the given input token to the output stream with the correct PDF format and encoding including whitespace and line breaks as applicable.
    /// </summary>
    /// <param name="token">The token to write to the stream.</param>
    /// <param name="outputStream">The stream to write the token to.</param>
    void WriteToken(IToken token, Stream outputStream);

    /// <summary>
    /// Writes pre-serialized token as an object token to the output stream.
    /// </summary>
    /// <param name="objectNumber">Object number of the indirect object.</param>
    /// <param name="generation">Generation of the indirect object.</param>
    /// <param name="data">Pre-serialized object contents.</param>
    /// <param name="outputStream">The stream to write the token to.</param>
    void WriteObject(long objectNumber, int generation, byte[] data, Stream outputStream);

    /// <summary>
    /// Writes a valid single section cross-reference (xref) table plus trailer dictionary to the output for the set of object offsets.
    /// </summary>
    /// <param name="objectOffsets">The byte offset from the start of the document for each object in the document.</param>
    /// <param name="catalogToken">The object representing the catalog dictionary which is referenced from the trailer dictionary.</param>
    /// <param name="outputStream">The output stream to write to.</param>
    /// <param name="documentInformationReference">The object reference for the document information dictionary if present.</param>
    void WriteCrossReferenceTable(IReadOnlyDictionary<IndirectReference, long> objectOffsets, IndirectReference catalogToken, Stream outputStream, IndirectReference? documentInformationReference);
}