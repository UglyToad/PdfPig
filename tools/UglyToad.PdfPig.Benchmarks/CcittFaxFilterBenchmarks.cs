using BenchmarkDotNet.Attributes;
using UglyToad.PdfPig.Filters;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Benchmarks;

[MemoryDiagnoser]
public class CcittFaxFilterBenchmarks
{
    private readonly CcittFaxDecodeFilter filter = new();
    private byte[] input = [];
    private DictionaryToken dictionary = null!;

    [Params("Small", "Fixture", "Large")]
    public string Image { get; set; } = "Fixture";

    [Params(true, false)]
    public bool BlackIsOne { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var columns = Image == "Small" ? 64 : Image == "Large" ? 8192 : 1800;
        var rows = Image == "Small" ? 64 : Image == "Large" ? 8192 : 3113;
        byte[] expected;
        if (Image == "Fixture")
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "src", "UglyToad.PdfPig.Tests", "Images", "Files", "ccittfax-encoded.bin")))
                directory = directory.Parent;
            if (directory == null) throw new FileNotFoundException("Run from a checkout containing the CCITT test fixture.");
            var path = Path.Combine(directory.FullName, "src", "UglyToad.PdfPig.Tests", "Images", "Files");
            input = File.ReadAllBytes(Path.Combine(path, "ccittfax-encoded.bin"));
            expected = File.ReadAllBytes(Path.Combine(path, "ccittfax-decoded.bin"));
        }
        else
        {
            // One vertical-zero bit per all-white Group 4 row.
            input = Enumerable.Repeat((byte)255, (rows + 7) / 8).ToArray();
            expected = new byte[(columns + 7) / 8 * rows];
        }
        dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
        {
            [NameToken.Filter] = NameToken.CcittfaxDecode,
            [NameToken.DecodeParms] = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                [NameToken.Columns] = new NumericToken(columns),
                [NameToken.Rows] = new NumericToken(rows),
                [NameToken.K] = new NumericToken(-1),
                [NameToken.BlackIs1] = BlackIsOne ? BooleanToken.True : BooleanToken.False
            })
        });
        if (!BlackIsOne) for (var i = 0; i < expected.Length; i++) expected[i] = (byte)~expected[i];
        if (!Decode().Span.SequenceEqual(expected)) throw new InvalidOperationException("CCITT output does not match the expected pixels.");
    }

    [Benchmark]
    public Memory<byte> Decode() => filter.Decode(input, dictionary, DefaultFilterProvider.Instance, 0);
}