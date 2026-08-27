namespace UglyToad.PdfPig.Fonts.SystemFonts
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal sealed class WindowsSystemFontLister : ISystemFontLister
    {
        public IEnumerable<SystemFontRecord> GetAllFonts(IEnumerable<string>? additionalDirectories)
        {
            var winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            var directories = new List<string>
            {
                Path.Combine(winDir, "Fonts"),
                Path.Combine(winDir, "PSFonts")
            };


            if (additionalDirectories is not null)
            {
                directories.AddRange(additionalDirectories);
            }

            foreach (var directory in directories)
            {
                foreach (var record in GetForDirectory(directory))
                {
                    yield return record;
                }
            }
        }

        private IEnumerable<SystemFontRecord> GetForDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path);

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