namespace UglyToad.PdfPig.Fonts.TrueType.Parser
{
    using System;
    using Tables;

    internal static class TableParser
    {
        private static readonly NameTableParser NameTableParser = new NameTableParser();
        private static readonly Os2TableParser Os2TableParser = new Os2TableParser();

        public static T Parse<T>(TrueTypeHeaderTable table, TrueTypeDataBytes data, TableRegister.Builder register) where T : ITable
        {
            if (typeof(T) == typeof(NameTable))
            {
                return (T) (object) NameTableParser.Parse(table, data, register);
            }

            if (typeof(T) == typeof(Os2Table))
            {
                return (T)(object)Os2TableParser.Parse(table, data, register);
            }

            throw new NotImplementedException();
        }
    }
}
