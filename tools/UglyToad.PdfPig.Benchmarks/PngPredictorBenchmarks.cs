using System.IO;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using UglyToad.PdfPig.Filters;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Benchmarks;

/// <summary>
/// Measures undoing the PNG predictors on their own, without the inflater in front of them. The
/// images with a predictor in two real documents (all of them Sub filtered, 8 MB) are inflated
/// once in setup; Up, Average and Paeth are measured on a synthetic image because the documents
/// carry none. PngPredictor is internal and changed shape, so it is reached through reflection in
/// whichever form the build under test has.
/// </summary>
[Config(typeof(NuGetPackageConfig))]
[MemoryDiagnoser(displayGenColumns: false)]
public class PngPredictorBenchmarks
{
    private const int SyntheticColumns = 1000;
    private const int SyntheticRows = 1000;
    private const int SyntheticColors = 3;

    private readonly FlateFilter filter = new();

    private (byte[] Raw, byte[] Scratch, int Colors, int BitsPerComponent, int Columns)[] realSub = [];
    private (Memory<byte> Data, DictionaryToken Dictionary)[] realStreams = [];

    private byte[] syntheticSub = [], syntheticUp = [], syntheticAverage = [], syntheticPaeth = [], syntheticScratch = [];

    private Func<byte[], int, int, int, int, int> undoPredictor = null!;

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

        var image = SyntheticImage();
        syntheticSub = Filtered(image, 1);
        syntheticUp = Filtered(image, 2);
        syntheticAverage = Filtered(image, 3);
        syntheticPaeth = Filtered(image, 4);
        syntheticScratch = new byte[syntheticSub.Length];
    }

    /// <summary>The 33 Sub filtered images of the two documents, 8 MB of rows, predictor only.</summary>
    [Benchmark]
    public long RealImagesSub()
    {
        long total = 0;

        foreach (var (raw, scratch, colors, bitsPerComponent, columns) in realSub)
        {
            Buffer.BlockCopy(raw, 0, scratch, 0, raw.Length);
            total += undoPredictor(scratch, 15, colors, bitsPerComponent, columns);
        }

        return total;
    }

    [Benchmark]
    public int SyntheticSub() => Run(syntheticSub);

    [Benchmark]
    public int SyntheticUp() => Run(syntheticUp);

    [Benchmark]
    public int SyntheticAverage() => Run(syntheticAverage);

    [Benchmark]
    public int SyntheticPaeth() => Run(syntheticPaeth);

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

    private int Run(byte[] filtered)
    {
        Buffer.BlockCopy(filtered, 0, syntheticScratch, 0, filtered.Length);
        return undoPredictor(syntheticScratch, 15, SyntheticColors, 8, SyntheticColumns);
    }

    private static int Get(DictionaryToken dictionary, NameToken key, int fallback)
        => dictionary.TryGet(key, out var token) && token is NumericToken number ? number.Int : fallback;

    /// <summary>A 1000 by 1000 RGB image: smooth gradients with noise, so no filter is trivial.</summary>
    private static byte[] SyntheticImage()
    {
        var random = new Random(7);
        var image = new byte[SyntheticRows * SyntheticColumns * SyntheticColors];

        for (var y = 0; y < SyntheticRows; y++)
        {
            for (var x = 0; x < SyntheticColumns; x++)
            {
                var i = ((y * SyntheticColumns) + x) * SyntheticColors;
                image[i] = (byte)(x / 4 + random.Next(8));
                image[i + 1] = (byte)(y / 4 + random.Next(8));
                image[i + 2] = (byte)((x + y) / 8 + random.Next(8));
            }
        }

        return image;
    }

    /// <summary>Applies one PNG filter type to every row, the way an encoder would.</summary>
    private static byte[] Filtered(byte[] image, byte filterType)
    {
        const int bytesPerPixel = SyntheticColors;
        const int rowLength = SyntheticColumns * SyntheticColors;

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
    private static Func<byte[], int, int, int, int, int> BindPredictor()
    {
        var type = typeof(FlateFilter).Assembly.GetType("UglyToad.PdfPig.Filters.PngPredictor")
            ?? throw new InvalidOperationException("PngPredictor not found.");

        var decode = type.GetMethod("Decode", BindingFlags.Public | BindingFlags.Static,
            null, [typeof(Memory<byte>), typeof(int), typeof(int), typeof(int), typeof(int)], null);

        if (decode != null)
        {
            var call = (Func<Memory<byte>, int, int, int, int, Memory<byte>>)decode.CreateDelegate(typeof(Func<Memory<byte>, int, int, int, int, Memory<byte>>));
            return (data, predictor, colors, bitsPerComponent, columns) => call(data, predictor, colors, bitsPerComponent, columns).Length;
        }

        var wrap = type.GetMethod("WrapPredictor", BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException("Neither PngPredictor.Decode nor WrapPredictor found.");

        return (data, predictor, colors, bitsPerComponent, columns) =>
        {
            using var output = new MemoryStream(data.Length);
            using var predicted = (Stream)wrap.Invoke(null, [output, predictor, colors, bitsPerComponent, columns])!;
            predicted.Write(data, 0, data.Length);
            predicted.Flush();
            return (int)output.Length;
        };
    }
}
