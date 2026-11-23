using BenchmarkDotNet.Attributes;
using UglyToad.PdfPig.Content;

namespace UglyToad.PdfPig.Benchmarks;

/// <summary>
/// Turns every image of a document into a PNG. The images are decoded once in the setup, so what
/// is measured is <c>TryGetPng</c> itself: the conversion to RGB and the PNG encoding.
/// algo.pdf is 494 gray images, GHOSTSCRIPT-697234-0.pdf CMYK, fseprd1102849.pdf RGB and indexed,
/// and Pig Production Handbook.pdf ICC-based images, some with a soft mask.
/// </summary>
[Config(typeof(RuntimesConfig))]
[MemoryDiagnoser(displayGenColumns: false)]
public class PngBenchmarks
{
    private PdfDocument? document;
    private IPdfImage[] images = [];

    [Params("algo.pdf", "GHOSTSCRIPT-697234-0.pdf", "fseprd1102849.pdf", "Pig Production Handbook.pdf")]
    public string File { get; set; } = "";

    [GlobalSetup]
    public void Setup()
    {
        var opened = PdfDocument.Open(File, new ParsingOptions { UseLenientParsing = true, SkipMissingFonts = true });
        document = opened;
        images = Enumerable.Range(1, opened.NumberOfPages)
            .SelectMany(p => opened.GetPage(p).GetImages())
            .ToArray();

        foreach (var image in images)
        {
            image.TryGetBytesAsMemory(out _);
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        document?.Dispose();
    }

    [Benchmark]
    public long TryGetPng()
    {
        long length = 0;
        foreach (var image in images)
        {
            if (image.TryGetPng(out var png))
            {
                length += png.Length;
            }
        }

        return length;
    }
}
