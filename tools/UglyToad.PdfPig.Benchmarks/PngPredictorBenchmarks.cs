using System.IO;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using UglyToad.PdfPig.Filters;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Benchmarks;

/// <summary>
/// Measures undoing the PNG predictors on their own, without the inflater in front of them. The
/// images with a predictor in two real documents (all of them Sub filtered, 8 MB) are inflated
/// once in setup; the other filters, and other pixel widths, are measured on synthetic images
/// because the documents carry none. PngPredictor is internal and changed shape, so it is reached
/// through reflection in whichever form the build under test has.
/// </summary>
[Config(typeof(NuGetPackageConfig))]
[MemoryDiagnoser(displayGenColumns: false)]
public class PngPredictorBenchmarks
{
    private const int SyntheticColumns = 1000;
    private const int SyntheticRows = 1000;

    private readonly FlateFilter filter = new();

    private (byte[] Raw, byte[] Scratch, int Colors, int BitsPerComponent, int Columns)[] realSub = [];
    private (Memory<byte> Data, DictionaryToken Dictionary)[] realStreams = [];

    private byte[] sub1 = [], sub3 = [], sub4 = [], up3 = [], average3 = [], average4 = [], paeth3 = [], paeth4 = [], scratch = [];

    /// <summary>Decodes (buffer, length, predictor, colors, bitsPerComponent, columns) and yields the decoded length.</summary>
    private Func<byte[], int, int, int, int, int, int> undoPredictor = null!;

    [GlobalSetup]
    public void Setup()
    {
        undoPredictor = BindPredictor();

        var streams = new List<(Memory<byte>, DictionaryToken)>();
        var raw = new List<(byte[], byte[], int, int, int)>();

        foreach (var file in new[] { "Pig Production Handbook.pdf", "GHOSTSCRIPT-697234-0.pdf" })
        {
            using var document = PdfDocument.Open(file, new ParsingOptions { UseLenientParsing = true });

            foreach (var reference in document.Structure.CrossReferenceTable.ObjectOffsets.Keys)
            {
                ObjectToken obj;
                try
                {
                    obj = document.Structure.GetObject(reference);
                }
                catch
                {
                    continue;
                }

                if (obj.Data is not StreamToken stream
                    || !stream.StreamDictionary.TryGet(NameToken.Filter, out var token)
                    || token is not NameToken name
                    || name.Data != NameToken.FlateDecode.Data
                    || !stream.StreamDictionary.TryGet(NameToken.DecodeParms, out var parms)
                    || parms is not DictionaryToken parameters
                    || !parameters.TryGet(NameToken.Predictor, out var predictorToken)
                    || predictorToken is not NumericToken predictor
                    || predictor.Int < 10)
                {
                    continue;
                }

                streams.Add((stream.Data.ToArray(), stream.StreamDictionary));

                // Inflate without the predictor to get the rows with their filter type bytes.
                var plain = new DictionaryToken(stream.StreamDictionary.Data
                    .Where(kv => kv.Key != NameToken.DecodeParms.Data)
                    .ToDictionary(kv => NameToken.Create(kv.Key), kv => kv.Value));

                var inflated = filter.Decode(stream.Data, plain, DefaultFilterProvider.Instance, 0).ToArray();

                raw.Add((inflated, new byte[inflated.Length],
                    Get(parameters, NameToken.Colors, 1),
                    Get(parameters, NameToken.BitsPerComponent, 8),
                    Get(parameters, NameToken.Columns, 1)));
            }
        }

        realStreams = streams.ToArray();
        realSub = raw.ToArray();

        var grey = SyntheticImage(1);
        var rgb = SyntheticImage(3);
        var cmyk = SyntheticImage(4);

        sub1 = Filtered(grey, 1, 1);
        sub3 = Filtered(rgb, 3, 1);
        sub4 = Filtered(cmyk, 4, 1);
        up3 = Filtered(rgb, 3, 2);
        average3 = Filtered(rgb, 3, 3);
        average4 = Filtered(cmyk, 4, 3);
        paeth3 = Filtered(rgb, 3, 4);
        paeth4 = Filtered(cmyk, 4, 4);
        scratch = new byte[sub4.Length];
    }

    /// <summary>The 33 Sub filtered images of the two documents, 8 MB of rows, predictor only.</summary>
    [Benchmark]
    public long RealImagesSub()
    {
        long total = 0;

        foreach (var (raw, buffer, colors, bitsPerComponent, columns) in realSub)
        {
            Buffer.BlockCopy(raw, 0, buffer, 0, raw.Length);
            total += undoPredictor(buffer, raw.Length, 15, colors, bitsPerComponent, columns);
        }

        return total;
    }

    [Benchmark]
    public int SyntheticSub1() => Run(sub1, 1);

    [Benchmark]
    public int SyntheticSub3() => Run(sub3, 3);

    [Benchmark]
    public int SyntheticSub4() => Run(sub4, 4);

    [Benchmark]
    public int SyntheticUp3() => Run(up3, 3);

