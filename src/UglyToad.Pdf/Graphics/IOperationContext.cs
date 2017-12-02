namespace UglyToad.Pdf.Graphics
{
    using IO;

    internal interface IOperationContext
    {
        CurrentGraphicsState GetCurrentState();

        TextMatrices TextMatrices { get; }

        int StackSize { get; }

        void PopState();

        void PushState();

        void ShowText(IInputBytes bytes);
    }
}