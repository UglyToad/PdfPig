using BenchmarkDotNet.Attributes;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Fonts.SystemFonts;
using UglyToad.PdfPig.Fonts.TrueType;

namespace UglyToad.PdfPig.Benchmarks;

[Config(typeof(NuGetPackageConfig))]
[MemoryDiagnoser(displayGenColumns: false)]
public class SystemFontFinderBenchmarks
{
    [Benchmark]
    public IReadOnlyList<Letter> ARVE_2745540212_Open()
    {
        List<Letter> letters = new List<Letter>();
        using (var doc = PdfDocument.Open("iizieileamidagi.ARVE_2745540212.pdf"))
        {
            foreach (var page in doc.GetPages())
            {
                letters.AddRange(page.Letters);
            }
        }

        return letters;
    }

    [Benchmark]
    public IReadOnlyList<TrueTypeFont?> ARVE_2745540212_GetTrueTypeFont()
    {
        List<TrueTypeFont?> letters = new List<TrueTypeFont?>();
        using (var doc = PdfDocument.Open("iizieileamidagi.ARVE_2745540212.pdf"))
        {
            foreach (var page in doc.GetPages())
            {
                foreach (var letter in page.Letters)
                {
                    letters.Add(SystemFontFinder.Instance.GetTrueTypeFont(letter.FontName));
                }
            }
        }

        return letters;
    }
}