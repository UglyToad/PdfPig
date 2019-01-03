namespace UglyToad.PdfPig.Graphics.Operations.TextShowing
{
    using System;
    using System.IO;
    using System.Linq;
    using TextPositioning;
    using Util.JetBrains.Annotations;

    /// <inheritdoc />
    /// <summary>
    /// Move to the next line and show a text string.
    /// </summary>
    public class MoveToNextLineShowText : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "'";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The text to show as a <see cref="string"/>.
        /// </summary>
        [CanBeNull]
        public string Text { get; }

        /// <summary>
        /// The text to show as hex bytes.
        /// </summary>
        [CanBeNull]
        public byte[] Bytes { get; }

        /// <summary>
        /// Create a new <see cref="MoveToNextLineShowText"/>.
        /// </summary>
        /// <param name="text">The text to show.</param>
        public MoveToNextLineShowText(string text)
        {
            Text = text;
        }
        
        /// <summary>
        /// Create a new <see cref="MoveToNextLineShowText"/>.
        /// </summary>
        /// <param name="hexBytes">The bytes of the text to show.</param>
        public MoveToNextLineShowText(byte[] hexBytes)
        {
            Bytes = hexBytes;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            var move = MoveToNextLine.Value;

            var showText = Text != null ? new ShowText(Text) : new ShowText(Bytes);

            move.Run(operationContext);
            showText.Run(operationContext);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            if (Bytes == null)
            {
                stream.WriteText($"({Text}) {Symbol}");
                stream.WriteNewLine();
            }
            else
            {
                var hex = BitConverter.ToString(Bytes.ToArray()).Replace("-", string.Empty);
                stream.WriteText($"<{hex}> {Symbol}");
                stream.WriteNewLine();
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Text} {Symbol}";
        }
    }
}