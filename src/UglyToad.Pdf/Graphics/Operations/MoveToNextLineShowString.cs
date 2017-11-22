namespace UglyToad.Pdf.Graphics.Operations
{
    internal class MoveToNextLineShowString : IGraphicsStateOperation
    {
        public const string Symbol = "'";

        public string Operator => Symbol;

        public string Text { get; }

        public MoveToNextLineShowString(string text)
        {
            Text = text;
        }

        public override string ToString()
        {
            return $"{Text} {Symbol}";
        }
    }
}