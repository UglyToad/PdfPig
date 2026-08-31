using BenchmarkDotNet.Attributes;

namespace UglyToad.PdfPig.Benchmarks;

/// <summary>
/// Measures the cost the ICC profile / rendering intent / output intent work adds when it is
/// <i>opted out of</i>, which is the default: <c>ParsingOptions.IccProfileService</c> is null, no profile is
/// ever parsed, and the observable behaviour is meant to be exactly what it was before.
/// </summary>
[Config(typeof(NuGetPackageConfig))]
[MemoryDiagnoser(displayGenColumns: false)]
public class IccColorManagementBenchmarks
{
    /// <summary>
    /// Six pages selecting colour through named <c>/ICCBased</c> content colour spaces, over ~21k letters.
    /// This is the colour <i>operator</i> path: every <c>cs</c>/<c>sc</c>/<c>scn</c> now converts under an
    /// intent and is stored as operands, and every read of the current colour checks whether the intent has
    /// moved since. Twenty ICCBased spaces are also resolved to their alternate on the way.
    /// </summary>
    [Benchmark]
    public int IccBasedContent_ParsePages()
    {
        int count = 0;
        using (var doc = PdfDocument.Open("GHOSTSCRIPT-700236-1.pdf"))
        {
            for (int p = 1; p <= doc.NumberOfPages; p++)
            {
                count += doc.GetPage(p).Letters.Count;
            }
        }

        return count;
    }

    /// <summary>
    /// 86 pages carrying ~190 <c>/ICCBased</c> images. Dominated by constructing
    /// <c>ICCBasedColorSpaceDetails</c> and falling back to the alternate colour space, without paying for a
    /// full pixel decode of every image.
    /// </summary>
    [Benchmark]
    public int IccBasedImages_ResolveColorSpaces()
    {
        int count = 0;
        using (var doc = PdfDocument.Open("Pig Production Handbook.pdf"))
        {
            for (int p = 1; p <= doc.NumberOfPages; p++)
            {
                foreach (var image in doc.GetPage(p).GetImages())
                {
                    if (image.ColorSpaceDetails is not null)
                    {
                        count += image.ColorSpaceDetails.NumberOfColorComponents;
                    }
                }
            }
        }

        return count;
    }

    /// <summary>
    /// ICCBased, CalRGB and Indexed images decoded all the way to PNG. This is the per-sample path, where
    /// the extra rendering intent argument rides through <c>ColorSpaceDetails.Transform</c> and the byte
    /// converter into the inner loop.
    /// </summary>
    [Benchmark]
    public int MixedColorSpaceImages_TryGetPng()
    {
        int totalBytes = 0;
        using (var doc = PdfDocument.Open("issue_671.pdf"))
        {
            for (int p = 1; p <= doc.NumberOfPages; p++)
            {
                foreach (var image in doc.GetPage(p).GetImages())
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

    /// <summary>
    /// A document declaring <c>/OutputIntents</c> with a real <c>/DestOutputProfile</c> stream. Small and
    /// quick on purpose: the output intent lookup runs once per page whatever else the page contains, so a
    /// cheap page is what exposes it. Opted out, the lookup must short-circuit before the profile stream is
    /// decoded.
    /// </summary>
    [Benchmark]
    public int OutputIntentDocument_ParsePages()
    {
        int count = 0;
        using (var doc = PdfDocument.Open("GHOSTSCRIPT-699375-5.pdf"))
        {
            for (int p = 1; p <= doc.NumberOfPages; p++)
            {
                count += doc.GetPage(p).Letters.Count;
            }
        }

        return count;
    }

    /// <summary>
    /// Opens a document without parsing a page, isolating the once-per-document cost: the resource store now
    /// takes the catalog dictionary and holds an ICC profile cache and a lazy output intent list. None of
    /// that should be paid for until something asks for it.
    /// </summary>
    [Benchmark]
    public int OpenDocument_NoPageParse()
    {
        using (var doc = PdfDocument.Open("GHOSTSCRIPT-700236-1.pdf"))
        {
            return doc.NumberOfPages;
        }
    }
}
