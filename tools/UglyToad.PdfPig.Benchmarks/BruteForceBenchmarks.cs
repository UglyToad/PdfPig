using BenchmarkDotNet.Attributes;
using UglyToad.PdfPig.Content;

namespace UglyToad.PdfPig.Benchmarks;

[Config(typeof(NuGetPackageConfig))]
[MemoryDiagnoser(displayGenColumns: false)]
public class BruteForceBenchmarks
{

    [Benchmark]
    public IReadOnlyList<Letter> OpenOffice()
    {
        List<Letter> letters = new List<Letter>();
        using (var doc = PdfDocument.Open("Single Page Simple - from open office.pdf"))
        {
            foreach (var page in doc.GetPages())
            {
                letters.AddRange(page.Letters);
            }
        }

        return letters;
    }

    [Benchmark]
    public IReadOnlyList<Letter> Inkscape()
    {
        List<Letter> letters = new List<Letter>();
        using (var doc = PdfDocument.Open("Single Page Simple - from inkscape.pdf"))
        {
            foreach (var page in doc.GetPages())
            {
                letters.AddRange(page.Letters);
            }
        }

        return letters;
    }

    [Benchmark]
    public IReadOnlyList<Letter> Algo()
    {
        List<Letter> letters = new List<Letter>();
        using (var doc = PdfDocument.Open("algo.pdf"))
        {
            foreach (var page in doc.GetPages())
            {
                letters.AddRange(page.Letters);
            }
        }

        return letters;
    }


    [Benchmark]
    public IReadOnlyList<Letter> PDFBOX_492_4_jar_8()
    {
        List<Letter> letters = new List<Letter>();
        using (var doc = PdfDocument.Open("PDFBOX-492-4.jar-8.pdf"))
        {
            foreach (var page in doc.GetPages())
            {
                letters.AddRange(page.Letters);
            }
        }

        return letters;
    }
}
