namespace UglyToad.PdfPig.Fonts.SystemFonts
{
    using System;

    internal readonly struct SystemFontRecord
    {
        public string Path { get; }

        public SystemFontType Type { get; }

        public SystemFontRecord(string path, SystemFontType type)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Type = type;
        }

        public static bool TryCreate(string path, out SystemFontRecord type)
        {
            type = default(SystemFontRecord);

            // Font files are often named in upper case on Windows (e.g. ARIALN.TTF), so ignore case.
            SystemFontType fontType;
            if (path.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
            {
                fontType = SystemFontType.TrueType;
            }
            else if (path.EndsWith(".otf", StringComparison.OrdinalIgnoreCase))
            {
                fontType = SystemFontType.OpenType;
            }
            else if (path.EndsWith(".ttc", StringComparison.OrdinalIgnoreCase))
            {
                fontType = SystemFontType.TrueTypeCollection;
            }
            else if (path.EndsWith(".otc", StringComparison.OrdinalIgnoreCase))
            {
                fontType = SystemFontType.OpenTypeCollection;
            }
            else if (path.EndsWith(".pfb", StringComparison.OrdinalIgnoreCase))
            {
                fontType = SystemFontType.Type1;
            }
            else
            {
                return false;
            }

            type = new SystemFontRecord(path, fontType);

            return true;
        }
    }
}