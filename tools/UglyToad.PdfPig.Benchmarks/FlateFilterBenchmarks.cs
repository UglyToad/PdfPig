using System.IO;
using System.IO.Compression;
using BenchmarkDotNet.Attributes;
using UglyToad.PdfPig.Filters;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Benchmarks;

/// <summary>
/// Where does the time go in the Flate filter? The inflater itself is native zlib behind
/// DeflateStream, so the filter can only lose or win in what surrounds it: the stream over the
/// input, the size of each read, the buffer the output grows in, and the copy into the result.
/// The Flate streams of several real documents are pulled out once; the filter is measured
/// against plumbing variants written out here, and against the bare inflater as the floor.
/// </summary>
[Config(typeof(RuntimesConfig))]
[MemoryDiagnoser(displayGenColumns: false)]
public class FlateFilterBenchmarks
{
    private const int MaximumCapacity = 0x7FFFFFC7;

    private readonly FlateFilter filter = new();

    private (byte[] Data, DictionaryToken Dictionary)[] streams = [];
    private byte[] scratch = [];

    [GlobalSetup]
    public void Setup()
    {
        var found = new List<(byte[], DictionaryToken)>();

        foreach (var file in new[] { "Pig Production Handbook.pdf", "fseprd1102849.pdf", "MOZILLA-7375-0.pdf", "iron-ore-q2-q3-2013.pdf", "11194059_2017-11_de_s.pdf", "algo.pdf" })
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
                    || stream.StreamDictionary.TryGet(NameToken.DecodeParms, out _)
                    || stream.Data.Length < 3)
                {
                    continue;
                }

