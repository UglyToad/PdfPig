namespace UglyToad.PdfPig.Graphics.Operations.General
{
    using System;
    using System.IO;
    using Tokens;
    using UglyToad.PdfPig.Graphics.Core;

    /// <inheritdoc />
    /// <summary>
    /// Set the color rendering intent in the graphics state.
    /// </summary>
    public class SetColorRenderingIntent : IGraphicsStateOperation
    {
        /// <summary>
        /// The symbol for this operation in a stream.
        /// </summary>
        public const string Symbol = "ri";

        /// <inheritdoc />
        public string Operator => Symbol;

        /// <summary>
        /// The rendering intent for CIE-based colors.
        /// </summary>
        public NameToken RenderingIntent { get; }

        /// <summary>
        /// Create new <see cref="SetColorRenderingIntent"/>.
        /// </summary>
        /// <param name="renderingIntent">The rendering intent.</param>
        public SetColorRenderingIntent(NameToken renderingIntent)
        {
            RenderingIntent = renderingIntent ?? throw new ArgumentNullException(nameof(renderingIntent));
        }

        /// <inheritdoc />
        public void Run(IOperationContext operationContext)
        {
            operationContext.GetCurrentState().RenderingIntent = RenderingIntentExtensions.ToRenderingIntent(RenderingIntent);
        }

        /// <inheritdoc />
        public void Write(Stream stream)
        {
            stream.WriteText($"/{RenderingIntent.Data} {Symbol}");
            stream.WriteNewLine();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{RenderingIntent} {Symbol}";
        }
    }
}