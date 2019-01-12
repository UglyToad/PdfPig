namespace UglyToad.PdfPig.Fonts.CompactFontFormat.Charsets
{
    internal interface ICompactFontFormatCharset
    {
        bool IsCidCharset { get; }

        string GetNameByGlyphId(int glyphId);

        string GetNameByStringId(int stringId);

        int GetStringIdByGlyphId(int glyphId);
    }
}