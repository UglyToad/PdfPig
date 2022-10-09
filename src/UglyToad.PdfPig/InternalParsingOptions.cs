namespace UglyToad.PdfPig;

using Logging;
using System.Collections.Generic;

/// <summary>
/// <see cref="ParsingOptions"/> but without being a public API/
/// </summary>
internal class InternalParsingOptions
{
    public IReadOnlyList<string> Passwords { get; }

    public bool UseLenientParsing { get; }

    public bool ClipPaths { get; }

    public bool SkipMissingFonts { get; }

    public ILog Logger { get; }

    public InternalParsingOptions(
        IReadOnlyList<string> passwords,
        bool useLenientParsing,
        bool clipPaths,
        bool skipMissingFonts,
        ILog logger)
    {
        Passwords = passwords;
        UseLenientParsing = useLenientParsing;
        ClipPaths = clipPaths;
        SkipMissingFonts = skipMissingFonts;
        Logger = logger;
    }
}