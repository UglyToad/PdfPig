namespace UglyToad.PdfPig.Fonts.Composite
{
    using System;
    using Cmap;
    using IO;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Defines the information content (actual text) of the font
    /// as opposed to the display format.
    /// </summary>
    internal class ToUnicodeCMap
    {
        [CanBeNull]
        private readonly CMap cMap;

        /// <summary>
        /// Does the font provide a CMap to map CIDs to Unicode values?
        /// </summary>
        public bool CanMapToUnicode => cMap != null;

        /// <summary>
        /// Is this document (unexpectedly) using a predefined Identity-H/V CMap as its ToUnicode CMap?
        /// </summary>
        public bool IsUsingIdentityAsUnicodeMap { get; }

        public ToUnicodeCMap([CanBeNull]CMap cMap)
        {
            this.cMap = cMap;

            if (CanMapToUnicode)
            {
                IsUsingIdentityAsUnicodeMap =
                    cMap.Name?.StartsWith("Identity-", StringComparison.InvariantCultureIgnoreCase) == true;
            }
        }

        public bool TryGet(int code, out string value)
        {
            value = null;

            if (!CanMapToUnicode)
            {
                return false;
            }

            return cMap.TryConvertToUnicode(code, out value);
        }

        public int ReadCode(IInputBytes inputBytes)
        {
            return cMap.ReadCode(inputBytes);
        }
    }
}