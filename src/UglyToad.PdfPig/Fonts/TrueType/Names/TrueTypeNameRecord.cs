namespace UglyToad.PdfPig.Fonts.TrueType.Names
{
    internal class TrueTypeNameRecord
    {
        public TrueTypePlatformIdentifier PlatformId { get; }

        public int PlatformEncodingId { get; }

        public int LanguageId { get; }

        public int NameId { get; }

        public int Length { get; }

        public int Offset { get; }

        public string Value { get; }

        public TrueTypeNameRecord(TrueTypePlatformIdentifier platformId, int platformEncodingId, int languageId, int nameId, int length, int offset, string value)
        {
            PlatformId = platformId;
            PlatformEncodingId = platformEncodingId;
            LanguageId = languageId;
            NameId = nameId;
            Length = length;
            Offset = offset;
            Value = value;
        }
    }
}
