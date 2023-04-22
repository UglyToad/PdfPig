using System;
using System.Linq;

namespace IccProfileNet.Tags
{
    internal sealed class IccMultiLocalizedUnicodeType : IccTagTypeBase
    {
        public const int NumberOfRecordOffset = 8;
        public const int NumberOfRecordLength = 4;
        public const int RecordSizeOffset = 12;
        public const int RecordSizeLength = 4;

        public const int FirstRecordLanguageOffset = 16;
        public const int RecordLanguageLength = 2;
        public const int FirstRecordCountryOffset = 18;
        public const int RecordCountryLength = 2;
        public const int FirstRecordLengthOffset = 20;
        public const int RecordLengthLength = 4;

        public const int FirstRecordOffsetOffset = 24;
        public const int RecordOffsetLength = 4;

        private readonly Lazy<int> _numberOfRecords;
        /// <summary>
        /// Number of records (n).
        /// </summary>
        public int NumberOfRecords => _numberOfRecords.Value;

        private readonly Lazy<int> _recordSize;
        public int RecordSize => _recordSize.Value;

        private readonly Lazy<IccMultiLocalizedUnicodeRecord[]> _records;
        /// <summary>
        /// TODO
        /// </summary>
        public IccMultiLocalizedUnicodeRecord[] Records => _records.Value;

        public IccMultiLocalizedUnicodeType(byte[] rawData)
        {
            string typeSignature = IccHelper.GetString(rawData, TypeSignatureOffset, TypeSignatureLength);

            if (typeSignature != "mluc")
            {
                throw new ArgumentException(nameof(typeSignature));
            }

            RawData = rawData;

            _numberOfRecords = new Lazy<int>(() =>
            {
                // Number of records (n)
                // 8 to 11
                return (int)IccHelper.ReadUInt32(RawData
                    .Skip(NumberOfRecordOffset)
                    .Take(NumberOfRecordLength)
                    .ToArray());
            });

            _recordSize = new Lazy<int>(() =>
            {
                // Record size: the length in bytes of every record. The value is 12.
                // 12 to 15
                int recordSize = (int)IccHelper.ReadUInt32(RawData
                    .Skip(RecordSizeOffset)
                    .Take(RecordSizeLength)
                    .ToArray());

                if (recordSize != 12)
                {
                    throw new ArgumentException(nameof(recordSize));
                }
                return recordSize;
            });

            _records = new Lazy<IccMultiLocalizedUnicodeRecord[]>(() =>
            {
                IccMultiLocalizedUnicodeRecord[] records = new IccMultiLocalizedUnicodeRecord[NumberOfRecords];

                const int recordsStartOffset = RecordSizeOffset + RecordSizeLength;
                const int recordLanguageOffset = FirstRecordLanguageOffset - recordsStartOffset;
                const int recordCountryOffset = FirstRecordCountryOffset - recordsStartOffset;
                const int recordLengthOffset = FirstRecordLengthOffset - recordsStartOffset;
                const int recordOffsetOffset = FirstRecordOffsetOffset - recordsStartOffset;

                for (var i = 0; i < NumberOfRecords; ++i)
                {
                    int currentOffset = recordsStartOffset + (i * RecordSize);

                    // First record language code: in accordance with the
                    // language code specified in ISO 639-1
                    // 16 to 17
                    string language = IccHelper.GetString(RawData
                        .Skip(currentOffset + recordLanguageOffset)
                        .Take(RecordLanguageLength)
                        .ToArray());

                    // First record country code: in accordance with the country
                    // code specified in ISO 3166-1
                    // 18 to 19
                    string country = IccHelper.GetString(RawData
                        .Skip(currentOffset + recordCountryOffset)
                        .Take(RecordCountryLength)
                        .ToArray());

                    // First record string length: the length in bytes of the string
                    // 20 to 23
                    uint length = IccHelper.ReadUInt32(RawData
                        .Skip(currentOffset + recordLengthOffset)
                        .Take(RecordLengthLength)
                        .ToArray());

                    // First record string offset: the offset from the start of the tag
                    // to the start of the string, in bytes.
                    // 24 to 27
                    uint offset = IccHelper.ReadUInt32(RawData
                        .Skip(currentOffset + recordOffsetOffset)
                        .Take(RecordOffsetLength)
                        .ToArray());

                    string text = IccHelper.GetString(RawData, (int)offset, (int)length);

                    records[i] = new IccMultiLocalizedUnicodeRecord(language, country, text);
                }
                return records;
            });
        }

        /// <summary>
        /// TODO
        /// </summary>
        public readonly struct IccMultiLocalizedUnicodeRecord
        {
            /// <summary>
            /// Language code specified in ISO 639-1.
            /// </summary>
            public string Language { get; }

            /// <summary>
            /// Country code specified in ISO 3166-1.
            /// </summary>
            public string Country { get; }

            /// <summary>
            /// TODO
            /// </summary>
            public string Text { get; }

            /// <summary>
            /// TODO
            /// </summary>
            public IccMultiLocalizedUnicodeRecord(string language, string country, string text)
            {
                Language = language;
                Country = country;
                Text = text;
            }

            /// <inheritdoc/>
            public override string ToString()
            {
                return $"{Country}-{Language}: {Text}";
            }
        }
    }
}
