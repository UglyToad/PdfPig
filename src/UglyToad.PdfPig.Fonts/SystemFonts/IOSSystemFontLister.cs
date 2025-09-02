namespace UglyToad.PdfPig.Fonts.SystemFonts
{
#if NET
    using System.Collections.Generic;

    internal sealed class IOSSystemFontLister : ISystemFontLister
    {
        // Very early version, intended to help developing support for iOS

        public IEnumerable<SystemFontRecord> GetAllFonts()
        {
            yield break;
        }
    }
#endif
}
