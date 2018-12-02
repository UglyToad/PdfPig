namespace UglyToad.PdfPig.Graphics.Operations.TextShowing
{
    using System.IO;
    using Content;
    using TextPositioning;
    using Util.JetBrains.Annotations;

    internal class MoveToNextLineShowText : IGraphicsStateOperation
    {
        public const string Symbol = "'";

        public string Operator => Symbol;

        [CanBeNull]
        public string Text { get; }

        [CanBeNull]
        public byte[] Bytes { get; }

        public MoveToNextLineShowText(string text)
        {
            Text = text;
        }

        public MoveToNextLineShowText(byte[] hexBytes)
        {
            Bytes = hexBytes;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var move = MoveToNextLine.Value;

            var showText = Text != null ? new ShowText(Text) : new ShowText(Bytes);

            move.Run(operationContext, resourceStore);
            showText.Run(operationContext, resourceStore);
        }

        public void Write(Stream stream)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"{Text} {Symbol}";
        }
    }
}