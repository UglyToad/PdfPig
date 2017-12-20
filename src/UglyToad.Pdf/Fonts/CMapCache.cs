namespace UglyToad.Pdf.Fonts
{
    using System;
    using System.Collections.Generic;
    using Cmap;
    using IO;
    using Parser;

    internal class CMapCache
    {
        private readonly Dictionary<string, CMap> cache = new Dictionary<string, CMap>(StringComparer.InvariantCultureIgnoreCase);
        private readonly CMapParser cMapParser;
        
        public CMapCache(CMapParser cMapParser)
        {
            this.cMapParser = cMapParser;
        }

        public CMap Get(string name)
        {
            if (cache.TryGetValue(name, out var result))
            {
                return result;
            }

            result = cMapParser.ParseExternal(name);

            cache[name] = result;

            return result;
        }

        public CMap Parse(IInputBytes bytes, bool isLenientParsing)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            var result = cMapParser.Parse(bytes, isLenientParsing);

            return result;
        }
    }
}
