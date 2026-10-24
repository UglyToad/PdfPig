namespace UglyToad.PdfPig.Tests.Fonts.SystemFonts
{
    using System.IO;
    using PdfPig.Fonts.SystemFonts;
    using PdfPig.Fonts.TrueType;
    using PdfPig.Fonts.TrueType.Parser;
    using UglyToad.PdfPig.Tests.Fonts.TrueType.Parser;

    public class SystemFontFinderCollectionTests
    {
        [SkippableFact]
        public void FindsEveryFaceOfAnInstalledCollection()
        {
            var path = TrueTypeCollectionParserTests.WindowsCollection;
            Skip.IfNot(File.Exists(path), $"Skipped because {path} is not installed.");

            // Every face, so the later ones are found through the names mapped when the file was first read.
            foreach (var face in TrueTypeFontParser.ParseCollection(new TrueTypeDataBytes(File.ReadAllBytes(path))))
            {
                var name = face.TableRegister.NameTable!.GetPostscriptName()!;

                var found = SystemFontFinder.Instance.GetTrueTypeFont(name);

                Assert.NotNull(found);
                Assert.Equal(name, found.TableRegister.NameTable!.GetPostscriptName());
            }
        }
    }
}
