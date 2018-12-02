namespace UglyToad.PdfPig.Graphics.Operations.TextShowing
{
    using System;
    using System.IO;
    using Content;
    using IO;
    using Util;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Show a text string
    /// </summary>
    /// <remarks>
    /// <para>The input is a sequence of character codes to be shown as glyphs.</para>
    /// <para>
    /// Generally each byte represents a single character code, however starting in version 1.2+
    /// a composite font might use multi-byte character codes to map to glyphs.
    /// For these composite fonts, the <see cref="Fonts.Cmap.CMap"/> of the font defines the mapping from code to glyph.
    /// </para>
    /// <para>
    /// The grouping of character codes in arguments to this operator does not have any impact on the meaning; for example:<br/>
    /// (Abc) Tj is equivalent to (A) Tj (b) Tj (c) Tj<br/>
    /// However grouping character codes makes the document easier to search and extract text from.
    /// </para>
    /// </remarks>
    internal class ShowText : IGraphicsStateOperation
    {
        public const string Symbol = "Tj";

        public string Operator => Symbol;

        [CanBeNull]
        public string Text { get; }

        [CanBeNull]
        public byte[] Bytes { get; }

        public ShowText(string text)
        {
            Text = text;
        }

        public ShowText(byte[] hexBytes)
        {
            Bytes = hexBytes;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var input = new ByteArrayInputBytes(Text != null ? OtherEncodings.StringAsLatin1Bytes(Text) : Bytes);

            operationContext.ShowText(input);
        }

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

        public override string ToString()
        {
            return $"{Text} {Symbol}";
        }
    }
}