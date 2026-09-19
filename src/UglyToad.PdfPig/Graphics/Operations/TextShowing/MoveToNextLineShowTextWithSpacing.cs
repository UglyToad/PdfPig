namespace UglyToad.PdfPig.Graphics.Operations.TextShowing
{
    using System.IO;
    using PdfPig.Core;
    using TextPositioning;

    /// <inheritdoc />
    /// <summary>
    /// Move to the next line and show a text string, using the first number as the word spacing and the second as the character spacing.
    /// </summary>
    public sealed class MoveToNextLineShowTextWithSpacing : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "\"";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The word spacing.
        /// </summary>
        public double WordSpacing { get; }

        /// <summary>
        /// The character spacing.
        /// </summary>
        public double CharacterSpacing { get; }

        /// <summary>
        /// The bytes of the text.
        /// </summary>
        public ReadOnlyMemory<byte> Bytes { get; }

        /// <summary>
        /// The text to show, or <see langword="null"/> when the operand was a hexadecimal string.
        /// Decoded from the character codes on first use.
        /// </summary>
        public string? Text => isHex ? null : text ??= OtherEncodings.BytesAsLatin1String(characterCodes.Span);

        /// <summary>
        /// The character codes to show, whichever form the operand was given in.
        /// </summary>
        private readonly ReadOnlyMemory<byte> characterCodes;

        private readonly bool isHex;

        private string? text;

        /// <summary>
        /// Create a new <see cref="MoveToNextLineShowTextWithSpacing"/>.
        /// </summary>
        /// <param name="wordSpacing">The word spacing.</param>
        /// <param name="characterSpacing">The character spacing.</param>
        /// <param name="text">The text to show.</param>
        public MoveToNextLineShowTextWithSpacing(double wordSpacing, double characterSpacing, string text)
        {
            WordSpacing = wordSpacing;
            CharacterSpacing = characterSpacing;
            this.text = text;
            characterCodes = OtherEncodings.StringAsLatin1Bytes(text);
        }

        /// <summary>
        /// Create a new <see cref="MoveToNextLineShowTextWithSpacing"/>.
        /// </summary>
        /// <param name="wordSpacing">The word spacing.</param>
        /// <param name="characterSpacing">The character spacing.</param>
        /// <param name="hexBytes">The bytes of the text to show.</param>
        public MoveToNextLineShowTextWithSpacing(double wordSpacing, double characterSpacing, ReadOnlyMemory<byte> hexBytes)
            : this(wordSpacing, characterSpacing, hexBytes, isHex: true)
        {
        }

        private MoveToNextLineShowTextWithSpacing(double wordSpacing, double characterSpacing,
            ReadOnlyMemory<byte> characterCodes, bool isHex)
        {
            WordSpacing = wordSpacing;
            CharacterSpacing = characterSpacing;
            this.characterCodes = characterCodes;
            this.isHex = isHex;

            if (isHex)
            {
                Bytes = characterCodes;
            }
        }

        /// <summary>
        /// Create a <see cref="MoveToNextLineShowTextWithSpacing"/> for a literal string operand from
        /// the character codes it was read as. See <see cref="ShowText.FromLiteralBytes"/>.
        /// </summary>
        /// <param name="wordSpacing">The word spacing.</param>
        /// <param name="characterSpacing">The character spacing.</param>
        /// <param name="characterCodes">The character codes the operand was read as.</param>
        /// <returns>An operation showing those character codes, written back as a literal string.</returns>
        public static MoveToNextLineShowTextWithSpacing FromLiteralBytes(double wordSpacing,
            double characterSpacing, ReadOnlyMemory<byte> characterCodes)
        {
            return new MoveToNextLineShowTextWithSpacing(wordSpacing, characterSpacing, characterCodes, isHex: false);
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            // This is what running a SetWordSpacing and a SetCharacterSpacing amounts to, without
            // allocating one of each to do it.
            operationContext.SetWordSpacing(WordSpacing);
            operationContext.SetCharacterSpacing(CharacterSpacing);

            MoveToNextLine.Value.Run(operationContext);

            operationContext.ShowText(new MemoryInputBytes(characterCodes));
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteDouble(WordSpacing);
            stream.WriteWhiteSpace();
            stream.WriteDouble(CharacterSpacing);
            stream.WriteWhiteSpace();

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
            return $"{WordSpacing} {CharacterSpacing} {Text} {Symbol}";
        }
    }
}
