using UglyToad.PdfPig.Fonts.TrueType;

namespace UglyToad.PdfPig.Fonts.SystemFonts
{
    internal interface ISystemFontFinder
    {
        TrueTypeFontProgram GetTrueTypeFont(string name);
    }
}