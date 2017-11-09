namespace UglyToad.Pdf
{
    using Logging;

    public class ParsingOptions
    {
        public bool UseLenientParsing { get; set; } = true;

        public ILog Logger { get; set; } = new NoOpLog();
    }
}