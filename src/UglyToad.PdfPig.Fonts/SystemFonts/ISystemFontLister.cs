namespace UglyToad.PdfPig.Fonts.SystemFonts
{
    using System.Collections.Generic;

    internal interface ISystemFontLister
    {
        IEnumerable<SystemFontRecord> GetAllFonts();
    }
}