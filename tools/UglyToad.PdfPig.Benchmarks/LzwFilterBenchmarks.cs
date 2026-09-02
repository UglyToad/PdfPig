using BenchmarkDotNet.Attributes;
using UglyToad.PdfPig.Filters;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Benchmarks;

/// <summary>
/// Decodes the LZW compressed streams of a real document. The raw streams are pulled out of the
/// file once in setup so that only the filter is measured, not the parsing around it.
/// </summary>
[Config(typeof(NuGetPackageConfig))]
[MemoryDiagnoser(displayGenColumns: false)]
public class LzwFilterBenchmarks
{
    private readonly LzwFilter filter = new();

    private (Memory<byte> Data, DictionaryToken Dictionary)[] streams = [];
    private (Memory<byte> Data, DictionaryToken Dictionary) largest;

    [GlobalSetup]
    public void Setup()
    {
        var found = new List<(Memory<byte> Data, DictionaryToken Dictionary)>();

        using var document = PdfDocument.Open("ssm2163.pdf");

        foreach (var reference in document.Structure.CrossReferenceTable.ObjectOffsets.Keys)
        {
            if (document.Structure.GetObject(reference).Data is not StreamToken stream)
            {
                continue;
            }

            if (!stream.StreamDictionary.TryGet(NameToken.Filter, out var token)
                || token is not NameToken name
                || name.Data != NameToken.LzwDecode.Data)
            {
                continue;
            }

            found.Add((stream.Data.ToArray(), stream.StreamDictionary));
        }

        streams = found.OrderBy(x => x.Data.Length).ToArray();
        largest = streams[^1];
    }

    /// <summary>All sixteen streams of the document, about 230 KB compressed, 560 KB decoded.</summary>
    [Benchmark]
    public long AllStreams()
    {
        long total = 0;

        foreach (var (data, dictionary) in streams)
        {
            total += filter.Decode(data, dictionary, DefaultFilterProvider.Instance, 0).Length;
        }

        return total;
    }

    /// <summary>The largest stream alone, about 46 KB compressed, 150 KB decoded.</summary>
    [Benchmark]
    public int LargestStream()
    {
        return filter.Decode(largest.Data, largest.Dictionary, DefaultFilterProvider.Instance, 0).Length;
    }
}
