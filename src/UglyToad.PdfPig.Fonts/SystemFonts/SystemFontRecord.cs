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

            SystemFontType fontType;
            if (path.EndsWith(".ttf"))
            {
                fontType = SystemFontType.TrueType;
            }
            else if (path.EndsWith(".otf"))
            {
                fontType = SystemFontType.OpenType;
            }
            else if (path.EndsWith(".ttc"))
            {
                fontType = SystemFontType.TrueTypeCollection;
            }
            else if (path.EndsWith(".otc"))
            {
                fontType = SystemFontType.OpenTypeCollection;
            }
            else if (path.EndsWith(".pfb"))
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