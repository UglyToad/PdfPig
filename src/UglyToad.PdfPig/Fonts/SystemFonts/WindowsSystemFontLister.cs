namespace UglyToad.PdfPig.Fonts.SystemFonts
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class WindowsSystemFontLister : ISystemFontLister
    {
        public IEnumerable<SystemFontRecord> GetAllFonts()
        {
            // TODO: Could use System.Drawing InstalledFontCollection to do this?

            var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            var fonts = Path.Combine(winDir, "Fonts");

            if (Directory.Exists(fonts))
            {
                var files = Directory.GetFiles(fonts);

                foreach (var file in files)
                {
                    if (SystemFontRecord.TryCreate(file, out var record))
                    {
                        yield return record;
                    }
                }
            }

            var psFonts = Path.Combine(winDir, "PSFonts");

            if (Directory.Exists(psFonts))
            {
                var files = Directory.GetFiles(fonts);

                foreach (var file in files)
                {
                    if (SystemFontRecord.TryCreate(file, out var record))
                    {
                        yield return record;
                    }
                }
            }
        }
    }
}