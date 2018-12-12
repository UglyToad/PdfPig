namespace UglyToad.PdfPig.Graphics
{
    using System.Collections.Generic;
    using Fonts;
    using Geometry;
    using IO;
    using Tokens;
    using Util.JetBrains.Annotations;

    internal interface IOperationContext
    {
        [CanBeNull]
        PdfPath CurrentPath { get; }

        PdfPoint CurrentPosition { get; set; }

        CurrentGraphicsState GetCurrentState();

        TextMatrices TextMatrices { get; }

        int StackSize { get; }

        void PopState();

        void PushState();

        void ShowText(IInputBytes bytes);

        void ShowPositionedText(IReadOnlyList<IToken> tokens);

        void ApplyXObject(StreamToken xObjectStream);

        void BeginSubpath();

        void StrokePath(bool close);

        void ClosePath();
    }
}