namespace UglyToad.PdfPig.Tests.Fonts.SystemFonts
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using PdfPig.Fonts.SystemFonts;
    using PdfPig.Fonts.TrueType;
    using PdfPig.Fonts.TrueType.Parser;

    public class SystemFontRecordTests
    {
        [Theory]
        [InlineData(@"C:\Windows\Fonts\arial.ttf", "TrueType")]
        [InlineData(@"C:\Windows\Fonts\ARIALN.TTF", "TrueType")]
        [InlineData("/usr/share/fonts/Font.Ttf", "TrueType")]
        [InlineData("/Library/Fonts/Font.OTF", "OpenType")]
        [InlineData(@"C:\Windows\Fonts\MSGOTHIC.TTC", "TrueTypeCollection")]
        [InlineData("/usr/share/fonts/NotoSansCJK.OTC", "OpenTypeCollection")]
        [InlineData("/usr/share/fonts/type1/font.PFB", "Type1")]
        public void ExtensionIsMatchedIgnoringCase(string path, string expectedType)
        {
            Assert.True(SystemFontRecord.TryCreate(path, out var record));
            Assert.Equal(expectedType, record.Type.ToString());
            Assert.Equal(path, record.Path);
        }

        [Theory]
        [InlineData("font.fon")]
        [InlineData("FONT.FON")]
        [InlineData("readme.txt")]
        [InlineData("ttf")]
        [InlineData("font.ttf.bak")]
        public void OtherFilesAreNotFonts(string path)
        {
            Assert.False(SystemFontRecord.TryCreate(path, out _));
        }

        [SkippableFact]
        public void FindsAFontInstalledWithAnUpperCaseExtension()
        {
            Skip.IfNot(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), "Skipped because the test looks in the Windows font directory.");

            var fontsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts");
            var name = Directory.GetFiles(fontsDirectory, "*.TTF")
                .Where(f => f.EndsWith(".TTF", StringComparison.Ordinal))
                .Select(GetPostScriptName)
                .FirstOrDefault(n => !string.IsNullOrEmpty(n));

            Skip.If(name is null, "Skipped because no font with an upper-case .TTF extension is installed.");

            var found = SystemFontFinder.Instance.GetTrueTypeFont(name!);

            Assert.NotNull(found);
            Assert.Equal(name, found.TableRegister.NameTable!.GetPostscriptName());
        }

        private static string? GetPostScriptName(string path)
        {
            try
            {
                return TrueTypeFontParser.GetNameTable(new TrueTypeDataBytes(File.ReadAllBytes(path)))?.GetPostscriptName();
            }
            catch
            {
                return null;
            }
        }
    }
}
