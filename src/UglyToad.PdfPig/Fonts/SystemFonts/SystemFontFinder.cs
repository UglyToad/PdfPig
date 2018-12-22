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

    internal interface ISystemFontLister
    {
        IEnumerable<SystemFontRecord> GetAllFonts();
    }

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

    internal class LinuxSystemFontLister : ISystemFontLister
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

    internal struct SystemFontRecord
    {
        public string Path { get; }

        public SystemFontType Type { get; }

        public SystemFontRecord(string path, SystemFontType type)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Type = type;
        }

        public static bool TryCreate(string path, out SystemFontRecord type)
        {
            type = default(SystemFontRecord);

            SystemFontType fontType;
            if (path.EndsWith(".ttf"))
            {
                fontType = SystemFontType.TrueType;
            }
            else if (path.EndsWith(".otf"))
            {
                fontType = SystemFontType.OpenType;
            }
            else if (path.EndsWith(".ttc"))
            {
                fontType = SystemFontType.TrueTypeCollection;
            }
            else if (path.EndsWith(".otc"))
            {
                fontType = SystemFontType.OpenTypeCollection;
            }
            else if (path.EndsWith(".pfb"))
            {
                fontType = SystemFontType.Type1;
            }
            else
            {
                return false;
            }

            type = new SystemFontRecord(path, fontType);

            return true;
        }
    }

    internal enum SystemFontType
    {
        Unknown = 0,
        TrueType = 1,
        OpenType = 2,
        Type1 = 3,
        TrueTypeCollection = 4,
        OpenTypeCollection = 5
    }
}