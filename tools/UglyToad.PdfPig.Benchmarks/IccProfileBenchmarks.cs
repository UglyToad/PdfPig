using BenchmarkDotNet.Attributes;

namespace UglyToad.PdfPig.Benchmarks;

[Config(typeof(NuGetPackageConfig))]
[MemoryDiagnoser(displayGenColumns: false)]
public class IccProfileBenchmarks
{
    [Benchmark]
    public double GWG130_ICC_Source_Profile_x4()
    {
        return ProcessDoc("GWG130_ICC_Source_Profile_x4.pdf");
    }

    [Benchmark]
    public double GWG206_ICC_V4_RGB_Image_x4()
    {
        return ProcessDoc("GWG206_ICC_V4-RGB-Image_x4.pdf");
    }

    [Benchmark]
    public double GWG230_Four_different_Grays_x1a()
    {
        return ProcessDoc("GWG230_Four_different Grays_x1a.pdf");
    }

    [Benchmark]
    public double GWG172_JPEG2000_ICCBasedRGB_x4()
    {
        return ProcessDoc("GWG172_JPEG2000_compression_ICCBasedRGB_x4.pdf");
    }

    [Benchmark]
    public double GWG221_OutputIntentChangeIndicator_x4()
    {
        return ProcessDoc("GWG221_OutputIntentChangeIndicator_x4.pdf");
    }

    [Benchmark]
    public int IronOre_ImageColorSpaces()
    {
        int count = 0;
        using (var doc = PdfDocument.Open("iron-ore-q2-q3-2013.pdf"))
        {
            for (int p = 1; p <= doc.NumberOfPages; p++)
            {
                foreach (var image in doc.GetPage(p).GetImages())
                {
                    count += image.ColorSpaceDetails?.NumberOfColorComponents ?? 0;
                }
            }
        }

        return count;
    }

    [Benchmark]
    public int IronOre_Images_TryGetPng()
    {
        int totalBytes = 0;
        using (var doc = PdfDocument.Open("iron-ore-q2-q3-2013.pdf"))
        {
            for (int p = 1; p <= doc.NumberOfPages; p++)
            {
                foreach (var image in doc.GetPage(p).GetImages())
                {
                    if (image.TryGetPng(out byte[]? png))
                    {
                        totalBytes += png.Length;
                    }
                }
            }
        }

        return totalBytes;
    }

    private static double ProcessDoc(string filePath)
    {
        double count = 0;
        using (var doc = PdfDocument.Open(filePath))
        {
            for (int p = 1; p <= doc.NumberOfPages; p++)
            {
                var page = doc.GetPage(p);

                foreach (var pdfImage in page.GetImages())
                {
                    count += pdfImage.RawBytes.Length;
                    count += pdfImage.ColorSpaceDetails?.NumberOfColorComponents ?? 0;
                    count += pdfImage.Decode.Count;
                }

                foreach (var path in page.Paths)
                {
                    var (r, g, b) = path.FillColor?.ToRGBValues() ?? (0, 0, 0);
                    count += r + g + b;
                    (r, g, b) = path.StrokeColor?.ToRGBValues() ?? (0, 0, 0);
                    count += r + g + b;
                }
            }
        }

        return count;
    }
}
