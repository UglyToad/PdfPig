namespace UglyToad.PdfPig.Fonts.TrueType.Subsetting
{
    using System;
    using System.IO;
    using Core;

    internal class TrueTypeOffsetSubtable : IWriteable
    {
        private static readonly byte[] VersionHeader = { 0, 1, 0, 0 };

        private readonly byte numberOfTables;

        public TrueTypeOffsetSubtable(byte numberOfTables)
        {
            this.numberOfTables = numberOfTables;
        }

        public void Write(Stream stream)
        {
            stream.Write(VersionHeader, 0, VersionHeader.Length);
            stream.WriteUShort(numberOfTables);

            var maximumPowerOf2 = GetHighestPowerOf2(numberOfTables);
            var searchRange = (ushort)(maximumPowerOf2 * 16);
            var entrySelector = (ushort)Math.Log(maximumPowerOf2, 2);
            var rangeShift = (ushort)((numberOfTables * 16) - searchRange);

            stream.WriteUShort(searchRange);
            stream.WriteUShort(entrySelector);
            stream.WriteUShort(rangeShift);
        }

        private static ushort GetHighestPowerOf2(int numberOfTables)
        {
            ushort result = 1;
            for (var i = 0; i < 8 * sizeof(ushort); i++)
            {
                var power = (ushort)(1 << i);
                if (power > numberOfTables)
                {
                    break;
                }

                result = power;
            }

            return result;
        }
    }
}
