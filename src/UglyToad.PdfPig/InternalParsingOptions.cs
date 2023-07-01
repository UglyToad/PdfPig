namespace UglyToad.PdfPig
{
    using Logging;
    using System.Collections.Generic;

    /// <summary>
    /// <see cref="ParsingOptions"/> but without being a public API.
    /// </summary>
    internal class InternalParsingOptions : IParsingOptions
    {
        public List<string> Passwords { get; }

        public bool UseLenientParsing { get; }

        public bool ClipPaths { get; }

        public bool SkipMissingFonts { get; }

        public bool SkipMissingXObjects { get; }

        public ILog Logger { get; }

        public InternalParsingOptions(
            List<string> passwords,
            bool useLenientParsing,
            bool clipPaths,
            bool skipMissingFonts,
            bool skipMissingXObjects,
            ILog logger)
        {
            Passwords = passwords;
            UseLenientParsing = useLenientParsing;
            ClipPaths = clipPaths;
            SkipMissingFonts = skipMissingFonts;
            SkipMissingXObjects = skipMissingXObjects;
            Logger = logger;
        }
    }
}