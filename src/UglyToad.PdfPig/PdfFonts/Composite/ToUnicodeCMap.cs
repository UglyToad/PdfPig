namespace UglyToad.PdfPig.PdfFonts.Composite
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using Cmap;
    using Core;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Defines the information content (actual text) of the font
    /// as opposed to the display format.
    /// </summary>
    internal class ToUnicodeCMap
    {
        private readonly CMap? cMap;

        /// <summary>
        /// Does the font provide a CMap to map CIDs to Unicode values?
        /// </summary>
        public bool CanMapToUnicode => cMap != null;

        /// <summary>
        /// Is this document (unexpectedly) using a predefined Identity-H/V CMap as its ToUnicode CMap?
        /// </summary>
        public bool IsUsingIdentityAsUnicodeMap { get; }

        public ToUnicodeCMap(CMap? cMap)
        {
            this.cMap = cMap;

            if (cMap != null)
            {
                IsUsingIdentityAsUnicodeMap = cMap.Name?.StartsWith("Identity-", StringComparison.InvariantCultureIgnoreCase) == true;
            }
        }

        public bool TryGet(int code, [NotNullWhen(true)] out string? value)
        {
            value = null;

            if (cMap is null)
            {
                return false;
            }

            return cMap.TryConvertToUnicode(code, out value);
        }

        public int ReadCode(IInputBytes inputBytes)
        {
            return cMap!.ReadCode(inputBytes);
        }
    }
}