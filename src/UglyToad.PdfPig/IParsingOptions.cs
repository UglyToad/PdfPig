namespace UglyToad.PdfPig
{
    using System.Collections.Generic;
    using UglyToad.PdfPig.Logging;

    /// <summary>
    /// Parsing options interface.
    /// </summary>
    public interface IParsingOptions
    {
        /// <summary>
        /// Should the parser apply clipping to paths?
        /// Defaults to <see langword="false"/>.
        /// <para>Bezier curves will be transformed into polylines if clipping is set to <see langword="true"/>.</para>
        /// </summary>
        bool ClipPaths { get; }

        /// <summary>
        /// Should the parser ignore issues where the document does not conform to the PDF specification?
        /// </summary>
        bool UseLenientParsing { get; }

        /// <summary>
        /// All passwords to try when opening this document, will include any values set for <see cref="ParsingOptions.Password"/>.
        /// </summary>
        List<string> Passwords { get; }

        /// <summary>
        /// Skip extracting content where the font could not be found, will result in some letters being skipped/missed
        /// but will prevent the library throwing where the source PDF has some corrupted text. Also skips XObjects like
        /// forms and images when missing.
        /// </summary>
        bool SkipMissingFonts { get; }

        /// <summary>
        /// The <see cref="ILog"/> used to record messages raised by the parsing process.
        /// </summary>
        ILog Logger { get; }
    }
}
