namespace UglyToad.PdfPig.Fonts.TrueType.Names
{
    internal class TrueTypeNameRecord
    {
        public TrueTypePlatformIdentifier PlatformId { get; }

        public ushort PlatformEncodingId { get; }

        public ushort LanguageId { get; }

        public ushort NameId { get; }
        
        public string Value { get; }

        public TrueTypeNameRecord(TrueTypePlatformIdentifier platformId, 
            ushort platformEncodingId,
            ushort languageId, 
            ushort nameId,
            string value)
        {
            PlatformId = platformId;
            PlatformEncodingId = platformEncodingId;
            LanguageId = languageId;
            NameId = nameId;
            Value = value;
        }

        public override string ToString()
        {
            return $"({PlatformId}, {NameId}) - {Value}";
        }
    }
}
