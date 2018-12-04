namespace UglyToad.PdfPig.Fonts.TrueType.Parser
{
    using System;
    using Tables;

    internal static class TableParser
    {
        private static readonly Os2TableParser Os2TableParser = new Os2TableParser();
        public static T Parse<T>(TrueTypeHeaderTable table, TrueTypeDataBytes data, TableRegister.Builder register) where T : ITable
        {
            if (typeof(T) == typeof(Os2Table))
            {
                return (T)(object)Os2TableParser.Parse(table, data, register);
            }

            throw new NotImplementedException();
        }
    }

    internal class Os2TableParser : ITrueTypeTableParser<Os2Table>
    {
        public Os2Table Parse(TrueTypeHeaderTable header, TrueTypeDataBytes data, TableRegister.Builder register)
        {
            data.Seek(header.Offset);

            var version = data.ReadUnsignedShort();

            var xAvgCharWidth = data.ReadSignedShort();
            var usWeightClass = data.ReadUnsignedShort();
            var usWidthClass = data.ReadUnsignedShort();
            var fsType = data.ReadSignedShort();
            var ySubscriptXSize = data.ReadSignedShort();
            var ySubscriptYSize = data.ReadSignedShort();
            var ySubscriptXOffset = data.ReadSignedShort();
            var ySubscriptYOffset = data.ReadSignedShort();
            var ySuperscriptXSize = data.ReadSignedShort();
            var ySuperscriptYSize = data.ReadSignedShort();
            var ySuperscriptXOffset = data.ReadSignedShort();
            var ySuperscriptYOffset = data.ReadSignedShort();
            var yStrikeoutSize = data.ReadSignedShort();
            var yStrikeoutPosition = data.ReadSignedShort();
            var sFamilyClass = data.ReadSignedShort();
            var panose = data.ReadByteArray(10);
            var ulCharRange1 = data.ReadUnsignedInt();
            var ulCharRange2 = data.ReadUnsignedInt();
            var ulCharRange3 = data.ReadUnsignedInt();
            var ulCharRange4 = data.ReadUnsignedInt();
            var vendorId = data.ReadByteArray(4);
            var fsSelection = data.ReadUnsignedShort();
            var fsFirstCharIndex = data.ReadUnsignedShort();
            var fsLastCharIndex = data.ReadUnsignedShort();

            var bytesRead = data.Position - header.Offset;
            
            return null;
        }
    }

    internal interface ITrueTypeTableParser<out T> where T : ITable
    {
        T Parse(TrueTypeHeaderTable header, TrueTypeDataBytes data, TableRegister.Builder register);
    }
}
