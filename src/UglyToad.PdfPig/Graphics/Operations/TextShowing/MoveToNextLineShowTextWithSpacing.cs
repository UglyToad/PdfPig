namespace UglyToad.PdfPig.Graphics.Operations.TextShowing
{
    using System.IO;
    using Content;
    using TextPositioning;
    using TextState;
    using Util.JetBrains.Annotations;

    internal class MoveToNextLineShowTextWithSpacing : IGraphicsStateOperation
    {
        public const string Symbol = "\"";

        public string Operator => Symbol;

        public decimal WordSpacing { get; }

        public decimal CharacterSpacing { get; }

        [CanBeNull]
        public byte[] Bytes { get; }

        [CanBeNull]
        public string Text { get; }

        public MoveToNextLineShowTextWithSpacing(decimal wordSpacing, decimal characterSpacing, string text)
        {
            WordSpacing = wordSpacing;
            CharacterSpacing = characterSpacing;
            Text = text;
        }

        public MoveToNextLineShowTextWithSpacing(decimal wordSpacing, decimal characterSpacing, byte[] hexBytes)
        {
            WordSpacing = wordSpacing;
            CharacterSpacing = characterSpacing;
            Bytes = hexBytes;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            var setWordSpacing = new SetWordSpacing(WordSpacing);
            var setCharacterSpacing = new SetCharacterSpacing(CharacterSpacing);
            var moveToNextLine = MoveToNextLine.Value;
            var showText = Text != null ? new ShowText(Text) : new ShowText(Bytes);

            setWordSpacing.Run(operationContext, resourceStore);
            setCharacterSpacing.Run(operationContext, resourceStore);
            moveToNextLine.Run(operationContext, resourceStore);
            showText.Run(operationContext, resourceStore);
        }

        public void Write(Stream stream)
        {
            throw new System.NotImplementedException();
        }

        public override string ToString()
        {
            return $"{WordSpacing} {CharacterSpacing} {Text} {Symbol}";
        }
    }
}