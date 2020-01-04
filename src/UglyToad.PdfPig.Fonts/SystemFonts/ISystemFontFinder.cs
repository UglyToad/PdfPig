namespace UglyToad.PdfPig.Fonts.SystemFonts
{
    using TrueType;

    internal interface ISystemFontFinder
    {
        TrueTypeFont GetTrueTypeFont(string name);
    }
}