// ReSharper disable UnusedVariable
namespace UglyToad.PdfPig.Fonts.TrueType.Tables.CMapSubTables
{
    using System;
    using System.Collections.Generic;

    /// <inheritdoc />
    /// <summary>
    /// A format 4 CMap sub-table which defines gappy ranges of character code to glyph index mappings.
    /// </summary>
    internal class Format4CMapTable : ICMapSubTable
    {
        public int PlatformId { get; }

        public int EncodingId { get; }

        public int Language { get; }

        public IReadOnlyList<Segment> Segments { get; }

        public IReadOnlyList<int> GlyphIds { get; }

        /// <summary>
        /// Create a new <see cref="Format4CMapTable"/>.
        /// </summary>
        public Format4CMapTable(int platformId, int encodingId, int language, IReadOnlyList<Segment> segments, IReadOnlyList<int> glyphIds)
        {
            PlatformId = platformId;
            EncodingId = encodingId;
            Language = language;
            Segments = segments ?? throw new ArgumentNullException(nameof(segments));
            GlyphIds = glyphIds ?? throw new ArgumentNullException(nameof(glyphIds));
        }

        public int CharacterCodeToGlyphIndex(int characterCode)
        {
            for (var i = 0; i < Segments.Count; i++)
            {
                var segment = Segments[i];

                if (segment.EndCode < characterCode || segment.StartCode > characterCode)
                {
                    continue;
                }

                if (segment.IdRangeOffset == 0)
                {
                    return (characterCode + segment.IdDelta) & 0xFFFF;
                }

                var offset = segment.IdRangeOffset / 2 + (characterCode - segment.StartCode);

                return GlyphIds[offset - Segments.Count + i];
            }

            return 0;
        }

        public static Format4CMapTable Load(TrueTypeDataBytes data, int platformId, int encodingId)
        {
            // Length in bytes.
            var length = data.ReadUnsignedShort();

            // Used for sub-tables with a Macintosh platform ID.
            var version = data.ReadUnsignedShort();

            var doubleSegmentCount = data.ReadUnsignedShort();

            // Defines the number of contiguous segments.
            var segmentCount = doubleSegmentCount / 2;

            // Some crazy sum.
            var searchRange = data.ReadUnsignedShort();
            var entrySelector = data.ReadUnsignedShort();
            var rangeShift = data.ReadUnsignedShort();

            // End character codes for each segment.
            var endCounts = data.ReadUnsignedShortArray(segmentCount);

            // Should be zero.
            var reservedPad = data.ReadUnsignedShort();

            // Start character codes for each segment.
            var startCounts = data.ReadUnsignedShortArray(segmentCount);

            // Delta for all character codes in the segment. Contrary to the spec this is actually a short[].
            var idDeltas = data.ReadShortArray(segmentCount);

            var idRangeOffsets = data.ReadUnsignedShortArray(segmentCount);

            const int singleIntsRead = 8;
            const int intArraysRead = 8;

            // ReSharper disable once ArrangeRedundantParentheses
            var remainingBytes = length - ((singleIntsRead * 2) + intArraysRead * segmentCount);

            var remainingInts = remainingBytes / 2;

            var glyphIndices = data.ReadUnsignedShortArray(remainingInts);

            var segments = new Segment[endCounts.Length];
            for (int i = 0; i < endCounts.Length; i++)
            {
                var start = startCounts[i];
                var end = endCounts[i];

                var delta = idDeltas[i];
                var offsets = idRangeOffsets[i];

                segments[i] = new Segment(start, end, delta, offsets);
            }

            return new Format4CMapTable(platformId, encodingId, version, segments, glyphIndices);
        }

        /// <summary>
        /// A contiguous segment which maps character to glyph codes in a Format 4 CMap sub-table.
        /// </summary>
        public struct Segment
        {
            /// <summary>
            /// The start character code in the range.
            /// </summary>
            public int StartCode { get; }

            /// <summary>
            /// The end character code in the range.
            /// </summary>
            public int EndCode { get; }

            /// <summary>
            /// The delta for the codes in the segment.
            /// </summary>
            public int IdDelta { get; }

            /// <summary>
            /// Offset in bytes to glyph index array.
            /// </summary>
            public int IdRangeOffset { get; }

            /// <summary>
            /// Create a new <see cref="Segment"/>.
            /// </summary>
            public Segment(int startCode, int endCode, int idDelta, int idRangeOffset)
            {
                StartCode = startCode;
                EndCode = endCode;
                IdDelta = idDelta;
                IdRangeOffset = idRangeOffset;
            }

            public override string ToString()
            {
                return $"Start: {StartCode}, End: {EndCode}, Delta: {IdDelta}, Offset: {IdRangeOffset}";
            }
        }
    }
}
