namespace UglyToad.PdfPig.Fonts.SystemFonts
{
#if NET
    using System.Collections.Generic;
    using System.IO;

    internal sealed class AndroidSystemFontLister : ISystemFontLister
    {
        public IEnumerable<SystemFontRecord> GetAllFonts(IEnumerable<string>? additionalDirectories)
        {
            var directories = new List<string>
            {
                "/system/fonts",
            };

            if (additionalDirectories is not null)
            {
                directories.AddRange(additionalDirectories);
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
#endif
}
