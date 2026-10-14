using System.Text;
using BenchmarkDotNet.Attributes;
using UglyToad.PdfPig.Tokens;
using UglyToad.PdfPig.Writer;

namespace UglyToad.PdfPig.Benchmarks;

/// <summary>Measures name-cache hits, dictionary adapters and byte-preserving writing.</summary>
[MemoryDiagnoser]
public class NameTokenBenchmarks
{
    [Params("Type", "Performance-Font-Resource-Name", "宋体-é")]
    public string Text { get; set; } = "Type";

    private byte[] bytes = [];
    private NameToken name = null!;
    private DictionaryToken typed = null!;
    private DictionaryToken legacy = null!;
    private readonly TokenWriter writer = new();
    private readonly MemoryStream output = new(1024);

    [GlobalSetup]
    public void Setup()
    {
        bytes = Encoding.UTF8.GetBytes(Text);
        name = NameToken.Create(bytes.AsSpan());
        typed = new DictionaryToken(new Dictionary<NameToken, IToken> { [name] = NumericToken.One });
        legacy = DictionaryToken.With(new Dictionary<string, IToken> { [Encoding.Latin1.GetString(bytes)] = NumericToken.One });
        _ = name.Data;
        legacy.TryGet(name, out _);
    }

    [Benchmark] public NameToken ByteCacheHit() => NameToken.Create(bytes.AsSpan());
    [Benchmark] public NameToken TextCacheHit() => NameToken.Create(Text);
    [Benchmark] public bool TypedLookup() => typed.TryGet(name, out _);
    [Benchmark] public bool LegacyLookup() => legacy.TryGet(name, out _);
    [Benchmark] public DictionaryToken EditDictionary() => typed.With(name, NumericToken.One);

    [Benchmark]
    public NameToken EnumerateLegacyDictionary()
    {
        foreach (var entry in legacy.Entries) return entry.Key;
        throw new InvalidOperationException("The benchmark dictionary must contain an entry.");
    }

    [Benchmark] public string CachedText() => name.Data;

    [Benchmark]
    public long WriteName()
    {
        output.Position = 0;
        writer.WriteToken(name, output);
        return output.Position;
    }

    [GlobalCleanup] public void Cleanup() => output.Dispose();
}
