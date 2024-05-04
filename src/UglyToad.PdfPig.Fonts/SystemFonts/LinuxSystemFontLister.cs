namespace UglyToad.PdfPig.Fonts.SystemFonts
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal sealed class LinuxSystemFontLister : ISystemFontLister
    {
        public IEnumerable<SystemFontRecord> GetAllFonts()
        {
            var directories = new List<string>
            {
                "/usr/local/fonts", // local
                "/usr/local/share/fonts", // local shared
                "/usr/share/fonts", // system
                "/usr/X11R6/lib/X11/fonts" // X
            };

            try
            {
                var folder = Environment.GetEnvironmentVariable("$HOME");
                if (string.IsNullOrWhiteSpace(folder))
                {
                    folder = Environment.GetEnvironmentVariable("HOME");
                }

                if (!string.IsNullOrWhiteSpace(folder))
                {
                    directories.Add($"{folder}/.fonts");
                }
            }
            catch
            {
                // ignored
            }

            foreach (var directory in directories)
            {
                try
                {
                    if (!Directory.Exists(directory))
                    {
                        continue;
                    }
                }
                catch
                {
                    continue;
                }

                string[] files;

                try
                {
                    files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
                }
                catch
                {
                    continue;
                }

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