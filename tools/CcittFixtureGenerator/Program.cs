using System.Text.Json;
using BitMiracle.LibTiff.Classic;

var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
var tiffPath = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.Combine(repoRoot, "src", "UglyToad.PdfPig.Tests", "Images", "Files", "Tif", "TiffCcittG4.tif");

var base64OutputPath = args.Length > 1
    ? Path.GetFullPath(args[1])
    : Path.Combine(repoRoot, "src", "UglyToad.PdfPig.Tests", "Images", "Files", "Tif", "TiffCcittG4.ccitt.base64");

var metadataOutputPath = args.Length > 2
    ? Path.GetFullPath(args[2])
    : Path.Combine(repoRoot, "src", "UglyToad.PdfPig.Tests", "Images", "Files", "Tif", "TiffCcittG4.fixture.json");

var fixture = CcittFixtureExtractor.Extract(tiffPath);

Directory.CreateDirectory(Path.GetDirectoryName(base64OutputPath)!);
File.WriteAllText(base64OutputPath, Convert.ToBase64String(fixture.Payload));

var metadata = new CcittFixtureMetadata(
    fixture.Width,
    fixture.Height,
    fixture.BlackIs1,
    fixture.Photometric,
    fixture.FillOrder,
    fixture.Payload.Length,
    Path.GetFileName(tiffPath));

var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
File.WriteAllText(metadataOutputPath, json);

Console.WriteLine(base64OutputPath);
Console.WriteLine(metadataOutputPath);
Console.WriteLine($"Width={fixture.Width} Height={fixture.Height} BlackIs1={fixture.BlackIs1} Photometric={fixture.Photometric} FillOrder={fixture.FillOrder} PayloadLength={fixture.Payload.Length}");

internal sealed record CcittFixture(int Width, int Height, bool BlackIs1, int Photometric, int FillOrder, byte[] Payload);

internal sealed record CcittFixtureMetadata(
    int Width,
    int Height,
    bool BlackIs1,
    int Photometric,
    int FillOrder,
    int PayloadLength,
    string SourceFile);

internal static class CcittFixtureExtractor
{
    private static readonly byte[] ReverseByteLookup = BuildReverseByteLookup();

    public static CcittFixture Extract(string tiffPath)
    {
        using var tif = Tiff.Open(tiffPath, "r");
        if (tif == null)
        {
            throw new InvalidOperationException($"Cannot open TIFF: {tiffPath}");
        }

        tif.SetDirectory(0);

        int compression = GetIntTagOrDefault(tif, TiffTag.COMPRESSION, 0);
        if (compression != (int)Compression.CCITTFAX4)
        {
            throw new InvalidOperationException($"Expected CCITT Group 4 TIFF, got Compression={compression}.");
        }

        int width = GetIntTag(tif, TiffTag.IMAGEWIDTH);
        int height = GetIntTag(tif, TiffTag.IMAGELENGTH);

        int spp = GetIntTagOrDefault(tif, TiffTag.SAMPLESPERPIXEL, 1);
        int bps = GetIntTagOrDefault(tif, TiffTag.BITSPERSAMPLE, 1);
        if (!(spp == 1 && bps == 1))
        {
            throw new InvalidOperationException($"Expected bilevel TIFF, got spp={spp}, bps={bps}.");
        }

        if (GetSegmentCountForCurrentDirectory(tif) > 1)
        {
            throw new InvalidOperationException("TIFF is not single-strip/tile; cannot pass-through safely.");
        }

        int photometric = GetIntTagOrDefault(tif, TiffTag.PHOTOMETRIC, (int)Photometric.MINISBLACK);
        bool blackIs1 = photometric == (int)Photometric.MINISBLACK;

        var payload = ReadCcittPayloadForCurrentDirectory(tif, tiffPath);

        int fillOrder = GetIntTagOrDefault(tif, TiffTag.FILLORDER, 1);
        if (fillOrder == 2)
        {
            payload = ReverseBitsPerByte(payload);
        }

        return new CcittFixture(width, height, blackIs1, photometric, fillOrder, payload);
    }

    private static int GetSegmentCountForCurrentDirectory(Tiff tif)
    {
        var stripOffsets = TryGetLongArrayTag(tif, TiffTag.STRIPOFFSETS);
        var stripByteCounts = TryGetLongArrayTag(tif, TiffTag.STRIPBYTECOUNTS);
        if (stripOffsets != null && stripByteCounts != null &&
            stripOffsets.Length == stripByteCounts.Length && stripOffsets.Length > 0)
        {
            return stripOffsets.Length;
        }

        var tileOffsets = TryGetLongArrayTag(tif, TiffTag.TILEOFFSETS);
        var tileByteCounts = TryGetLongArrayTag(tif, TiffTag.TILEBYTECOUNTS);
        if (tileOffsets != null && tileByteCounts != null &&
            tileOffsets.Length == tileByteCounts.Length && tileOffsets.Length > 0)
        {
            return tileOffsets.Length;
        }

        return 1;
    }

