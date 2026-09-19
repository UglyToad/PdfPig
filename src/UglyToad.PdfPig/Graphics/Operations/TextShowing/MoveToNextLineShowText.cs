namespace UglyToad.PdfPig.Graphics.Operations.TextShowing
{
    using System.IO;
    using PdfPig.Core;
    using TextPositioning;

    /// <inheritdoc />
    /// <summary>
    /// Move to the next line and show a text string.
    /// </summary>
    public sealed class MoveToNextLineShowText : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "'";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The text to show as a <see cref="string"/>, or <see langword="null"/> when the operand
        /// was a hexadecimal string. Decoded from the character codes on first use.
        /// </summary>
        public string? Text => isHex ? null : text ??= OtherEncodings.BytesAsLatin1String(characterCodes.Span);

        /// <summary>
        /// The text to show as hex bytes.
        /// </summary>
        public ReadOnlyMemory<byte> Bytes { get; }

        /// <summary>
        /// The character codes to show, whichever form the operand was given in.
        /// </summary>
        private readonly ReadOnlyMemory<byte> characterCodes;

        private readonly bool isHex;

        private string? text;

        /// <summary>
        /// Create a new <see cref="MoveToNextLineShowText"/>.
        /// </summary>
        /// <param name="text">The text to show.</param>
        public MoveToNextLineShowText(string text)
        {
            this.text = text;
            characterCodes = OtherEncodings.StringAsLatin1Bytes(text);
        }

        /// <summary>
        /// Create a new <see cref="MoveToNextLineShowText"/>.
        /// </summary>
        /// <param name="hexBytes">The bytes of the text to show.</param>
        public MoveToNextLineShowText(ReadOnlyMemory<byte> hexBytes) : this(hexBytes, isHex: true)
        {
        }

        private MoveToNextLineShowText(ReadOnlyMemory<byte> characterCodes, bool isHex)
        {
            this.characterCodes = characterCodes;
            this.isHex = isHex;

            if (isHex)
            {
                Bytes = characterCodes;
            }
        }

        /// <summary>
        /// Create a <see cref="MoveToNextLineShowText"/> for a literal string operand from the
        /// character codes it was read as. See <see cref="ShowText.FromLiteralBytes"/>.
        /// </summary>
        /// <param name="characterCodes">The character codes the operand was read as.</param>
        /// <returns>An operation showing those character codes, written back as a literal string.</returns>
        public static MoveToNextLineShowText FromLiteralBytes(ReadOnlyMemory<byte> characterCodes)
        {
            return new MoveToNextLineShowText(characterCodes, isHex: false);
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            MoveToNextLine.Value.Run(operationContext);
            operationContext.ShowText(new MemoryInputBytes(characterCodes));
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
                ShowText.WriteLiteral(characterCodes.Span, stream);
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
