namespace UglyToad.PdfPig.PdfFonts.Cmap
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Core;
    using Parser;

    internal static class CMapCache
    {
        private static readonly Dictionary<string, CMap> Cache = new Dictionary<string, CMap>(StringComparer.OrdinalIgnoreCase);
        private static readonly object Lock = new object();

        private static readonly CMapParser CMapParser = new CMapParser();

        public static bool TryGet(string name, [NotNullWhen(true)] out CMap? result)
        {
            result = null;

            lock (Lock)
            {
                if (Cache.TryGetValue(name, out result))
                {
                    return true;
                }

                if (CMapParser.TryParseExternal(name, out result))
                {
                    Cache[name] = result;
                    return true;
                }

                return false;
            }
        }

        public static CMap Parse(IInputBytes bytes)
        {
            if (bytes is null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            return CMapParser.Parse(bytes);
        }
    }
}
