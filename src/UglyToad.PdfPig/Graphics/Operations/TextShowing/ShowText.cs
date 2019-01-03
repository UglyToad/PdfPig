namespace UglyToad.PdfPig.Graphics.Operations.TextShowing
{
    using System;
    using System.IO;
    using IO;
    using Util;
    using Util.JetBrains.Annotations;

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
        [CanBeNull]
        public string Text { get; }

        /// <summary>
        /// The bytes of the string to show.
        /// </summary>
        [CanBeNull]
        public byte[] Bytes { get; }

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
        public ShowText(byte[] hexBytes)
        {
            Bytes = hexBytes;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            var input = new ByteArrayInputBytes(Text != null ? OtherEncodings.StringAsLatin1Bytes(Text) : Bytes);

            operationContext.ShowText(input);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            if (Text == null && Bytes != null)
            {
                throw new NotImplementedException("Support for writing hex not done yet.");
            }

            stream.WriteText($"({Text})");
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