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

            // Code 66 is ZapfDingbats' /a9, which MinionPro does not have: no outline rather than the .notdef box.
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
        public void TrueTypeStandard14FontIsReadAsSubstitutedStandard14Font()
        {
            // Not ZapfDingbats: TrueTypeStandard14FallbackSimpleFont debug-asserts it never gets it.
            var pdf = BuildPdf($"<< /Type /Font /Subtype /TrueType /BaseFont /Symbol {DifferencesA} >>", "(A)");

            var withoutSubstitute = GetLetters(pdf).Single().GetFont()!;
            Assert.IsType<TrueTypeStandard14FallbackSimpleFont>(withoutSubstitute);

            using var registration = Register(MinionPro, Standard14Font.Symbol);
            var withSubstitute = GetLetters(pdf).Single().GetFont()!;

            Assert.IsType<Type1Standard14Font>(withSubstitute);
            Assert.True(withSubstitute.TryGetNormalisedPath(65, out _));
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
            // Code 65 is renamed, 66 keeps ZapfDingbats' built-in name (/a9), not StandardEncoding's /B.
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
