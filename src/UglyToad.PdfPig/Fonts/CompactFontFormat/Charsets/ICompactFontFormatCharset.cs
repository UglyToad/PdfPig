namespace UglyToad.PdfPig.Fonts.CompactFontFormat.Charsets
{
    internal interface ICompactFontFormatCharset
    {
        string GetNameByGlyphId(int glyphId);

        string GetNameByStringId(int stringId);

        string GetStringIdByGlyphId(int glyphId);
    }
}