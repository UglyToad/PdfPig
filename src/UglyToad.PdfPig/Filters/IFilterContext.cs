namespace UglyToad.PdfPig.Filters
{
    using Logging;

    /// <summary>
    /// Lets a filter reach the log and the leniency setting of the document being
    /// decoded.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A filter receives the <see cref="IFilterProvider"/> it was obtained from, and
    /// during parsing that is the per document <c>FilterProviderWithLookup</c>. Asking
    /// it for this interface is therefore the one route from a filter to
    /// <see cref="ParsingOptions"/> that does not change <see cref="IFilter"/>, which
    /// is public and cannot gain a member without breaking every implementation of it.
    /// </para>
    /// <para>
    /// A provider that does not offer this - the default one, or a caller decoding a
    /// stream on their own through <c>PdfExtensions.Decode</c> - simply leaves a filter
    /// without a log, which is what it had before.
    /// </para>
    /// </remarks>
    internal interface IFilterContext
    {
        /// <summary>Where a filter reports what it had to repair.</summary>
        ILog Log { get; }

        /// <summary>
        /// Whether a filter should carry on past damaged input. False asks it to raise
        /// instead, which is what <see cref="ParsingOptions.UseLenientParsing"/> means
        /// everywhere else in the parser.
        /// </summary>
        bool UseLenientParsing { get; }
    }
}
