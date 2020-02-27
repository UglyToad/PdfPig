namespace UglyToad.PdfPig.PdfFonts.Cmap
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Parser;

    internal static class CMapCache
    {
        private static readonly Dictionary<string, CMap> Cache = new Dictionary<string, CMap>(StringComparer.OrdinalIgnoreCase);
        private static readonly object Lock = new object();

        private static readonly CMapParser CMapParser = new CMapParser();

        public static CMap Get(string name)
        {
            lock (Lock)
            {
                if (Cache.TryGetValue(name, out var result))
                {
                    return result;
                }

                result = CMapParser.ParseExternal(name);

                Cache[name] = result;

                return result;
            }
        }

        public static CMap Parse(IInputBytes bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            var result = CMapParser.Parse(bytes);

            return result;
        }
    }
}
