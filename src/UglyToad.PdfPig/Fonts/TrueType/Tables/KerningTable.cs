namespace UglyToad.PdfPig.Fonts.TrueType.Tables
{
    using Kerning;

    internal class KerningTable
    {
        public static KerningTable Load(TrueTypeDataBytes data, TrueTypeHeaderTable headerTable)
        {
            data.Seek(headerTable.Offset);

            var version = data.ReadUnsignedShort();

            var numberOfSubtables = data.ReadUnsignedShort();

            for (var i = 0; i < numberOfSubtables; i++)
            {
                var subtableVersion = data.ReadUnsignedShort();
                var subtableLength = data.ReadUnsignedShort();
                var coverage = data.ReadUnsignedShort();

                var kernCoverage = (KernCoverage) coverage;
                var format = ((coverage & 255) >> 8);
            }

            return new KerningTable();
        }
    }
}
