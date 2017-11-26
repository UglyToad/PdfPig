namespace UglyToad.Pdf.Graphics.Operations.TextShowing
{
    internal class MoveToNextLineShowTextWithSpacing : IGraphicsStateOperation
    {
        public const string Symbol = "\"";

        public string Operator => Symbol;

        public decimal WordSpacing { get; }

        public decimal CharacterSpacing { get; }

        public string Text { get; }

        public MoveToNextLineShowTextWithSpacing(decimal wordSpacing, decimal characterSpacing, string text)
        {
            WordSpacing = wordSpacing;
            CharacterSpacing = characterSpacing;
            Text = text;
        }

        public override string ToString()
        {
            return $"{WordSpacing} {CharacterSpacing} {Text} {Symbol}";
        }
    }
}