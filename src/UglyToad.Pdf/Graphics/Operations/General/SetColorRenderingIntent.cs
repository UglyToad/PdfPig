namespace UglyToad.Pdf.Graphics.Operations.General
{
    internal class SetColorRenderingIntent : IGraphicsStateOperation
    {
        public const string Symbol = "ri";

        public string Operator => Symbol;
    }
}