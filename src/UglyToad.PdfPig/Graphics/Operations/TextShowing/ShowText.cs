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
    public sealed class ShowText : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "Tj";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The text string to show, or <see langword="null"/> when the operand was a hexadecimal
        /// string. Decoded from the character codes on first use.
        /// </summary>
        public string? Text => isHex ? null : text ??= OtherEncodings.BytesAsLatin1String(characterCodes.Span);

        /// <summary>
        /// The bytes of the string to show, when it was given as a hexadecimal string.
        /// </summary>
        public ReadOnlyMemory<byte> Bytes { get; }

        /// <summary>
        /// The character codes to show, whichever form the operand was given in. Held so that
        /// running the operation does not have to encode <see cref="Text"/> back to bytes.
        /// </summary>
        private readonly ReadOnlyMemory<byte> characterCodes;

        private readonly bool isHex;

        private string? text;

        /// <summary>
        /// Create a new <see cref="ShowText"/>.
        /// </summary>
        public ShowText(string text)
        {
            this.text = text;
            characterCodes = OtherEncodings.StringAsLatin1Bytes(text);
        }

        /// <summary>
        /// Create a new <see cref="ShowText"/>.
        /// </summary>
        public ShowText(ReadOnlyMemory<byte> hexBytes) : this(hexBytes, isHex: true)
        {
        }

        private ShowText(ReadOnlyMemory<byte> characterCodes, bool isHex)
        {
            this.characterCodes = characterCodes;
            this.isHex = isHex;

            if (isHex)
            {
                Bytes = characterCodes;
            }
        }

        /// <summary>
        /// Create a <see cref="ShowText"/> for a literal string operand from the character codes it
        /// was read as, so that parsing does not decode them to <see cref="Text"/> and encode them
        /// straight back. <see cref="Text"/> is decoded from them only if something asks for it.
        /// </summary>
        /// <param name="characterCodes">The character codes the operand was read as.</param>
        /// <returns>An operation showing those character codes, written back as a literal string.</returns>
        public static ShowText FromLiteralBytes(ReadOnlyMemory<byte> characterCodes)
        {
            return new ShowText(characterCodes, isHex: false);
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.ShowText(new MemoryInputBytes(characterCodes));
        }

        /// <summary>
        /// Write the character codes as a literal string, which is what they were read as.
        /// </summary>
        internal static void WriteLiteral(ReadOnlySpan<byte> characterCodes, Stream stream)
        {
            // The strings must conform to the syntax for string objects.
            // When a string is written by enclosing the data in parentheses,
            // bytes whose values are the same as those 
            // of the ASCII characters left parenthesis (40), right parenthesis (41), and backslash (92)
            // must be preceded by a backslash character.
            // All other byte values between 0 and 255 may be used in a string object.
            // These rules apply to each individual byte in a string object, whether the string is interpreted by the text-showing operators
            // as single-byte or multiple-byte character codes. 
            // An end-of-line marker inside a literal string that no backslash precedes is read back
            // as a single line feed, so a carriage return has to be escaped to survive at all and a
            // carriage return line feed pair would otherwise collapse into one byte.

            stream.WriteByte((byte)'(');

            foreach (var b in characterCodes)
            {
                switch (b)
                {
                    // Fix Issue 350 from PDF Spec 1.7 (page 408) on handling 'special characters' of '(', ')' and '\'.
                    case (byte)'\\':
                    case (byte)'(':
                    case (byte)')':
                        stream.WriteByte((byte)'\\');
                        stream.WriteByte(b);
                        break;
                    case (byte)'\r':
                        stream.WriteByte((byte)'\\');
                        stream.WriteByte((byte)'r');
                        break;
                    case (byte)'\n':
                        stream.WriteByte((byte)'\\');
                        stream.WriteByte((byte)'n');
                        break;
                    default:
                        stream.WriteByte(b);
                        break;
                }
            }

            stream.WriteByte((byte)')');
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            if (isHex)
            {
                stream.WriteHex(Bytes.Span);
            }
            else
            {
                WriteLiteral(characterCodes.Span, stream);
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
