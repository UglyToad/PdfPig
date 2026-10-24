namespace UglyToad.PdfPig.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Fonts.CompactFontFormat;
    using UglyToad.PdfPig.Fonts.Standard14Fonts;
    using UglyToad.PdfPig.Fonts.SystemFonts;
    using UglyToad.PdfPig.Fonts.TrueType;
    using UglyToad.PdfPig.Fonts.TrueType.Parser;
    using UglyToad.PdfPig.Fonts.TrueType.Subsetting;
    using UglyToad.PdfPig.Fonts.Type1.Parser;
    using UglyToad.PdfPig.PdfFonts.Simple;

    /// <summary>
    /// Substitutes are registered process-wide, so these tests must not run alongside the others.
    /// </summary>
    [CollectionDefinition(nameof(SubstituteFontTests), DisableParallelization = true)]
    public class SubstituteFontTestsCollection
    {
    }

    [Collection(nameof(SubstituteFontTests))]
    public class SubstituteFontTests
    {
        // Code 65 is renamed to /A, a glyph both test programs have; code 66 keeps its built-in
        // Standard 14 name, which the test programs do not have.
        private const string DifferencesA = "/Encoding << /Type /Encoding /Differences [65 /A] >>";

        private const string Descriptor =
            "<< /Type /FontDescriptor /FontName /{0} /Flags 4 /FontBBox [0 0 1000 1000] /ItalicAngle 0 /Ascent 800 /Descent -200 /CapHeight 700 /StemV 80 >>";

        private static byte[] MinionPro => File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "CompactFontFormat", "MinionPro.bin"));

        private static byte[] RalewayPfb => File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Fonts", "Type1", "Raleway-Black.pfb"));

        [Fact]
        public void Standard14FontWithoutSubstituteHasNoOutline()
        {
            var pdf = BuildPdf($"<< /Type /Font /Subtype /Type1 /BaseFont /ZapfDingbats {DifferencesA} >>", "(A)");

            var letter = GetLetters(pdf).Single();

            Assert.False(letter.GetFont()!.TryGetNormalisedPath(65, out _));
        }

        [Fact]
        public void Standard14FontUsesSubstituteOutline()
        {
            using var registration = Register(MinionPro, Standard14Font.ZapfDingbats);
            var pdf = BuildPdf($"<< /Type /Font /Subtype /Type1 /BaseFont /ZapfDingbats {DifferencesA} >>", "(A)");

            var font = GetLetters(pdf).Single().GetFont()!;

            Assert.IsType<Type1Standard14Font>(font);
            Assert.True(font.TryGetPath(65, out var glyphSpacePath));
            Assert.True(font.TryGetNormalisedPath(65, out var path));

            var expected = GetMinionProBounds("A");
            AssertBounds(expected, glyphSpacePath, 1);
            AssertBounds(expected, path, 0.001);
        }

        [Fact]
        public void SubstituteDoesNotChangeTextOrMetrics()
        {
            var pdf = BuildPdf($"<< /Type /Font /Subtype /Type1 /BaseFont /ZapfDingbats {DifferencesA} >>", "(AB)");

            var without = GetLetters(pdf);
            IReadOnlyList<Letter> with;
            using (Register(MinionPro, Standard14Font.ZapfDingbats))
            {
                with = GetLetters(pdf);
            }

            Assert.Equal(without.Select(l => l.Value), with.Select(l => l.Value));
            Assert.Equal(without.Select(l => l.GlyphRectangle), with.Select(l => l.GlyphRectangle));
            Assert.Equal(without.Select(l => l.Width), with.Select(l => l.Width));
        }

        [Fact]
        public void GlyphMissingFromSubstituteHasNoOutline()
        {
            using var registration = Register(MinionPro, Standard14Font.ZapfDingbats);
            var pdf = BuildPdf($"<< /Type /Font /Subtype /Type1 /BaseFont /ZapfDingbats {DifferencesA} >>", "(B)");

            var font = GetLetters(pdf).Single().GetFont()!;

            // Code 66 is ZapfDingbats' /a29, which MinionPro does not have: no outline rather than the .notdef box.
            Assert.False(font.TryGetNormalisedPath(66, out _));
        }

        [Fact]
        public void Type1FontWithDescriptorButNoProgramUsesSubstitute()
        {
            using var registration = Register(MinionPro, Standard14Font.Symbol);
            var pdf = BuildPdf(
                $"<< /Type /Font /Subtype /Type1 /BaseFont /Symbol /FirstChar 65 /LastChar 65 /Widths [500] /FontDescriptor 6 0 R {DifferencesA} >>",
                "(A)",
                string.Format(Descriptor, "Symbol"));

            var font = GetLetters(pdf).Single().GetFont()!;

            Assert.IsType<Type1FontSimple>(font);
            Assert.True(font.TryGetNormalisedPath(65, out var path));
            AssertBounds(GetMinionProBounds("A"), path, 0.001);
        }

        [Fact]
        public void TrueTypeStandard14FontUsesSubstituteOutline()
        {
            var pdf = BuildPdf($"<< /Type /Font /Subtype /TrueType /BaseFont /Symbol {DifferencesA} >>", "(A)");

            using var registration = Register(MinionPro, Standard14Font.Symbol);
            var font = GetLetters(pdf).Single().GetFont()!;

            Assert.IsType<TrueTypeStandard14FallbackSimpleFont>(font);
            Assert.True(font.TryGetPath(65, out var glyphSpacePath));
            Assert.True(font.TryGetNormalisedPath(65, out var path));
            AssertBounds(GetMinionProBounds("A"), glyphSpacePath, 1);
            AssertBounds(GetMinionProBounds("A"), path, 0.001);
        }

        [Fact]
        public void TrueTypeStandard14FontKeepsItsWidthsAndNameWithASubstitute()
        {
            // No descriptor, so this is read as a Standard 14 font, but /Widths still overrides the AFM width.
            var pdf = BuildPdf("<< /Type /Font /Subtype /TrueType /BaseFont /Arial /FirstChar 65 /LastChar 65 /Widths [999] >>", "(A)");

            var without = GetLetters(pdf).Single();
            Letter with;
            using (Register(Roboto, Standard14Font.Helvetica))
            {
                with = GetLetters(pdf).Single();
                Assert.True(with.GetFont()!.TryGetNormalisedPath(65, out _));
            }

            Assert.Equal("Arial", with.FontName);
            Assert.Equal(without.FontName, with.FontName);
            Assert.Equal(without.Value, with.Value);
            Assert.Equal(without.Width, with.Width, 6);
            Assert.Equal(0.999 * 12, with.Width, 6);
        }

        [Fact]
        public void TrueTypeZapfDingbatsUsesSubstitute()
        {
            using var registration = Register(SingleGlyphFont(DingbatA29, unicode: true), Standard14Font.ZapfDingbats);
            var pdf = BuildPdf("<< /Type /Font /Subtype /TrueType /BaseFont /ZapfDingbats >>", "(B)");

            var letter = GetLetters(pdf).Single();

            // Code 66 is ZapfDingbats' /a29, U+2722.
            Assert.Equal("✢", letter.Value);
            Assert.True(letter.GetFont()!.TryGetNormalisedPath(66, out var path));
            AssertBounds(GetRobotoBounds('a'), path, 1);
        }

        [Fact]
        public void DifferencesOnTrueTypeSymbolicStandard14FontApplyToItsBuiltInEncoding()
        {
            // Code 97 keeps Symbol's built-in name (/alpha), not StandardEncoding's /a.
            var pdf = BuildPdf($"<< /Type /Font /Subtype /TrueType /BaseFont /Symbol {DifferencesA} >>", "(a)");

            Assert.Equal("α", GetLetters(pdf).Single().Value);
        }

        [Fact]
        public void DifferencesOnSymbolicStandard14FontWithDescriptorApplyToItsBuiltInEncoding()
        {
            var pdf = BuildPdf(
                $"<< /Type /Font /Subtype /Type1 /BaseFont /Symbol /FirstChar 97 /LastChar 97 /Widths [631] /FontDescriptor 6 0 R {DifferencesA} >>",
                "(a)",
                string.Format(Descriptor, "Symbol"));

            Assert.Equal("α", GetLetters(pdf).Single().Value);

            // The substitute is asked for /alpha (U+03B1), which is all it has.
            using var registration = Register(SingleGlyphFont(GreekAlpha, unicode: true), Standard14Font.Symbol);
            var font = GetLetters(pdf).Single().GetFont()!;

            Assert.IsType<Type1FontSimple>(font);
            Assert.True(font.TryGetNormalisedPath(97, out var path));
            AssertBounds(GetRobotoBounds('a'), path, 1);
        }

        [Fact]
        public void ZapfDingbatsGlyphsAreFoundInATrueTypeSubstituteByUnicode()
        {
            using var registration = Register(SingleGlyphFont(DingbatA29, unicode: true), Standard14Font.ZapfDingbats);
            var pdf = BuildPdf("<< /Type /Font /Subtype /Type1 /BaseFont /ZapfDingbats >>", "(B)");

            Assert.True(SystemFontFinder.TryGetSubstituteFont("ZapfDingbats", out var substitute));
            Assert.True(substitute!.HasGlyph("a29"));
            Assert.False(substitute.HasGlyph("a1"));

            Assert.True(GetLetters(pdf).Single().GetFont()!.TryGetNormalisedPath(66, out var path));
            AssertBounds(GetRobotoBounds('a'), path, 1);
        }

        [Theory]
        [InlineData(0xF000 + 0x61)]
        [InlineData(0x61)]
        public void SymbolGlyphsAreFoundThroughASymbolCmap(int mappedCode)
        {
            // A (3,0) cmap is addressed by Symbol's own codes (/alpha is 0x61), usually moved to U+F0xx.
            using var registration = Register(SingleGlyphFont(mappedCode, unicode: false), Standard14Font.Symbol);
            var pdf = BuildPdf("<< /Type /Font /Subtype /Type1 /BaseFont /Symbol >>", "(a)");

            Assert.True(SystemFontFinder.TryGetSubstituteFont("Symbol", out var substitute));
            Assert.True(substitute!.HasGlyph("alpha"));
            Assert.False(substitute.HasGlyph("beta"));

            Assert.True(GetLetters(pdf).Single().GetFont()!.TryGetNormalisedPath(97, out var path));
            AssertBounds(GetRobotoBounds('a'), path, 1);
        }

        [Fact]
        public void NonEmbeddedTrueTypeFontWithDescriptorIsReadAsSubstitutedStandard14Font()
        {
            using var registration = Register(MinionPro, Standard14Font.Symbol);
            var pdf = BuildPdf(
                "<< /Type /Font /Subtype /TrueType /BaseFont /Symbol /FirstChar 97 /LastChar 97 /Widths [631] /FontDescriptor 6 0 R >>",
                "(a)",
                string.Format(Descriptor, "Symbol"));

            var font = GetLetters(pdf).Single().GetFont()!;

            // Read from the document and Standard 14 metrics, not from whichever "Symbol" TrueType font is installed.
            Assert.IsType<Type1FontSimple>(font);
            Assert.Equal(0.631, font.GetBoundingBox(97).Width, 6);
        }

        [Fact]
        public void DifferencesOnSymbolicStandard14FontApplyToItsBuiltInEncoding()
        {
            // Code 65 is renamed, 66 keeps ZapfDingbats' built-in name (/a29), not StandardEncoding's /B.
            var pdf = BuildPdf($"<< /Type /Font /Subtype /Type1 /BaseFont /ZapfDingbats {DifferencesA} >>", "(AB)");

            var letters = GetLetters(pdf);

            Assert.Equal("A", letters[0].Value);
            Assert.Equal("✢", letters[1].Value);
        }

        [Fact]
        public void Type1PfbProgramIsSupported()
        {
            using var registration = Register(RalewayPfb, Standard14Font.ZapfDingbats);
            var pdf = BuildPdf($"<< /Type /Font /Subtype /Type1 /BaseFont /ZapfDingbats {DifferencesA} >>", "(A)");

            var font = GetLetters(pdf).Single().GetFont()!;

            Assert.True(font.TryGetNormalisedPath(65, out var path));
            Assert.NotEmpty(path);
        }

        [Fact]
        public void FontWithoutSubstituteIsUnchanged()
        {
            using var registration = Register(MinionPro, Standard14Font.ZapfDingbats);
            var pdf = BuildPdf($"<< /Type /Font /Subtype /Type1 /BaseFont /Symbol {DifferencesA} >>", "(A)");

            Assert.False(GetLetters(pdf).Single().GetFont()!.TryGetNormalisedPath(65, out _));
        }

        [Fact]
        public void AlternativeNamesMapToTheSubstitute()
        {
            using var registration = Register(MinionPro, Standard14Font.Symbol);

            Assert.True(SystemFontFinder.TryGetSubstituteFont("Symbol,Bold", out var substitute));
            Assert.Equal(Standard14Font.Symbol, substitute!.Font);
            Assert.False(SystemFontFinder.TryGetSubstituteFont("ZapfDingbats", out _));
            Assert.False(SystemFontFinder.TryGetSubstituteFont("NotAFont", out _));
        }

        [Fact]
        public void RemovingTheSubstituteRestoresTheDefault()
        {
            var pdf = BuildPdf($"<< /Type /Font /Subtype /Type1 /BaseFont /ZapfDingbats {DifferencesA} >>", "(A)");

            using (Register(MinionPro, Standard14Font.ZapfDingbats))
            {
                Assert.True(GetLetters(pdf).Single().GetFont()!.TryGetNormalisedPath(65, out _));
            }

            Assert.False(SystemFontFinder.TryGetSubstituteFont("ZapfDingbats", out _));
            Assert.False(GetLetters(pdf).Single().GetFont()!.TryGetNormalisedPath(65, out _));
        }

        [Fact]
        public void OpenTypeProgramWithCffOutlinesIsSupported()
        {
            using var registration = Register(OpenTypeCffFont(DingbatA29), Standard14Font.Helvetica);
            var pdf = BuildPdf("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>", "(A)");

            // /A is found by its CFF glyph name.
            Assert.True(GetLetters(pdf).Single().GetFont()!.TryGetNormalisedPath(65, out var path));
            AssertBounds(GetMinionProBounds("A"), path, 0.001);
        }

        [Fact]
        public void OpenTypeCffGlyphsAreFoundThroughTheCmap()
        {
            // /a29 is not a CFF glyph name: U+2722 through the cmap gives the glyph index of the CFF's /A.
            using var registration = Register(OpenTypeCffFont(DingbatA29), Standard14Font.ZapfDingbats);
            var pdf = BuildPdf("<< /Type /Font /Subtype /Type1 /BaseFont /ZapfDingbats >>", "(B)");

            Assert.True(GetLetters(pdf).Single().GetFont()!.TryGetNormalisedPath(66, out var path));
            AssertBounds(GetMinionProBounds("A"), path, 0.001);
        }

        [Theory]
        [InlineData(Standard14Font.HelveticaBold, 'o', "B")]
        [InlineData(Standard14Font.Helvetica, 'a', "A")]
        public void CollectionUsesTheFacesNamedAfterTheSubstitutedFont(Standard14Font font, char expectedGlyph, string missingName)
        {
            // Helvetica has /B (Roboto's a), Helvetica-Bold has /A (Roboto's o): each face only has its own glyph.
            var collection = FontCollection(
                SingleGlyphFont('B', unicode: true, glyph: 'a', postScriptName: "Helvetica"),
                SingleGlyphFont('A', unicode: true, glyph: 'o', postScriptName: "Helvetica-Bold"));

            using var registration = Register(collection, font);
            Assert.True(SystemFontFinder.TryGetSubstituteFont(Standard14.GetAdobeFontMetrics(font).FontName, out var substitute));

            var presentName = missingName == "A" ? "B" : "A";
            Assert.True(substitute!.TryGetNormalisedPath(presentName, out var path));
            AssertBounds(GetRobotoBounds(expectedGlyph), path!, 1);

            // The other face's glyph is not used.
            Assert.False(substitute.HasGlyph(missingName));
        }

        [Fact]
        public void CollectionWithoutAFaceNamedAfterTheFontUsesAllFacesInOrder()
        {
            var collection = FontCollection(
                SingleGlyphFont('A', unicode: true, glyph: 'a', postScriptName: "Roboto-Regular"),
                SingleGlyphFont('A', unicode: true, glyph: 'o', postScriptName: "Roboto-Bold"),
                SingleGlyphFont('B', unicode: true, glyph: 'o', postScriptName: "Roboto-Italic"));

            using var registration = Register(collection, Standard14Font.Courier);
            Assert.True(SystemFontFinder.TryGetSubstituteFont("Courier", out var substitute));

            // /A from the first face that has it, /B from the only one.
            Assert.True(substitute!.TryGetNormalisedPath("A", out var a));
            AssertBounds(GetRobotoBounds('a'), a!, 1);
            Assert.True(substitute.TryGetNormalisedPath("B", out var b));
            AssertBounds(GetRobotoBounds('o'), b!, 1);
        }

        [Fact]
        public void CollectionFaceThatCannotBeReadIsSkipped()
        {
            var collection = FontCollection(
                new byte[64],
                SingleGlyphFont('A', unicode: true, postScriptName: "Courier"));

            using var registration = Register(collection, Standard14Font.Courier);

            Assert.True(SystemFontFinder.TryGetSubstituteFont("Courier", out var substitute));
            Assert.True(substitute!.HasGlyph("A"));
        }

        [Fact]
        public void InvalidCollectionIsRejected()
        {
            // No faces, then more faces than the file could hold, then a face that is not a font.
            byte[] empty = [(byte)'t', (byte)'t', (byte)'c', (byte)'f', 0, 1, 0, 0, 0, 0, 0, 0];
            byte[] tooMany = [(byte)'t', (byte)'t', (byte)'c', (byte)'f', 0, 1, 0, 0, 0x7F, 0xFF, 0xFF, 0xFF];

            Assert.Throws<ArgumentException>(() => SystemFontFinder.AddSubstituteFont(Standard14Font.Courier, empty));
            Assert.Throws<ArgumentException>(() => SystemFontFinder.AddSubstituteFont(Standard14Font.Courier, tooMany));
            Assert.Throws<ArgumentException>(() => SystemFontFinder.AddSubstituteFont(Standard14Font.Courier, FontCollection(new byte[64])));
            Assert.False(SystemFontFinder.TryGetSubstituteFont("Courier", out _));
        }

        [Fact]
        public void InvalidProgramIsRejected()
        {
            Assert.Throws<ArgumentException>(() => SystemFontFinder.AddSubstituteFont(Standard14Font.ZapfDingbats, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }));
            Assert.Throws<ArgumentException>(() => SystemFontFinder.AddSubstituteFont(Standard14Font.ZapfDingbats, new byte[0]));
            Assert.Throws<ArgumentException>(() => SystemFontFinder.AddSubstituteFont(Standard14Font.ZapfDingbats, MinionPro, new byte[] { 1, 2, 3, 4 }));
            Assert.False(SystemFontFinder.TryGetSubstituteFont("ZapfDingbats", out _));
        }

        [Fact]
        public void TrueTypeProgramIsSupported()
        {
            using var registration = Register(Roboto, Standard14Font.Helvetica);
            var pdf = BuildPdf("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>", "(A)");

            var font = GetLetters(pdf).Single().GetFont()!;

            Assert.True(font.TryGetNormalisedPath(65, out var path));
            var bounds = PdfSubpath.GetBoundingRectangle(path)!.Value;
            // Roughly an em-sized capital, in text space.
            Assert.InRange(bounds.Height, 0.5, 1);
        }

        [Fact]
        public void TrueTypeGlyphsAreFoundByUnicodeWhenNotNamed()
        {
            using var registration = Register(Roboto, Standard14Font.Helvetica);
            Assert.True(SystemFontFinder.TryGetSubstituteFont("Helvetica", out var substitute));

            // /eacute through the post table or, failing that, U+00E9 through the cmap; /uni00E9 only through the cmap.
            Assert.True(substitute!.HasGlyph("eacute"));
            Assert.True(substitute.HasGlyph("uni00E9"));
            Assert.False(substitute.HasGlyph("definitely.not.a.glyph"));
        }

        [Fact]
        public void SubsetTaggedType1FontWithoutProgramUsesSubstituteAndStandard14Metrics()
        {
            const string font = "<< /Type /Font /Subtype /Type1 /BaseFont /{0} /FirstChar 65 /LastChar 65 /Widths [667] /FontDescriptor 6 0 R >>";
            var exact = GetLetters(BuildPdf(string.Format(font, "Helvetica"), "(A)", string.Format(Descriptor, "Helvetica"))).Single();
            var pdf = BuildPdf(string.Format(font, "ABCDEF+Helvetica"), "(A)", string.Format(Descriptor, "ABCDEF+Helvetica"));

            // The glyph box comes from Helvetica's metrics, as for the exact name, not the descriptor's fallback box.
            Assert.Equal(exact.GlyphRectangle, GetLetters(pdf).Single().GlyphRectangle);

            using var registration = Register(Roboto, Standard14Font.Helvetica);
            var letter = GetLetters(pdf).Single();

            Assert.Equal("ABCDEF+Helvetica", letter.FontName);
            Assert.True(letter.GetFont()!.TryGetNormalisedPath(65, out _));
        }

        [Fact]
        public void LooselyNamedType1FontWithoutDescriptorUsesItsStandard14Font()
        {
            // Not a Standard 14 name as such, so this used to fall back to Times-Roman.
            var exact = GetLetters(BuildPdf("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>", "(A)")).Single();
            var pdf = BuildPdf("<< /Type /Font /Subtype /Type1 /BaseFont /ABCDEF+Arial,Bold >>", "(A)");

            var letter = GetLetters(pdf).Single();
            Assert.Equal(exact.Width, letter.Width, 6);
            Assert.Equal(exact.GlyphRectangle, letter.GlyphRectangle);

            using var registration = Register(Roboto, Standard14Font.HelveticaBold);
            Assert.True(GetLetters(pdf).Single().GetFont()!.TryGetNormalisedPath(65, out _));
        }

        [Fact]
        public void SubsetTaggedNonEmbeddedTrueTypeFontUsesSubstitute()
        {
            using var registration = Register(Roboto, Standard14Font.Helvetica);
            var pdf = BuildPdf(
                "<< /Type /Font /Subtype /TrueType /BaseFont /ABCDEF+Arial /FirstChar 65 /LastChar 65 /Widths [999] /FontDescriptor 6 0 R >>",
                "(A)",
                string.Format(Descriptor, "ABCDEF+Arial"));

            var letter = GetLetters(pdf).Single();

            Assert.Equal("ABCDEF+Arial", letter.FontName);
            Assert.Equal(0.999 * 12, letter.Width, 6);

            // The substitute's outline, not one from an Arial the machine may have installed.
            Assert.True(letter.GetFont()!.TryGetNormalisedPath(65, out var path));
            AssertBounds(GetRobotoBounds('A'), path, 1);
        }

        [Fact]
        public void LooseNamesMapToTheSubstitute()
        {
            using var registration = Register(MinionPro, Standard14Font.HelveticaBold);

            Assert.True(SystemFontFinder.TryGetSubstituteFont("ABCDEF+Helvetica-Bold", out var substitute));
            Assert.Equal(Standard14Font.HelveticaBold, substitute!.Font);
            Assert.True(SystemFontFinder.TryGetSubstituteFont("arial bold", out _));
            Assert.False(SystemFontFinder.TryGetSubstituteFont("ABCDEF+Helvetica", out _));
        }

        [Theory]
        [InlineData("ff", "f_f")]
        [InlineData("ffi", "f_f_i")]
        [InlineData("ffl", "f_f_l")]
        [InlineData("fi", "f_i")]
        [InlineData("fl", "f_l")]
        [InlineData("st", "s_t")]
        public void LigaturesAreFoundUnderTheirComponentNames(string standardName, string nameInFont)
        {
            // Raleway names its ligatures after their components only.
            using var registration = Register(RalewayPfb, Standard14Font.Helvetica);

            Assert.True(SystemFontFinder.TryGetSubstituteFont("Helvetica", out var substitute));
            Assert.True(substitute!.TryGetNormalisedPath(standardName, out var path));
            AssertBounds(GetRalewayBounds(nameInFont), path!, 1);
        }

        [Fact]
        public void Standard14LigatureUsesTheAlternativeNameInTheSubstitute()
        {
            using var registration = Register(RalewayPfb, Standard14Font.Helvetica);
            // No /Encoding: Helvetica's built-in StandardEncoding, where code 174 (octal 256) is /fi.
            var pdf = BuildPdf("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>", "(\\256)");

            var letter = GetLetters(pdf).Single();

            Assert.Equal("ﬁ", letter.Value);
            Assert.True(letter.GetFont()!.TryGetNormalisedPath(174, out var path));
            AssertBounds(GetRalewayBounds("f_i"), path, 1);
        }

        [Fact]
        public void LaterProgramsFillGapsInEarlierOnes()
        {
            // Both programs have /A: it must come from the first one, MinionPro. /uni00E9 is not a MinionPro glyph
            // name, so it can only come from Roboto (through its cmap). Neither has ZapfDingbats' /a9.
            SystemFontFinder.AddSubstituteFont(Standard14Font.Helvetica, MinionPro, Roboto);
            try
            {
                Assert.True(SystemFontFinder.TryGetSubstituteFont("Helvetica", out var substitute));
                Assert.True(substitute!.TryGetNormalisedPath("A", out var path));
                AssertBounds(GetMinionProBounds("A"), path!, 0.001);

                Assert.True(substitute.HasGlyph("uni00E9"));
                Assert.False(substitute.HasGlyph("a9"));
            }
            finally
            {
                SystemFontFinder.RemoveSubstituteFont(Standard14Font.Helvetica);
            }
        }

        [Fact]
        public void LoadersRunOnlyWhenAGlyphIsNeededAndOnlyOnce()
        {
            int calls = 0;
            SystemFontFinder.AddSubstituteFont(Standard14Font.ZapfDingbats, () => { calls++; return MinionPro; });
            try
            {
                Assert.Equal(0, calls);

                var pdf = BuildPdf($"<< /Type /Font /Subtype /Type1 /BaseFont /ZapfDingbats {DifferencesA} >>", "(A)");
                var font = GetLetters(pdf).Single().GetFont()!;
                Assert.Equal(0, calls);

                Assert.True(font.TryGetNormalisedPath(65, out _));
                Assert.True(font.TryGetNormalisedPath(65, out _));
                Assert.Equal(1, calls);
            }
            finally
            {
                SystemFontFinder.RemoveSubstituteFont(Standard14Font.ZapfDingbats);
            }
        }

        [Fact]
        public void FailingLoaderIsSkipped()
        {
            SystemFontFinder.AddSubstituteFont(Standard14Font.ZapfDingbats,
                () => throw new InvalidOperationException("boom"),
                () => MinionPro);
            try
            {
                Assert.True(SystemFontFinder.TryGetSubstituteFont("ZapfDingbats", out var substitute));
                Assert.True(substitute!.HasGlyph("A"));
            }
            finally
            {
                SystemFontFinder.RemoveSubstituteFont(Standard14Font.ZapfDingbats);
            }
        }

        private static byte[] Roboto => File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts", "TrueType", "Roboto-Regular.ttf"));

        private const int DingbatA29 = 0x2722;

        private const int GreekAlpha = 0x03B1;

        /// <summary>
        /// Roboto cut down to its <paramref name="glyph"/> glyph, as glyph 1, with no post table and a cmap whose only
        /// entry maps <paramref name="code"/> to it: a (3,1) Unicode cmap if <paramref name="unicode"/>, else a (3,0)
        /// symbol one. With <paramref name="postScriptName"/>, its name table holds only that PostScript name.
        /// </summary>
        private static byte[] SingleGlyphFont(int code, bool unicode, char glyph = 'a', string? postScriptName = null)
        {
            var font = TrueTypeSubsetter.Subset(Roboto, new TrueTypeSubsetEncoding(new[] { glyph })).ToList();
            ReplaceTable(font, "cmap", Format4Cmap(code, 1, unicode));
            if (postScriptName is not null)
            {
                ReplaceTable(font, "name", PostScriptNameTable(postScriptName));
            }

            return font.ToArray();
        }

        /// <summary>
        /// An OpenType font with PostScript outlines ("OTTO"): the <see cref="SingleGlyphFont"/> tables with MinionPro's
        /// CFF program in place of glyf, and a (3,1) cmap mapping <paramref name="code"/> to MinionPro's /A.
        /// </summary>
        private static byte[] OpenTypeCffFont(int code)
        {
            Assert.True(CompactFontFormatParser.Parse(new CompactFontFormatData(MinionPro)).FirstFont.TryGetGlyphIndexByName("A", out int glyphA));

            var font = TrueTypeSubsetter.Subset(Roboto, new TrueTypeSubsetEncoding(new[] { 'a' })).ToList();
            ReplaceTable(font, "glyf", MinionPro, "CFF ");
            ReplaceTable(font, "cmap", Format4Cmap(code, glyphA, unicode: true));
            for (int i = 0; i < 4; i++)
            {
                font[i] = (byte)"OTTO"[i];
            }

            return font.ToArray();
        }

        /// <summary>
        /// A TrueType collection ("ttcf") of <paramref name="faces"/>, in order.
        /// </summary>
        private static byte[] FontCollection(params byte[][] faces)
        {
            var collection = new List<byte>();
            AddUInt32(collection, 0x74746366); // "ttcf"
            AddUInt32(collection, 0x00010000);
            AddUInt32(collection, faces.Length);
            int offsetsStart = collection.Count;
            collection.AddRange(new byte[4 * faces.Length]);

            for (int f = 0; f < faces.Length; f++)
            {
                Pad(collection);
                int faceStart = collection.Count;
                WriteUInt32(collection, offsetsStart + (4 * f), faceStart);
                collection.AddRange(faces[f]);

                // Table offsets in a collection are from the start of the file.
                int numTables = (faces[f][4] << 8) | faces[f][5];
                for (int t = 0; t < numTables; t++)
                {
                    int entry = faceStart + 12 + (16 * t);
                    WriteUInt32(collection, entry + 8, ReadUInt32(collection, entry + 8) + faceStart);
                }
            }

            return collection.ToArray();
        }

        /// <summary>
        /// A cmap with a single platform 3 record, (3,1) or (3,0), whose format 4 subtable maps only
        /// <paramref name="code"/> to <paramref name="glyphIndex"/>.
        /// </summary>
        private static byte[] Format4Cmap(int code, int glyphIndex, bool unicode)
        {
            var cmap = new List<byte>();

            // Header: version 0, one encoding record at offset 12.
            AddUInt16s(cmap, 0, 1, 3, unicode ? 1 : 0);
            AddUInt32(cmap, 12);

            // Format 4, length 32, two segments: [code, code] -> glyphIndex and the closing [0xFFFF, 0xFFFF].
            AddUInt16s(cmap, 4, 32, 0, 4, 4, 1, 0);
            AddUInt16s(cmap, code, 0xFFFF, 0, code, 0xFFFF);
            AddUInt16s(cmap, (glyphIndex - code) & 0xFFFF, 1, 0, 0);

            return cmap.ToArray();
        }

        /// <summary>
        /// A format 0 name table holding only <paramref name="postScriptName"/>, as a Windows Unicode name 6 record.
        /// </summary>
        private static byte[] PostScriptNameTable(string postScriptName)
        {
            var value = Encoding.BigEndianUnicode.GetBytes(postScriptName);

            var name = new List<byte>();
            AddUInt16s(name, 0, 1, 6 + 12);
            AddUInt16s(name, 3, 1, 0x0409, 6, value.Length, 0);
            name.AddRange(value);

            return name.ToArray();
        }

        /// <summary>
        /// Append <paramref name="table"/> to <paramref name="font"/> and point the directory entry for
        /// <paramref name="tag"/> at it, renaming it to <paramref name="newTag"/> if given.
        /// </summary>
        private static void ReplaceTable(List<byte> font, string tag, byte[] table, string? newTag = null)
        {
            Pad(font);
            int offset = font.Count;
            font.AddRange(table);

            int numTables = (font[4] << 8) | font[5];
            for (int i = 0; i < numTables; i++)
            {
                int entry = 12 + (16 * i);
                if (font[entry] == tag[0] && font[entry + 1] == tag[1] && font[entry + 2] == tag[2] && font[entry + 3] == tag[3])
                {
                    if (newTag is not null)
                    {
                        for (int c = 0; c < 4; c++)
                        {
                            font[entry + c] = (byte)newTag[c];
                        }
                    }

                    WriteUInt32(font, entry + 8, offset);
                    WriteUInt32(font, entry + 12, table.Length);
                    return;
                }
            }

            throw new InvalidOperationException($"The font has no {tag} table.");
        }

        private static void Pad(List<byte> bytes)
        {
            while (bytes.Count % 4 != 0)
            {
                bytes.Add(0);
            }
        }

        private static void AddUInt16s(List<byte> bytes, params int[] values)
        {
            foreach (int value in values)
            {
                bytes.Add((byte)(value >> 8));
                bytes.Add((byte)value);
            }
        }

        private static void AddUInt32(List<byte> bytes, int value)
        {
            bytes.AddRange(new byte[4]);
            WriteUInt32(bytes, bytes.Count - 4, value);
        }

        private static int ReadUInt32(List<byte> bytes, int index)
        {
            return (bytes[index] << 24) | (bytes[index + 1] << 16) | (bytes[index + 2] << 8) | bytes[index + 3];
        }

        private static void WriteUInt32(List<byte> bytes, int index, int value)
        {
            bytes[index] = (byte)(value >> 24);
            bytes[index + 1] = (byte)(value >> 16);
            bytes[index + 2] = (byte)(value >> 8);
            bytes[index + 3] = (byte)value;
        }

        /// <summary>
        /// The bounds of Raleway's glyph called <paramref name="name"/> in text space (1 unit = 1 em).
        /// </summary>
        private static PdfRectangle GetRalewayBounds(string name)
        {
            var font = Type1FontParser.Parse(new MemoryInputBytes(RalewayPfb), 0, 0, new StackDepthGuard(256));
            var path = font.GetCharacterPath(name);
            Assert.NotNull(path);
            return font.FontMatrix.Transform(PdfSubpath.GetBoundingRectangle(path)!.Value);
        }

        /// <summary>
        /// The bounds of Roboto's glyph for <paramref name="c"/> in text space (1 unit = 1 em).
        /// </summary>
        private static PdfRectangle GetRobotoBounds(char c)
        {
            var font = TrueTypeFontParser.Parse(new TrueTypeDataBytes(Roboto));
            int glyphIndex = font.WindowsUnicodeCMap.CharacterCodeToGlyphIndex(c);
            Assert.True(font.TableRegister.GlyphTable!.TryGetGlyphPath(glyphIndex, out var path));

            double scale = 1.0 / font.GetUnitsPerEm();
            var bounds = PdfSubpath.GetBoundingRectangle(path)!.Value;
            return new PdfRectangle(bounds.Left * scale, bounds.Bottom * scale, bounds.Right * scale, bounds.Top * scale);
        }

        private static IDisposable Register(byte[] program, Standard14Font font)
        {
            SystemFontFinder.AddSubstituteFont(font, program);
            return new Unregister(font);
        }

        private sealed class Unregister(Standard14Font font) : IDisposable
        {
            public void Dispose() => SystemFontFinder.RemoveSubstituteFont(font);
        }

        private static IReadOnlyList<Letter> GetLetters(byte[] pdf)
        {
            using var document = PdfDocument.Open(pdf);
            return document.GetPage(1).Letters.ToArray();
        }

        private static PdfRectangle GetMinionProBounds(string name)
        {
            var font = CompactFontFormatParser.Parse(new CompactFontFormatData(MinionPro)).FirstFont;
            return PdfSubpath.GetBoundingRectangle(font.GetCharacterPath(name))!.Value;
        }

        private static void AssertBounds(PdfRectangle expectedGlyphSpace, IReadOnlyList<PdfSubpath> actual, double scale)
        {
            var bounds = PdfSubpath.GetBoundingRectangle(actual)!.Value;
            Assert.Equal(expectedGlyphSpace.Left * scale, bounds.Left, 6);
            Assert.Equal(expectedGlyphSpace.Bottom * scale, bounds.Bottom, 6);
            Assert.Equal(expectedGlyphSpace.Right * scale, bounds.Right, 6);
            Assert.Equal(expectedGlyphSpace.Top * scale, bounds.Top, 6);
        }

        private static byte[] BuildPdf(string font, string text, string? descriptor = null)
        {
            string content = $"BT /F1 12 Tf 10 10 Td {text} Tj ET";

            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 5 0 R /Resources << /Font << /F1 4 0 R >> >> >>",
                font,
                $"<< /Length {content.Length} >>\nstream\n{content}\nendstream"
            };

            if (descriptor is not null)
            {
                objects.Add(descriptor);
            }

            var builder = new StringBuilder("%PDF-1.7\n");
            var offsets = new List<int>();
            for (int i = 0; i < objects.Count; i++)
            {
                offsets.Add(builder.Length);
                builder.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            int xref = builder.Length;
            builder.Append($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
            foreach (int offset in offsets)
            {
                builder.Append($"{offset:D10} 00000 n \n");
            }

            builder.Append($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");

            return Encoding.ASCII.GetBytes(builder.ToString());
        }
    }
}