    private static int GetIntTag(Tiff tif, TiffTag tag)
    {
        var fieldValue = tif.GetField(tag);
        if (fieldValue == null || fieldValue.Length == 0)
        {
            throw new InvalidOperationException($"Missing TIFF tag {tag}");
        }

        return Convert.ToInt32(fieldValue[0].Value);
    }

    private static int GetIntTagOrDefault(Tiff tif, TiffTag tag, int defaultValue)
    {
        var fieldValue = tif.GetField(tag);
        if (fieldValue == null || fieldValue.Length == 0)
        {
            return defaultValue;
        }

        try
        {
            return Convert.ToInt32(fieldValue[0].Value);
        }
        catch
        {
            return defaultValue;
        }
    }

    private static long[]? TryGetLongArrayTag(Tiff tif, TiffTag tag)
    {
        var fieldValue = tif.GetField(tag);
        if (fieldValue == null || fieldValue.Length == 0)
        {
            return null;
        }

        object value = fieldValue[0].Value;

        if (value is long[] longArray)
        {
            return longArray;
        }

        if (value is int[] intArray)
        {
            var result = new long[intArray.Length];
            for (int i = 0; i < intArray.Length; i++)
            {
                result[i] = intArray[i];
            }

            return result;
        }

        if (fieldValue.Length > 1 && fieldValue[1].Value is long[] longArray2)
        {
            return longArray2;
        }

        if (fieldValue.Length > 1 && fieldValue[1].Value is int[] intArray2)
        {
            var result = new long[intArray2.Length];
            for (int i = 0; i < intArray2.Length; i++)
            {
                result[i] = intArray2[i];
            }

            return result;
        }

        return null;
    }

    private static byte[] ReadCcittPayloadForCurrentDirectory(Tiff tif, string tiffPath)
    {
        var stripOffsets = TryGetLongArrayTag(tif, TiffTag.STRIPOFFSETS);
        var stripByteCounts = TryGetLongArrayTag(tif, TiffTag.STRIPBYTECOUNTS);
        if (stripOffsets != null && stripByteCounts != null && stripOffsets.Length == 1 && stripByteCounts.Length == 1)
        {
            return ReadBytesAt(tiffPath, stripOffsets[0], stripByteCounts[0]);
        }

        var tileOffsets = TryGetLongArrayTag(tif, TiffTag.TILEOFFSETS);
        var tileByteCounts = TryGetLongArrayTag(tif, TiffTag.TILEBYTECOUNTS);
        if (tileOffsets != null && tileByteCounts != null && tileOffsets.Length == 1 && tileByteCounts.Length == 1)
        {
            return ReadBytesAt(tiffPath, tileOffsets[0], tileByteCounts[0]);
        }

        throw new InvalidOperationException("TIFF is not single-strip/tile; cannot pass-through safely.");
    }

    private static byte[] ReadBytesAt(string filePath, long offset, long length)
    {
        if (offset < 0 || length <= 0)
        {
            return Array.Empty<byte>();
        }

        if (length > int.MaxValue)
        {
            throw new InvalidOperationException("CCITT payload too large.");
        }

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        fs.Seek(offset, SeekOrigin.Begin);

        var buffer = new byte[(int)length];
        int read = 0;
        while (read < buffer.Length)
        {
            int current = fs.Read(buffer, read, buffer.Length - read);
            if (current <= 0)
            {
                throw new EndOfStreamException("Unexpected EOF while reading CCITT payload.");
            }

            read += current;
        }

        return buffer;
    }

    private static byte[] ReverseBitsPerByte(byte[] payload)
    {
        var result = new byte[payload.Length];
        for (int i = 0; i < payload.Length; i++)
        {
            result[i] = ReverseByteLookup[payload[i]];
        }

        return result;
    }

    private static byte[] BuildReverseByteLookup()
    {
        var lookup = new byte[256];
        for (int i = 0; i < lookup.Length; i++)
        {
            int value = i;
            int reversed = 0;
            for (int bit = 0; bit < 8; bit++)
            {
                reversed = (reversed << 1) | (value & 1);
                value >>= 1;
            }

            lookup[i] = (byte)reversed;
        }

        return lookup;
    }
}
