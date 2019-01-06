namespace UglyToad.PdfPig.Fonts.SystemFonts
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class MacSystemFontLister : ISystemFontLister
    {
        public IEnumerable<SystemFontRecord> GetAllFonts()
        {
            var directories = new List<string>
            {
                "/Library/Fonts/", // local
                "/System/Library/Fonts/", // system
                "/Network/Library/Fonts/" // network
            };

            try
            {
                var folder = Environment.GetEnvironmentVariable("$HOME");

                if (!string.IsNullOrWhiteSpace(folder))
                {
                    directories.Add($"{folder}/Library/Fonts");
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
                    files = Directory.GetFiles(directory);
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