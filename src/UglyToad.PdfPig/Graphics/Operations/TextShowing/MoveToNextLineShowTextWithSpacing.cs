namespace UglyToad.PdfPig.Graphics.Operations.TextShowing
{
    using System.IO;
    using TextPositioning;
    using TextState;

    /// <inheritdoc />
    /// <summary>
    /// Move to the next line and show a text string, using the first number as the word spacing and the second as the character spacing.
    /// </summary>
    public class MoveToNextLineShowTextWithSpacing : IGraphicsStateOperation
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
        public byte[]? Bytes { get; }

        /// <summary>
        /// The text to show.
        /// </summary>
        public string? Text { get; }

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
            Text = text;
        }

        /// <summary>
        /// Create a new <see cref="MoveToNextLineShowTextWithSpacing"/>.
        /// </summary>
        /// <param name="wordSpacing">The word spacing.</param>
        /// <param name="characterSpacing">The character spacing.</param>
        /// <param name="hexBytes">The bytes of the text to show.</param>
        public MoveToNextLineShowTextWithSpacing(double wordSpacing, double characterSpacing, byte[] hexBytes)
        {
            WordSpacing = wordSpacing;
            CharacterSpacing = characterSpacing;
            Bytes = hexBytes;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            var setWordSpacing = new SetWordSpacing(WordSpacing);
            var setCharacterSpacing = new SetCharacterSpacing(CharacterSpacing);
            var moveToNextLine = MoveToNextLine.Value;
            var showText = Text != null ? new ShowText(Text) : new ShowText(Bytes!);

            setWordSpacing.Run(operationContext);
            setCharacterSpacing.Run(operationContext);
            moveToNextLine.Run(operationContext);
            showText.Run(operationContext);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteDouble(WordSpacing);
            stream.WriteWhiteSpace();
            stream.WriteDouble(CharacterSpacing);
            stream.WriteWhiteSpace();

            if (Bytes != null)
            {
                stream.WriteHex(Bytes);
            }
            else
            {
                stream.WriteText($"({Text})");
            }

            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{WordSpacing} {CharacterSpacing} {Text} {Symbol}";
        }
    }
}