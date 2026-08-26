namespace UglyToad.PdfPig.Fonts.SystemFonts
{
#if NET
    using System.Collections.Generic;

    internal sealed class BrowserSystemFontLister : ISystemFontLister
    {

        // Very early version, intended to help developing support for browser

        IEnumerable<SystemFontRecord> ISystemFontLister.GetAllFonts()
        {
            return GetAllFonts(null);
        }

        IEnumerable<SystemFontRecord> ISystemFontLister.GetAllFonts(IEnumerable<string> additionalDirectories)
        {
            return GetAllFonts(additionalDirectories);
        }

        private IEnumerable<SystemFontRecord> GetAllFonts(IEnumerable<string>? additionalDirectories)
        {
            yield break;
        }
    }
#endif
}
