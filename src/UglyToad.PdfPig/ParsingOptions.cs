namespace UglyToad.PdfPig
{
    using Filters;
    using System.Collections.Generic;
    using Logging;
    using Graphics.Colors.Icc;

    /// <summary>
    /// Configures options used by the parser when reading PDF documents.
    /// </summary>
    public class ParsingOptions
    {
        /// <summary>
        /// A default <see cref="ParsingOptions"/> with <see cref="UseLenientParsing"/> set to false.
        /// </summary>
        public static ParsingOptions LenientParsingOff { get; } = new ParsingOptions
        {
            UseLenientParsing = false
        };

        /// <summary>
        /// Should the parser apply clipping to paths?
        /// Defaults to <see langword="false"/>.
        /// <para>Bezier curves will be transformed into polylines if clipping is set to <see langword="true"/>.</para>
        /// </summary>
        public bool ClipPaths { get; set; } = false;

        /// <summary>
        /// Should the parser ignore issues where the document does not conform to the PDF specification?
        /// </summary>
        public bool UseLenientParsing { get; set; } = true;

        /// <summary>
        /// The <see cref="ILog"/> used to record messages raised by the parsing process.
        /// </summary>
        public ILog Logger { get; set; } = new NoOpLog();

        /// <summary>
        /// The password to use to open the document if it is encrypted. If you need to supply multiple passwords to test against
        /// you can use <see cref="Passwords"/>. The value of <see cref="Password"/> will be included in the list to test against.
        /// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// All passwords to try when opening this document, will include any values set for <see cref="Password"/>.
        /// </summary>
        public List<string> Passwords { get; set; } = new List<string>();

        /// <summary>
        /// Skip extracting content where the font could not be found, will result in some letters being skipped/missed
        /// but will prevent the library throwing where the source PDF has some corrupted text. Also skips XObjects like
        /// forms and images when missing.
        /// </summary>
        public bool SkipMissingFonts { get; set; } = false;

        /// <summary>
        /// Gets or sets the maximum allowed stack depth.
        /// </summary>
        /// <remarks>This property can be used to limit the depth of recursive or nested operations to
        /// prevent stack overflows or excessive resource usage.</remarks>
        public int MaxStackDepth { get; set; } = 256;

        /// <summary>
        /// Filter provider to use while parsing the document. The <see cref="DefaultFilterProvider"/> will be used if set to <c>null</c>.
        /// </summary>
        public IFilterProvider? FilterProvider { get; set; } = null;

        /// <summary>
        /// Should the parser use the replacement text specified by marked-content <c>/ActualText</c> entries
        /// when extracting text. When enabled, content enclosed by an <c>/ActualText</c> sequence is extracted
        /// using that replacement text (see the PDF specification, 14.9.4 "Replacement text") instead of the
        /// enclosed glyphs' own Unicode values.
        /// Defaults to <see langword="false"/>.
        /// </summary>
        public bool UseActualText { get; set; } = false;

        /// <summary>
        /// Service used to convert <c>/ICCBased</c> color space samples. When <c>null</c> (default),
        /// ICC-based color spaces fall back silently to their declared alternate color space.
        /// </summary>
        public IIccProfileService? IccProfileService { get; set; } = null;

        /// <summary>
        /// Should content in optional content groups (layers) that are hidden by the document's default
        /// configuration be skipped (see the PDF specification, 8.11 "Optional content"). When enabled,
        /// hidden letters, paths and images are left out of the page content.
        /// Graphics state changes made inside hidden content (e.g. <c>cm</c>, clipping) still apply.
        /// Custom processors built on <see cref="Graphics.BaseStreamProcessor{TPageContent}"/> do not receive hidden
        /// images or form XObjects, nor hidden glyphs other than those in a clip text rendering mode, but must check
        /// its <c>IsOptionalContentHidden</c> property before drawing those glyphs, painted paths and shadings.
        /// Defaults to <see langword="false"/>: all content is returned, whether visible or not.
        /// </summary>
        public bool SkipHiddenOptionalContent { get; set; } = false;
    }
}