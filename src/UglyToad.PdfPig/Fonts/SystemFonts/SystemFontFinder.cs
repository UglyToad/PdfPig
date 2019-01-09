namespace UglyToad.PdfPig.Fonts.SystemFonts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using IO;
    using TrueType;
    using TrueType.Parser;

    internal class SystemFontFinder : ISystemFontFinder
    {
        private readonly TrueTypeFontParser trueTypeFontParser;
        private readonly Lazy<IReadOnlyList<SystemFontRecord>> availableFonts;

        private readonly Dictionary<string, TrueTypeFontProgram> cache = new Dictionary<string, TrueTypeFontProgram>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> readFiles = new HashSet<string>();

        public SystemFontFinder(TrueTypeFontParser trueTypeFontParser)
        {
            this.trueTypeFontParser = trueTypeFontParser;

            ISystemFontLister lister;
#if NETSTANDARD2_0
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                lister = new WindowsSystemFontLister();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                lister = new MacSystemFontLister();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                lister = new LinuxSystemFontLister();
            }
            else
            {
                throw new NotSupportedException($"Unsupported operating system: {RuntimeInformation.OSDescription}.");
            }
#else 
            lister = new WindowsSystemFontLister();
#endif

            availableFonts = new Lazy<IReadOnlyList<SystemFontRecord>>(() => lister.GetAllFonts().ToList());
        }

        public TrueTypeFontProgram GetTrueTypeFont(string name)
        {
            if (cache.TryGetValue(name, out var result))
            {
                return result;
            }

            var nameCandidates = availableFonts.Value.Where(x => Path.GetFileName(x.Path)?.StartsWith(name[0].ToString(), StringComparison.OrdinalIgnoreCase) == true);

            foreach (var systemFontRecord in nameCandidates)
            {
                if (TryGetTrueTypeFont(name, systemFontRecord, out var font))
                {
                    return font;
                }
            }

            foreach (var record in availableFonts.Value)
            {
                if (TryGetTrueTypeFont(name, record, out var font))
                {
                    return font;
                }

                // TODO: OTF
            }

            return null;
        }

        private bool TryGetTrueTypeFont(string name, SystemFontRecord record, out TrueTypeFontProgram font)
        {
            font = null;
            if (record.Type == SystemFontType.TrueType)
            {
                if (readFiles.Contains(record.Path))
                {
                    return false;
                }

                using (var fileStream = File.OpenRead(record.Path))
                {
                    readFiles.Add(record.Path);

                    var input = new StreamInputBytes(fileStream);
                    var trueType = trueTypeFontParser.Parse(new TrueTypeDataBytes(input));
                    var psName = trueType.TableRegister.NameTable?.GetPostscriptName() ?? trueType.Name;
                    if (!cache.ContainsKey(psName))
                    {
                        cache[psName] = trueType;
                    }

                    if (string.Equals(psName, name, StringComparison.OrdinalIgnoreCase))
                    {
                        font = trueType;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}