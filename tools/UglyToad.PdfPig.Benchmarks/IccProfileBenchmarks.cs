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
    public int IronOre_ImageColorSpaces()
    {
        // 37 images across the document, all with an /ICCBased colour space. This is the shape where the
        // colour space cache key carries whatever the colour space definition holds, so an image costs a
        // full hash of the profile and, on a cache hit, a full byte-for-byte comparison of it - unless the
        // definition reaches the cache in the form it was written in. See XObjectFactory.Resolve.
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

    private static double ProcessDoc(string filePath)
    {
        double count = 0;
        using (var doc = PdfDocument.Open(filePath))
        {
            var page = doc.GetPage(1);
            foreach (var pdfImage in page.GetImages())
            {
                count += pdfImage.RawBytes.Length;
            }
            
            foreach (var path in page.Paths)
            {
                var (r, g, b) = path.FillColor?.ToRGBValues() ?? (0, 0, 0);
                count += r + g + b;
                (r, g, b) = path.StrokeColor?.ToRGBValues() ?? (0, 0, 0);
                count += r + g + b;
            }
        }
        
        return count;
    }
}