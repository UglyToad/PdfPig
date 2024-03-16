namespace UglyToad.PdfPig.Graphics.Operations.TextState
{
    using System;
    using System.Globalization;
    using System.IO;
    using Tokens;
    using Util.JetBrains.Annotations;

    /// <inheritdoc />
    /// <summary>
    /// Set the font and the font size.
    /// Font is the name of a font resource in the Font subdictionary of the current resource dictionary.
    /// Size is a number representing a scale factor.
    /// </summary>
    public class SetFontAndSize : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "Tf";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The name of the font as defined in the resource dictionary.
        /// </summary>
        public NameToken Font { get; }

        /// <summary>
        /// The font program defines glyphs for a standard size. This standard size is set so that each line of text will occupy 1 unit in user space.
        /// The size is the scale factor used to scale glyphs from the standard size to the display size rather than the font size in points.
        /// </summary>
        public double Size { get; }

        /// <summary>
        /// Create a new <see cref="SetFontAndSize"/>.
        /// </summary>
        /// <param name="font">The font name.</param>
        /// <param name="size">The font size.</param>
        public SetFontAndSize(NameToken font, double size)
        {
            Font = font ?? throw new ArgumentNullException(nameof(font));
            Size = size;
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.SetFontAndSize(Font, Size);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteText(Font.ToString());
            stream.WriteWhiteSpace();
            stream.WriteNumberText(Size, Symbol);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Font} {Size.ToString("G", CultureInfo.InvariantCulture)} {Symbol}";
        }
    }
}