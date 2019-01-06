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

            foreach (var record in availableFonts.Value)
            {
                if (record.Type == SystemFontType.TrueType)
                {
                    using (var fileStream = File.OpenRead(record.Path))
                    {
                        var input = new StreamInputBytes(fileStream);
                        var trueType = trueTypeFontParser.Parse(new TrueTypeDataBytes(input));
                        var psName = trueType.TableRegister.NameTable?.GetPostscriptName() ?? trueType.Name;

                        if (!cache.ContainsKey(psName))
                        {
                            cache[psName] = trueType;
                        }

                        if (string.Equals(psName, name, StringComparison.OrdinalIgnoreCase))
                        {
                            return trueType;
                        }
                    }
                }

                // TODO: OTF
            }

            return null;
        }
    }
}