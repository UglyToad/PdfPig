namespace UglyToad.PdfPig.Graphics
{
    using System.Collections.Generic;
    using IO;
    using Tokenization.Tokens;

    internal interface IOperationContext
    {
        CurrentGraphicsState GetCurrentState();

        TextMatrices TextMatrices { get; }

        int StackSize { get; }

        void PopState();

        void PushState();

        void ShowText(IInputBytes bytes);

        void ShowPositionedText(IReadOnlyList<IToken> tokens);

        void ApplyXObject(StreamToken xObjectStream);
    }
}