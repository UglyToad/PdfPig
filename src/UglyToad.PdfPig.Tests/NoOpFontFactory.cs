namespace UglyToad.PdfPig.Tests
{
    using PdfPig.PdfFonts;
    using PdfPig.Tokens;

    /// <summary>
    /// A font factory for tests that never load a font. <see cref="PdfPig.Content.ResourceStore"/> requires
    /// one, but a test exercising colour spaces, output intents or the resource cache never reaches it.
    /// </summary>
    internal sealed class NoOpFontFactory : IFontFactory
    {
        public IFont Get(DictionaryToken dictionary) => null!;
    }
}
