namespace UglyToad.PdfPig.Graphics.Operations.TextShowing
{
    using PdfPig.Core;
    using System;
    using System.IO;

    /// <inheritdoc />
    /// <summary>
    /// Show a text string
    /// </summary>
    /// <remarks>
    /// <para>The input is a sequence of character codes to be shown as glyphs.</para>
    /// <para>
    /// Generally each byte represents a single character code, however starting in version 1.2+
    /// a composite font might use multi-byte character codes to map to glyphs.
    /// For these composite fonts, the <see cref="T:UglyToad.PdfPig.Fonts.Cmap.CMap" /> of the font defines the mapping from code to glyph.
    /// </para>
    /// <para>
    /// The grouping of character codes in arguments to this operator does not have any impact on the meaning; for example:<br />
    /// (Abc) Tj is equivalent to (A) Tj (b) Tj (c) Tj<br />
    /// However grouping character codes makes the document easier to search and extract text from.
    /// </para>
    /// </remarks>
    public class ShowText : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "Tj";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The text string to show.
        /// </summary>
        public string? Text { get; }

        /// <summary>
        /// The bytes of the string to show.
        /// </summary>
        public ReadOnlyMemory<byte> Bytes { get; }

        /// <summary>
        /// Create a new <see cref="ShowText"/>.
        /// </summary>
        public ShowText(string text)
        {
            Text = text;
        }

        /// <summary>
        /// Create a new <see cref="ShowText"/>.
        /// </summary>
        public ShowText(ReadOnlyMemory<byte> hexBytes)
        {
            Bytes = hexBytes;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            var input = new MemoryInputBytes(Text != null ? OtherEncodings.StringAsLatin1Bytes(Text) : Bytes);
            operationContext.ShowText(input);
        }

        string? EscapeText(string? text)
        {
            if (text is null) return null;
            // Fix Issue 350 from PDF Spec 1.7 (page 408) on handling 'special characters' of '(', ')' and '\'.

            // The strings must conform to the syntax for string objects.
            // When a string is written by enclosing the data in parentheses,
            // bytes whose values are the same as those 
            // of the ASCII characters left parenthesis (40), right parenthesis (41), and backslash (92)
            // must be preceded by a backslash character.
            // All other byte values between 0 and 255 may be used in a string object.
            // These rules apply to each individual byte in a string object, whether the string is interpreted by the text-showing operators
            // as single-byte or multiple-byte character codes. 

            // Note: order of replacing is important. Replace slash first before brackets.
            text = text.Replace(@"\", @"\\");  // Escape any slash          '\'  -> '\\'
            text = text.Replace("(", @"\(");    // Escape any open  brackets '('  -> '\('
            text = text.Replace(")", @"\)");    // Escape any close brackets ')'  -> '\)'

            return text;
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            if (!Bytes.IsEmpty)
            {
                stream.WriteHex(Bytes.Span);
            }
            else
            {
                var escapedText = EscapeText(Text);     // escape '(', ')' or '\'
                stream.WriteText($"({escapedText})");
            }

            stream.WriteWhiteSpace();
            stream.WriteText(Symbol);
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Text} {Symbol}";
        }
    }
}