    [Benchmark]
    public int SyntheticAverage3() => Run(average3, 3);

    [Benchmark]
    public int SyntheticAverage4() => Run(average4, 4);

    [Benchmark]
    public int SyntheticPaeth3() => Run(paeth3, 3);

    [Benchmark]
    public int SyntheticPaeth4() => Run(paeth4, 4);

    /// <summary>The same 33 images through the Flate filter, inflater and predictor together.</summary>
    [Benchmark]
    public long FlateWithPredictor()
    {
        long total = 0;

        foreach (var (data, dictionary) in realStreams)
        {
            total += filter.Decode(data, dictionary, DefaultFilterProvider.Instance, 0).Length;
        }

        return total;
    }

    private int Run(byte[] filtered, int colors)
    {
        Buffer.BlockCopy(filtered, 0, scratch, 0, filtered.Length);
        return undoPredictor(scratch, filtered.Length, 15, colors, 8, SyntheticColumns);
    }

    private static int Get(DictionaryToken dictionary, NameToken key, int fallback)
        => dictionary.TryGet(key, out var token) && token is NumericToken number ? number.Int : fallback;

    /// <summary>A 1000 by 1000 image with the given channels: smooth gradients with noise, so no filter is trivial.</summary>
    private static byte[] SyntheticImage(int channels)
    {
        var random = new Random(7);
        var image = new byte[SyntheticRows * SyntheticColumns * channels];

        for (var y = 0; y < SyntheticRows; y++)
        {
            for (var x = 0; x < SyntheticColumns; x++)
            {
                var i = ((y * SyntheticColumns) + x) * channels;

                for (var channel = 0; channel < channels; channel++)
                {
                    var gradient = channel switch
                    {
                        0 => x / 4,
                        1 => y / 4,
                        2 => (x + y) / 8,
                        _ => (x * 3 + y) / 16
                    };

                    image[i + channel] = (byte)(gradient + random.Next(8));
                }
            }
        }

        return image;
    }

    /// <summary>Applies one PNG filter type to every row, the way an encoder would.</summary>
    private static byte[] Filtered(byte[] image, int bytesPerPixel, byte filterType)
    {
        var rowLength = SyntheticColumns * bytesPerPixel;
        var output = new byte[SyntheticRows * (rowLength + 1)];

        for (var y = 0; y < SyntheticRows; y++)
        {
            output[y * (rowLength + 1)] = filterType;

            for (var i = 0; i < rowLength; i++)
            {
                var x = image[(y * rowLength) + i];
                var a = i >= bytesPerPixel ? image[(y * rowLength) + i - bytesPerPixel] : 0;
                var b = y > 0 ? image[((y - 1) * rowLength) + i] : 0;
                var c = y > 0 && i >= bytesPerPixel ? image[((y - 1) * rowLength) + i - bytesPerPixel] : 0;

                int predicted = filterType switch
                {
                    1 => a,
                    2 => b,
                    3 => (a + b) >> 1,
                    4 => Paeth(a, b, c),
                    _ => 0
                };

                output[(y * (rowLength + 1)) + 1 + i] = (byte)(x - predicted);
            }
        }

        return output;
    }

    private static int Paeth(int a, int b, int c)
    {
        var p = a + b - c;
        var pa = Math.Abs(p - a);
        var pb = Math.Abs(p - b);
        var pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }

    /// <summary>
    /// Finds the predictor in the build under test: the in-place Decode of the rewrite, or the
    /// stream wrapper of the versions before it, and returns a call that decodes a buffer in place
    /// (or through a stream) and yields the decoded length.
    /// </summary>
    private static Func<byte[], int, int, int, int, int, int> BindPredictor()
    {
        var type = typeof(FlateFilter).Assembly.GetType("UglyToad.PdfPig.Filters.PngPredictor")
            ?? throw new InvalidOperationException("PngPredictor not found.");

        var decode = type.GetMethod("Decode", BindingFlags.Public | BindingFlags.Static,
            null, [typeof(Memory<byte>), typeof(int), typeof(int), typeof(int), typeof(int)], null);

        if (decode != null)
        {
            var call = (Func<Memory<byte>, int, int, int, int, Memory<byte>>)decode.CreateDelegate(typeof(Func<Memory<byte>, int, int, int, int, Memory<byte>>));
            return (data, length, predictor, colors, bitsPerComponent, columns) => call(new Memory<byte>(data, 0, length), predictor, colors, bitsPerComponent, columns).Length;
        }

        var wrap = type.GetMethod("WrapPredictor", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Neither PngPredictor.Decode nor WrapPredictor found.");

        return (data, length, predictor, colors, bitsPerComponent, columns) =>
        {
            using var output = new MemoryStream(length);
            using var predicted = (Stream)wrap.Invoke(null, [output, predictor, colors, bitsPerComponent, columns])!;
            predicted.Write(data, 0, length);
            predicted.Flush();
            return (int)output.Length;
        };
    }
}
