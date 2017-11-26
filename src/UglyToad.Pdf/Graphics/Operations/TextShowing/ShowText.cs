namespace UglyToad.Pdf.Graphics.Operations.TextShowing
{
    internal class ShowText : IGraphicsStateOperation
    {
        public const string Symbol = "Tj";

        public string Operator => Symbol;

        public string Text { get; }

        public ShowText(string text)
        {
            Text = text;
        }

        public override string ToString()
        {
            return $"{Text} {Symbol}";
        }
    }
}