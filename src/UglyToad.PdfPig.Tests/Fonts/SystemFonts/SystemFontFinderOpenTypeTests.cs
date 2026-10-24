namespace UglyToad.PdfPig.Tests.Fonts.SystemFonts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using PdfPig.Fonts.CompactFontFormat;
    using PdfPig.Fonts.SystemFonts;
    using PdfPig.Fonts.TrueType;
    using PdfPig.Fonts.TrueType.Parser;

    public class SystemFontFinderOpenTypeTests
    {
        [SkippableFact]
        public void FindsAnInstalledOpenTypeFont()
        {
            var name = FindInstalledOpenTypeFontName();
            Skip.If(name is null, "Skipped because no OpenType font (.otf/.otc) with CFF outlines that are not CID-keyed is installed.");

            var found = SystemFontFinder.Instance.GetTrueTypeFont(name!);

            Assert.NotNull(found);
            Assert.Equal(name, found.TableRegister.NameTable!.GetPostscriptName());
            Assert.NotNull(found.CffFontCollection);
        }

        /// <summary>
        /// The PostScript name of an installed .otf/.otc face, in a directory the platform's font lister searches,
        /// with CFF outlines that are not CID-keyed (those are not returned yet) and no .ttf/.ttc face of that name.
        /// </summary>
        private static string? FindInstalledOpenTypeFontName()
        {
            IEnumerable<string> files;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                files = GetFiles(SearchOption.TopDirectoryOnly, Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts"));
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                files = GetFiles(SearchOption.TopDirectoryOnly, "/Library/Fonts/", "/System/Library/Fonts/");
            }
            else
            {
                files = GetFiles(SearchOption.AllDirectories, "/usr/local/fonts", "/usr/local/share/fonts", "/usr/share/fonts");
            }

            foreach (var file in files.Where(f => f.EndsWith(".otf", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".otc", StringComparison.OrdinalIgnoreCase)))
            {
                IReadOnlyList<TrueTypeFont> faces;
                try
                {
                    var data = new TrueTypeDataBytes(File.ReadAllBytes(file));
                    faces = file.EndsWith(".otc", StringComparison.OrdinalIgnoreCase)
                        ? TrueTypeFontParser.ParseCollection(data)
                        : new[] { TrueTypeFontParser.Parse(data) };
                }
                catch
                {
                    continue;
                }

                foreach (var face in faces)
                {
                    var cff = face.CffFontCollection?.FirstFont;
                    var name = face.TableRegister.NameTable?.GetPostscriptName();
                    if (cff is not null && cff is not CompactFontFormatCidFont && !cff.Charset.IsCidCharset && !string.IsNullOrEmpty(name))
                    {
                        return name;
                    }
                }
            }

            return null;
        }

        private static IEnumerable<string> GetFiles(SearchOption option, params string[] directories)
        {
            foreach (var directory in directories.Where(Directory.Exists))
            {
                string[] files;
                try
                {
                    files = Directory.GetFiles(directory, "*.*", option);
                }
                catch (Exception)
                {
                    continue;
                }

                foreach (var file in files)
                {
                    yield return file;
                }
            }
        }
    }
}