                found.Add((stream.Data.ToArray(), stream.StreamDictionary));
            }
        }

        streams = found.ToArray();
        scratch = new byte[1 << 20];

        Console.WriteLine($"// {streams.Length} plain Flate streams, {streams.Sum(s => (long)s.Data.Length) / 1024} KB compressed, {streams.Sum(s => (long)filter.Decode(s.Data, s.Dictionary, DefaultFilterProvider.Instance, 0).Length) / 1024} KB decoded");
    }

    /// <summary>The filter as it is.</summary>
    [Benchmark(Baseline = true)]
    public long Filter()
    {
        long total = 0;

        foreach (var (data, dictionary) in streams)
        {
            total += filter.Decode(data, dictionary, DefaultFilterProvider.Instance, 0).Length;
        }

        return total;
    }

    /// <summary>The bare inflater into a fixed scratch buffer that is never kept: the floor nothing can go below.</summary>
    [Benchmark]
    public long InflateOnly()
    {
        long total = 0;

        foreach (var (data, _) in streams)
        {
            using var source = new MemoryStream(data, 2, data.Length - 2, writable: false);
            using var deflate = new DeflateStream(source, CompressionMode.Decompress);

            int read;
            while ((read = deflate.Read(scratch, 0, scratch.Length)) > 0)
            {
                total += read;
            }
        }

        return total;
    }

    /// <summary>DeflateStream.CopyTo into a MemoryStream sized like the old filter, result taken without a copy.</summary>
    [Benchmark]
    public long CopyToMemoryStream()
    {
        long total = 0;

        foreach (var (data, _) in streams)
        {
            using var source = new MemoryStream(data, 2, data.Length - 2, writable: false);
            using var deflate = new DeflateStream(source, CompressionMode.Decompress);
            using var output = new MemoryStream((int)(data.Length * 1.5));

            deflate.CopyTo(output);
            total += output.Length;
        }

        return total;
    }

    /// <summary>The filter's plumbing with 8 KB reads, written out here so the read size can be varied.</summary>
    [Benchmark]
    public long Rented8K() => RentedBlocks(8 * 1024, 4);

    /// <summary>The same with 64 KB reads: an eighth of the calls into the inflater.</summary>
    [Benchmark]
    public long Rented64K() => RentedBlocks(64 * 1024, 4);

    /// <summary>The same reading as much as the buffer has room for.</summary>
    [Benchmark]
    public long RentedFill() => RentedBlocks(int.MaxValue, 4);

    /// <summary>64 KB reads with the buffer starting at twice the input rather than four times.</summary>
    [Benchmark]
    public long Rented64KFactor2() => RentedBlocks(64 * 1024, 2);

    /// <summary>
    /// Inflate into a 64 KB block that stays in the cache and copy each block into the growing rented
    /// buffer, the way CopyTo works: one copy more, but the inflater never writes to cold memory.
    /// </summary>
    [Benchmark]
    public long ChunkCopy64K() => ChunkCopy(64 * 1024);

    /// <summary>The same with a 16 KB block, small enough for the L1 cache.</summary>
    [Benchmark]
    public long ChunkCopy16K() => ChunkCopy(16 * 1024);

    private long ChunkCopy(int chunkLength)
    {
        long total = 0;
        var chunk = System.Buffers.ArrayPool<byte>.Shared.Rent(chunkLength);

        try
        {
            foreach (var (data, _) in streams)
            {
                using var source = new MemoryStream(data, 2, data.Length - 2, writable: false);
                using var deflate = new DeflateStream(source, CompressionMode.Decompress);

                var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(Math.Max(4096, data.Length * 4));
                var length = 0;

                try
                {
                    int read;
                    while ((read = deflate.Read(chunk, 0, chunkLength)) > 0)
                    {
                        if (buffer.Length - length < read)
                        {
                            var grown = System.Buffers.ArrayPool<byte>.Shared.Rent((int)Math.Min(MaximumCapacity, Math.Max(buffer.Length * 2L, length + read)));
                            buffer.AsSpan(0, length).CopyTo(grown);
                            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                            buffer = grown;
                        }

                        chunk.AsSpan(0, read).CopyTo(buffer.AsSpan(length));
                        length += read;
                    }

                    var result = GC.AllocateUninitializedArray<byte>(length);
                    buffer.AsSpan(0, length).CopyTo(result);
                    total += result.Length;
                }
                finally
                {
                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                }
            }
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(chunk);
        }

        return total;
    }

    /// <summary>ZLibStream over the whole data, header and checksum handled natively, into a rented buffer with 64 KB reads.</summary>
    [Benchmark]
    public long ZLibStream64K()
    {
        long total = 0;

        foreach (var (data, _) in streams)
        {
            using var source = new MemoryStream(data, 0, data.Length, writable: false);
            using var zlib = new ZLibStream(source, CompressionMode.Decompress);

            total += ReadAllInto(zlib, data.Length, 64 * 1024, 4);
        }

        return total;
    }

    private long RentedBlocks(int blockLength, int factor)
    {
        long total = 0;

        foreach (var (data, _) in streams)
        {
            using var source = new MemoryStream(data, 2, data.Length - 2, writable: false);
            using var deflate = new DeflateStream(source, CompressionMode.Decompress);

            total += ReadAllInto(deflate, data.Length, blockLength, factor);
        }

        return total;
    }

    /// <summary>Inflates into a rented buffer that doubles when full, then copies the exact length out, as the filter does.</summary>
    private static int ReadAllInto(Stream inflater, int inputLength, int blockLength, int factor)
    {
        var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(Math.Max(4096, inputLength * factor));
        var length = 0;

        try
        {
            while (true)
            {
                if (buffer.Length - length < Math.Min(blockLength, 4096))
                {
                    var grown = System.Buffers.ArrayPool<byte>.Shared.Rent((int)Math.Min(MaximumCapacity, buffer.Length * 2L));
                    buffer.AsSpan(0, length).CopyTo(grown);
                    System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
                    buffer = grown;
                }

                var read = inflater.Read(buffer, length, Math.Min(blockLength, buffer.Length - length));
                if (read == 0)
                {
                    break;
                }

                length += read;
            }

            var result = GC.AllocateUninitializedArray<byte>(length);
            buffer.AsSpan(0, length).CopyTo(result);
            return result.Length;
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
