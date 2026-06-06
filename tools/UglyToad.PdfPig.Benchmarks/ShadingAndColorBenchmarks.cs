using BenchmarkDotNet.Attributes;

namespace UglyToad.PdfPig.Benchmarks;

[Config(typeof(NuGetPackageConfig))]
[MemoryDiagnoser(displayGenColumns: false)]
public class ShadingAndColorBenchmarks
{
    [Benchmark]
    public int IndexedCalRgbImage_TryGetPng()
    {
        int totalBytes = 0;
        using (var doc = PdfDocument.Open("MOZILLA-10084-0.pdf"))
        {
            for (int p = 1; p <= doc.NumberOfPages; p++)
            {
                var page = doc.GetPage(p);
                foreach (var image in page.GetImages())
                {
                    if (image.TryGetPng(out byte[] png))
                    {
                        totalBytes += png.Length;
                    }
                }
            }
        }

        return totalBytes;
    }

    [Benchmark]
    public int SeparationImages_TryGetPng()
    {
        int totalBytes = 0;
        using (var doc = PdfDocument.Open("MOZILLA-7375-0.pdf"))
        {
            // Only the first page carries the separation images that matter; restrict iteration so the
            // benchmark spends its time in the per-pixel loop, not in unrelated page parsing.
            var page = doc.GetPage(1);
            foreach (var image in page.GetImages())
            {
                if (image.TryGetPng(out byte[] png))
                {
                    totalBytes += png.Length;
                }
            }
        }

        return totalBytes;
    }

    [Benchmark]
    public int Type4HeavyDocument_ParsePages()
    {
        int count = 0;
        using (var doc = PdfDocument.Open("11194059_2017-11_de_s.pdf"))
        {
            for (int p = 1; p <= doc.NumberOfPages; p++)
            {
                var page = doc.GetPage(p);
                count += page.Letters.Count;
            }
        }

        return count;
    }

    [Benchmark]
    public int ShadingHeavyDocument_ParsePages()
    {
        int count = 0;
        using (var doc = PdfDocument.Open("iron-ore-q2-q3-2013.pdf"))
        {
            for (int p = 1; p <= doc.NumberOfPages; p++)
            {
                var page = doc.GetPage(p);
                count += page.Letters.Count;
            }
        }

        return count;
    }

    /// <summary>
    /// Minimal repro for a <c>FunctionBasedShading</c>: small document, fast to parse, useful as a
    /// noise floor and to verify the shading parser path still works end-to-end.
    /// </summary>
    [Benchmark]
    public int FunctionBasedShading_ParsePage()
    {
        using (var doc = PdfDocument.Open("PDFBOX-1869-4-1.pdf"))
        {
            var page = doc.GetPage(1);
            return page.Letters.Count;
        }
    }
}
