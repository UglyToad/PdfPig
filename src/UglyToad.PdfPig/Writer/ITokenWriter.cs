namespace UglyToad.PdfPig.Writer;

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
}