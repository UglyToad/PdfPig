using BenchmarkDotNet.Attributes;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.DocumentLayoutAnalysis;
using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;

namespace UglyToad.PdfPig.Benchmarks;

[Config(typeof(NuGetPackageConfig))]
[MemoryDiagnoser(displayGenColumns: false)]
public class LayoutAnalysisBenchmarks
{
    private readonly Letter[] _letters;
    private readonly Word[] _words;

    public LayoutAnalysisBenchmarks()
    {
        using (var doc = PdfDocument.Open("fseprd1102849.pdf"))
        {
            _letters = doc.GetPage(1).Letters.ToArray();
            _words = NearestNeighbourWordExtractor.Instance.GetWords(_letters).ToArray();
        }
    }

    [Benchmark]
    public IReadOnlyList<Word> GetWords_NearestNeighbourWord()
    {
        return NearestNeighbourWordExtractor.Instance.GetWords(_letters).ToArray();
    }

    [Benchmark]
    public IReadOnlyList<TextBlock> GetBlocks_Docstrum()
    {
        return DocstrumBoundingBoxes.Instance.GetBlocks(_words);
    }

    [Benchmark]
    public IReadOnlyList<Letter> DuplicateOverlappingText()
    {
        return DuplicateOverlappingTextProcessor.Get(_letters).ToArray();
    }
}