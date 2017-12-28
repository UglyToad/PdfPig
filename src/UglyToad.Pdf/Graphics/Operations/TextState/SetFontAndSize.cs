namespace UglyToad.Pdf.Graphics.Operations.TextState
{
    using System;
    using Content;
    using Cos;
    using Util.JetBrains.Annotations;

    internal class SetFontAndSize : IGraphicsStateOperation
    {
        public const string Symbol = "Tf";

        public string Operator => Symbol;

        /// <summary>
        /// The name of the font as defined in the resource dictionary.
        /// </summary>
        [NotNull]
        public CosName Font { get; }

        /// <summary>
        /// The font program defines glyphs for a standard size. This standard size is set so that each line of text will occupy 1 unit in user space.
        /// The size is the scale factor used to scale glyphs from the standard size to the display size rather than the font size in points.
        /// </summary>
        public decimal Size { get; }

        public SetFontAndSize(CosName font, decimal size)
        {
            Font = font ?? throw new ArgumentNullException(nameof(font));
            Size = size;
        }
        
        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var currentState = operationContext.GetCurrentState();

            currentState.FontState.FontSize = Size;
            currentState.FontState.FontName = Font;
        }

        public override string ToString()
        {
            return $"{Font} {Size} {Symbol}";
        }
    }
}