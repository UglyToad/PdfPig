namespace UglyToad.PdfPig.Fonts.SystemFonts
{
#if NET
    using System.Collections.Generic;

    internal sealed class BrowserSystemFontLister : ISystemFontLister
    {

        // Very early version, intended to help developing support for browser

        public IEnumerable<SystemFontRecord> GetAllFonts(IEnumerable<string>? additionalDirectories)
        {
            yield break;
        }
    }
#endif
}
