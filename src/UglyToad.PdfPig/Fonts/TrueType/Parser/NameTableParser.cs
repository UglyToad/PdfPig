namespace UglyToad.PdfPig.Fonts.TrueType.Parser
{
    using System;
    using System.Text;
    using Names;
    using Tables;
    using Util;
    using Util.JetBrains.Annotations;

    internal class HorizontalHeaderTableParser : ITrueTypeTableParser<HorizontalHeaderTable>
    {
        public HorizontalHeaderTable Parse(TrueTypeHeaderTable header, TrueTypeDataBytes data, TableRegister.Builder register)
        {
            data.Seek(header.Offset);
            var majorVersion = data.ReadUnsignedShort();
            var minorVersion = data.ReadUnsignedShort();

            var ascender = data.ReadSignedShort();
            var descender = data.ReadSignedShort();
            var lineGap = data.ReadSignedShort();

            var advancedWidthMax = data.ReadUnsignedShort();

            var minLeftSideBearing = data.ReadSignedShort();
            var minRightSideBearing = data.ReadSignedShort();
            var xMaxExtent = data.ReadSignedShort();

            var caretSlopeRise = data.ReadSignedShort();
            var caretSlopeRun = data.ReadSignedShort();
            var caretOffset = data.ReadSignedShort();

            // Reserved section
            data.ReadSignedShort();
            data.ReadSignedShort();
            data.ReadSignedShort();
            data.ReadSignedShort();

            var metricDataFormat = data.ReadSignedShort();

            if (metricDataFormat != 0)
            {
                throw new NotSupportedException("The metric data format for a horizontal header table should be 0.");
            }

            var numberOfHeaderMetrics = data.ReadUnsignedShort();

            return new HorizontalHeaderTable(header, majorVersion, minorVersion, ascender,
                descender, lineGap, advancedWidthMax,
                minLeftSideBearing,
                minRightSideBearing,
                xMaxExtent,
                caretSlopeRise,
                caretSlopeRun,
                caretOffset,
                metricDataFormat,
                numberOfHeaderMetrics);

        }
    }

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
