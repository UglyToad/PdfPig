namespace UglyToad.Pdf.Fonts.Parser.Parts
{
    using Cmap;
    using Tokenization.Scanner;

    /// <summary>
    /// Provides parsing for a certain operator type in a CID font definition.
    /// </summary>
    /// <typeparam name="TToken">The type of the token preceding the operation we wish to parse.</typeparam>
    internal interface ICidFontPartParser<in TToken>
    {
        /// <summary>
        /// Parse the definition for this part of the CID font and write the results to the <see cref="CharacterMapBuilder"/>.
        /// </summary>
        void Parse(TToken previous, ITokenScanner tokenScanner, CharacterMapBuilder builder, bool isLenientParsing);
    }
}