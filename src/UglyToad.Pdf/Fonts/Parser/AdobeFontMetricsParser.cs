namespace UglyToad.Pdf.Fonts.Parser
{
    using IO;

    internal class AdobeFontMetricsParser : IAdobeFontMetricsParser
    {
        public FontMetrics Parse(IInputBytes bytes, bool useReducedDataSet)
        {
            return new FontMetrics();
        }
    }

    internal interface IAdobeFontMetricsParser
    {
        FontMetrics Parse(IInputBytes bytes, bool useReducedDataSet);
    }
}
