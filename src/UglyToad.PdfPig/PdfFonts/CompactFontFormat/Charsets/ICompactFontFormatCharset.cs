namespace UglyToad.PdfPig.PdfFonts.CompactFontFormat.Charsets
{
    internal interface ICompactFontFormatCharset
    {
        bool IsCidCharset { get; }

        string GetNameByGlyphId(int glyphId);

        string GetNameByStringId(int stringId);

        int GetStringIdByGlyphId(int glyphId);

        int GetGlyphIdByName(string characterName);
    }
}