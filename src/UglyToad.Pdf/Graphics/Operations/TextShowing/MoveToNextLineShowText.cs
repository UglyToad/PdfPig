namespace UglyToad.Pdf.Graphics.Operations.TextShowing
{
    internal class MoveToNextLineShowText : IGraphicsStateOperation
    {
        public const string Symbol = "'";

        public string Operator => Symbol;

        public string Text { get; }

        public MoveToNextLineShowText(string text)
        {
            Text = text;
        }

        public override string ToString()
        {
            return $"{Text} {Symbol}";
        }
    }
}