namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using System.Collections.Generic;
    using Kerning;

    internal class KerningTable
    {
        public IReadOnlyList<KerningSubTable> KerningTables { get; }

        public KerningTable(IReadOnlyList<KerningSubTable> kerningTables)
        {
            var notNull = new List<KerningSubTable>();

            foreach (var kerningTable in kerningTables)
            {
                if (kerningTable != null)
                {
                    notNull.Add(kerningTable);
                }
            }

            KerningTables = notNull;
        }

        public static KerningTable Load(TrueTypeDataBytes data, TrueTypeHeaderTable headerTable)
        {
            data.Seek(headerTable.Offset);

            var version = data.ReadUnsignedShort();

            var numberOfSubtables = data.ReadUnsignedShort();

            var subTables = new KerningSubTable[numberOfSubtables];

            for (var i = 0; i < numberOfSubtables; i++)
            {
                var currentOffset = data.Position;

                var subtableVersion = data.ReadUnsignedShort();
                var subtableLength = data.ReadUnsignedShort();
                var coverage = data.ReadUnsignedShort();

                var kernCoverage = (KernCoverage) coverage;
                var format = ((coverage & 255) >> 8);

                switch (format)
                {
                    case 0:
                        subTables[i] = ReadFormat0Table(subtableVersion, data, kernCoverage);
                        break;
                    case 2:
                        subTables[i] = ReadFormat2Table(subtableVersion, data, kernCoverage, currentOffset);
                        break;
                }
            }

            return new KerningTable(subTables);
        }

        private static KerningSubTable ReadFormat0Table(int version, TrueTypeDataBytes data, KernCoverage coverage)
        {
            var numberOfPairs = data.ReadUnsignedShort();
            // ReSharper disable once UnusedVariable
            var searchRange = data.ReadUnsignedShort();
            // ReSharper disable once UnusedVariable
            var entrySelector = data.ReadUnsignedShort();
            // ReSharper disable once UnusedVariable
            var rangeShift = data.ReadUnsignedShort();

            var pairs = new KernPair[numberOfPairs];

            for (int i = 0; i < numberOfPairs; i++)
            {
                var leftGlyphIndex = data.ReadUnsignedShort();
                var rightGlyphIndex = data.ReadUnsignedShort();

                var value = data.ReadSignedShort();

                pairs[i] = new KernPair(leftGlyphIndex, rightGlyphIndex, value);
            }

            return new KerningSubTable(version, coverage, pairs);
        }

        private static KerningSubTable ReadFormat2Table(int version, TrueTypeDataBytes data, KernCoverage coverage, long tableStartOffset)
        {
            // TODO: Implement and test this;
            return null;

#pragma warning disable 162
            var rowWidth = data.ReadUnsignedShort();

            var leftClassTableOffset = data.ReadUnsignedShort();
            var rightClassTableOffset = data.ReadUnsignedShort();

            var kerningArrayOffset = data.ReadUnsignedShort();

            data.Seek(tableStartOffset + leftClassTableOffset);

            var leftTableFirstGlyph = data.ReadUnsignedShort();
            var numberOfLeftGlyphs = data.ReadUnsignedShort();

            var leftGlyphClassValues = new int[numberOfLeftGlyphs];

            for (var i = 0; i < numberOfLeftGlyphs; i++)
            {
                leftGlyphClassValues[i] = data.ReadUnsignedShort();
            }

            data.Seek(tableStartOffset + rightClassTableOffset);

            var rightTableFirstGlyph = data.ReadUnsignedShort();
            var numberOfRightGlyphs = data.ReadUnsignedShort();

            var rightGlyphClassValues = new int[numberOfRightGlyphs];

            for (var i = 0; i < numberOfRightGlyphs; i++)
            {
                rightGlyphClassValues[i] = data.ReadUnsignedShort();
            }

            data.Seek(tableStartOffset + kerningArrayOffset);

            var pairs = new List<KernPair>(numberOfRightGlyphs * numberOfLeftGlyphs);

            // Data is a [left glyph count, right glyph count] array:
            for (int i = 0; i < numberOfLeftGlyphs; i++)
            {
                var leftClassValue = leftGlyphClassValues[i];
                for (int j = 0; j < numberOfRightGlyphs; j++)
                {
                    var rightClassValue = rightGlyphClassValues[j];

                    pairs.Add(new KernPair(leftClassValue, rightClassValue, data.ReadSignedShort()));
                }
            }

            return new KerningSubTable(version, coverage, pairs);
#pragma warning restore 162
        }
    }
}
