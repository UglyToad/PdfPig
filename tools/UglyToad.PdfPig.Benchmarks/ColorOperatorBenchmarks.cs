using BenchmarkDotNet.Attributes;

namespace UglyToad.PdfPig.Benchmarks;

[Config(typeof(NuGetPackageConfig))]
[MemoryDiagnoser(displayGenColumns: false)]
public class ColorOperatorBenchmarks
{
    [Benchmark]
    public int DeviceN_LetterColors()
    {
        double total = 0;
        int count = 0;
        using (var doc = PdfDocument.Open("DeviceN_CS_test.pdf"))
        {
            for (int p = 1; p <= doc.NumberOfPages; p++)
            {
                foreach (var letter in doc.GetPage(p).Letters)
                {
                    var (r, g, b) = letter.Color.ToRGBValues();
                    total += r + g + b;
                    count++;
                }
            }
        }

        return count + (int)total;
    }

    [Benchmark]
    public int LayeredBrochure_PathColors()
    {
        double total = 0;
        int count = 0;
        using (var doc = PdfDocument.Open("Layer pdf - 322_High_Holborn_building_Brochure.pdf"))
        {
            for (int p = 1; p <= doc.NumberOfPages; p++)
            {
                foreach (var path in doc.GetPage(p).Paths)
                {
                    var (r, g, b) = path.FillColor?.ToRGBValues() ?? (0, 0, 0);
                    total += r + g + b;
                    (r, g, b) = path.StrokeColor?.ToRGBValues() ?? (0, 0, 0);
                    total += r + g + b;
                    count++;
                }
            }
        }

        return count + (int)total;
    }

    [Benchmark]
    public int UncolouredTilingPattern_ParsePage()
    {
        using (var doc = PdfDocument.Open("2_uncolor_tiling.pdf"))
        {
            return doc.GetPage(1).Paths.Count;
        }
    }

    [Benchmark]
    public int DefaultColourSpaces_ParsePage()
    {
        double total = 0;
        using (var doc = PdfDocument.Open("DefaultColourSpaces.230802.pdf"))
        {
            var page = doc.GetPage(1);
            foreach (var letter in page.Letters)
            {
                var (r, g, b) = letter.Color.ToRGBValues();
                total += r + g + b;
            }

            return page.Letters.Count + (int)total;
        }
    }
}
