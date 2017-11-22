namespace UglyToad.Pdf.Graphics.Operations
{
    internal class ShowString : IGraphicsStateOperation
    {
        public const string Symbol = "Tj";

        public string Operator => Symbol;

        public string Text { get; }

        public ShowString(string text)
        {
            Text = text;
        }

        public override string ToString()
        {
            return $"{Text} {Symbol}";
        }
    }
}