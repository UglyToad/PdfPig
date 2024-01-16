﻿namespace UglyToad.PdfPig.Fonts.TrueType.Parser
{
    using System;
    using System.Diagnostics;
    using Tables;

    internal static class TableParser
    {
        private static readonly CMapTableParser CMapTableParser = new CMapTableParser();
        private static readonly HorizontalMetricsTableParser HorizontalMetricsTableParser = new HorizontalMetricsTableParser();
        private static readonly HorizontalHeaderTableParser HorizontalHeaderTableParser = new HorizontalHeaderTableParser();
        private static readonly NameTableParser NameTableParser = new NameTableParser();
        private static readonly Os2TableParser Os2TableParser = new Os2TableParser();

        public static T Parse<T>(TrueTypeHeaderTable table, TrueTypeDataBytes data, TableRegister.Builder register) where T : ITrueTypeTable
        {
            // checksum https://developer.apple.com/fonts/TrueType-Reference-Manual/RM06/Chap6.html
            uint sum = 0;
            var nLongs = (table.Length + 3) / 4;
            data.Seek(table.Offset);
            while (nLongs-- > 0)
            {
                sum += (uint)data.ReadSignedInt();
            }

            if (sum != table.CheckSum)
            {
                Debug.WriteLine("Table with invalid checksum found in TrueType font file.");
            }

            if (typeof(T) == typeof(CMapTable))
            {
                return (T) (object) CMapTableParser.Parse(table, data, register);
            }

            if (typeof(T) == typeof(HorizontalMetricsTable))
            {
                return (T) (object) HorizontalMetricsTableParser.Parse(table, data, register);
            }

            if (typeof(T) == typeof(NameTable))
            {
                return (T) (object) NameTableParser.Parse(table, data, register);
            }

            if (typeof(T) == typeof(Os2Table))
            {
                return (T)(object)Os2TableParser.Parse(table, data, register);
            }

            if (typeof(T) == typeof(HorizontalHeaderTable))
            {
                return (T) (object) HorizontalHeaderTableParser.Parse(table, data, register);
            }

            throw new NotImplementedException();
        }
    }
}
