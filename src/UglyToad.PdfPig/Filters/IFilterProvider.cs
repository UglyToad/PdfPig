namespace UglyToad.PdfPig.Filters
{
    using System.Collections.Generic;
    using Tokenization.Scanner;
    using Tokens;

    /// <summary>
    /// Gets filter implementations (<see cref="IFilter"/>) for decoding PDF data.
    /// </summary>
    public interface IFilterProvider
    {
        /// <summary>
        /// Get the filters specified in this dictionary.
        /// </summary>
        IReadOnlyList<IFilter> GetFilters(DictionaryToken dictionary);

        /// <summary>
        /// Gets the filters specified by the filter names.
        /// </summary>
        IReadOnlyList<IFilter> GetNamedFilters(IReadOnlyList<NameToken> names);

        /// <summary>
        /// Get all available filters in this library.
        /// </summary>
        IReadOnlyList<IFilter> GetAllFilters();
    }

    /// <summary>
    /// Gets filter implementations (<see cref="IFilter"/>) for decoding PDF data.
    /// </summary>
    public interface ILookupFilterProvider : IFilterProvider
    {
        /// <summary>
        /// Get the filters specified in this dictionary, using the <see cref="IPdfTokenScanner"/>.
        /// </summary>
        IReadOnlyList<IFilter> GetFilters(DictionaryToken dictionary, IPdfTokenScanner scanner);

        /// <summary>
        /// The most bytes a stream of the document may decode to, see <see cref="ParsingOptions.MaxDecompressedStreamBytes"/>.
        /// </summary>
        int MaxDecompressedStreamBytes { get; }
    }
}