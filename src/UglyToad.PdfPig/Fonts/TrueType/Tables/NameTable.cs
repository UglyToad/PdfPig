namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System.Collections.Generic;
    using System.Text;
    using Names;
    using Util;
    using Util.JetBrains.Annotations;

    internal class NameTable
    {
        public string Tag => TrueTypeHeaderTable.Name;

        public TrueTypeHeaderTable DirectoryTable { get; }

        public string FontName { get; }

        public string FontFamilyName { get; }

        public string FontSubFamilyName { get; }

        public IReadOnlyList<TrueTypeNameRecord> NameRecords { get; }

        public NameTable(TrueTypeHeaderTable directoryTable, 
            string fontName,
            string fontFamilyName, 
            string fontSubFamilyName,
            IReadOnlyList<TrueTypeNameRecord> nameRecords)
        {
            DirectoryTable = directoryTable;
            FontName = fontName;
            FontFamilyName = fontFamilyName;
            FontSubFamilyName = fontSubFamilyName;
            NameRecords = nameRecords;
        }

        internal static NameTable Load(TrueTypeDataBytes data, TrueTypeHeaderTable table)
        {
            data.Seek(table.Offset);
            // ReSharper disable once UnusedVariable
            var format = data.ReadUnsignedShort();
            var count = data.ReadUnsignedShort();
            var stringOffset = data.ReadUnsignedShort();
            
            var names = new NameRecordBuilder[count];
            for (var i = 0; i < count; i++)
            {
                names[i] = NameRecordBuilder.Read(data);
            }

            var strings = new TrueTypeNameRecord[count];
            for (var i = 0; i < count; i++)
            {
                var nameRecord = names[i];

                var encoding = OtherEncodings.Iso88591;

                switch (nameRecord.PlatformId)
                {
                    case TrueTypePlatformIdentifier.Windows:
                    {
                        var platformEncoding = (TrueTypeWindowsEncodingIdentifier) nameRecord.PlatformEncodingId;

                        if (platformEncoding == TrueTypeWindowsEncodingIdentifier.Symbol
                            || platformEncoding == TrueTypeWindowsEncodingIdentifier.UnicodeBmp)
                        {
                            encoding = Encoding.BigEndianUnicode;
                        }
                        break;
                    }
                    case TrueTypePlatformIdentifier.Unicode:
                    {
                        encoding = Encoding.BigEndianUnicode;
                        break;
                    }
                    case TrueTypePlatformIdentifier.Iso:
                    {
                        switch (nameRecord.PlatformEncodingId)
                        {
                                case 0:
                                    encoding = Encoding.GetEncoding("US-ASCII");
                                    break;
                                case 1:
                                    encoding = Encoding.GetEncoding("ISO-10646-UCS-2");
                                    break;
                        }

                        break;
                    }
                }

                var position = table.Offset + stringOffset + nameRecord.Offset;

                data.Seek(position);

                var str = data.ReadString(nameRecord.Length, encoding);

                strings[i] = nameRecord.ToNameRecord(str);
            }

            return new NameTable(table, GetName(4, strings), GetName(1, strings), GetName(2, strings), strings);
        }

        [CanBeNull]
        private static string GetName(int nameId, TrueTypeNameRecord[] names)
        {
            const int windowsEnUs = 409;
            string windows = null;
            string any = null;

            for (var i = 0; i < names.Length; i++)
            {
                var name = names[i];

                if (name.NameId != nameId)
                {
                    continue;
                }

                if (name.PlatformId == TrueTypePlatformIdentifier.Windows && name.LanguageId == windowsEnUs)
                {
                    return name.Value;
                }

                if (name.PlatformId == TrueTypePlatformIdentifier.Windows)
                {
                    windows = name.Value;
                }

                any = name.Value;
            }

            return windows ?? any;
        }

        private struct NameRecordBuilder
        {
            public TrueTypePlatformIdentifier PlatformId { get; }

            public int PlatformEncodingId { get; }

            private int LanguageId { get; }

            private int NameId { get; }

            public int Length { get; }

            public int Offset { get; }

            private NameRecordBuilder(int platformId, int platformEncodingId, int languageId, int nameId, int length, int offset)
            {
                PlatformId = (TrueTypePlatformIdentifier)platformId;
                PlatformEncodingId = platformEncodingId;
                LanguageId = languageId;
                NameId = nameId;
                Length = length;
                Offset = offset;
            }

            public TrueTypeNameRecord ToNameRecord(string s)
            {
                return new TrueTypeNameRecord(PlatformId, PlatformEncodingId,
                    LanguageId, NameId, Length, Offset, s);
            }

            public static NameRecordBuilder Read(TrueTypeDataBytes data)
            {
                return new NameRecordBuilder(data.ReadUnsignedShort(),
                    data.ReadUnsignedShort(),
                    data.ReadUnsignedShort(),
                    data.ReadUnsignedShort(),
                    data.ReadUnsignedShort(),
                    data.ReadUnsignedShort());
            }
        }
    }
}
