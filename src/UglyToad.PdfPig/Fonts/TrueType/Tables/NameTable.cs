namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    internal class NameTable
    {
        public string Tag => TrueTypeHeaderTable.Name;

        public TrueTypeHeaderTable DirectoryTable { get; }

        public static void Load(TrueTypeDataBytes data, TrueTypeHeaderTable table)
        {
            data.Seek(table.Offset);
            var format = data.ReadUnsignedShort();
            var count = data.ReadUnsignedShort();
            var stringOffset = data.ReadUnsignedShort();

            var names = new NameRecord[count];
            for (var i = 0; i < count; i++)
            {
                names[i] = NameRecord.Read(data);
            }

        }

        public struct NameRecord
        {
            public int PlatformId { get; }

            public int PlatformSpecificId { get; }

            public int LanguageId { get; }

            public int NameId { get; }

            public int Length { get; }

            public int Offset { get; }

            public NameRecord(int platformId, int platformSpecificId, int languageId, int nameId, int length, int offset)
            {
                PlatformId = platformId;
                PlatformSpecificId = platformSpecificId;
                LanguageId = languageId;
                NameId = nameId;
                Length = length;
                Offset = offset;
            }

            public static NameRecord Read(TrueTypeDataBytes data)
            {
                return new NameRecord(data.ReadUnsignedShort(),
                    data.ReadUnsignedShort(),
                    data.ReadUnsignedShort(),
                    data.ReadUnsignedShort(),
                    data.ReadUnsignedShort(),
                    data.ReadUnsignedShort());
            }
        }
    }
}
