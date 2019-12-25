namespace UglyToad.PdfPig.Fonts.TrueType.Parser
{
    using System.Text;
    using Names;
    using Tables;
    using Util;
    using Util.JetBrains.Annotations;

    internal class NameTableParser : ITrueTypeTableParser<NameTable>
    {
        public NameTable Parse(TrueTypeHeaderTable header, TrueTypeDataBytes data, TableRegister.Builder register)
        {
            data.Seek(header.Offset);
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
                            var platformEncoding = (TrueTypeWindowsEncodingIdentifier)nameRecord.PlatformEncodingId;

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

                var position = header.Offset + stringOffset + nameRecord.Offset;

                data.Seek(position);

                var str = data.ReadString(nameRecord.Length, encoding);

                strings[i] = nameRecord.ToNameRecord(str);
            }

            return new NameTable(header, GetName(4, strings), GetName(1, strings), GetName(2, strings), strings);
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

            public ushort PlatformEncodingId { get; }

            private ushort LanguageId { get; }

            private ushort NameId { get; }

            public ushort Length { get; }

            public ushort Offset { get; }

            private NameRecordBuilder(ushort platformId, ushort platformEncodingId, ushort languageId,
                ushort nameId,
                ushort length,
                ushort offset)
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
                    LanguageId, NameId, s);
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
