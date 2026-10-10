namespace UglyToad.PdfPig.Tests.Parser.FileStructure;

using PdfPig.Core;
using PdfPig.Filters;
using PdfPig.Parser.FileStructure;
using PdfPig.Tokens;

public class XrefStreamParserTests
{
    // Two 4 byte entries for /W [1 2 1]: object 0 is free, object 1 is at offset 0x0110.
    private const string Data = "\u0000\u0000\u0000\u0000\u0001\u0001\u0010\u0000";

    [Fact]
    public void ReadsStreamWithValidW()
    {
        var result = Read("1 2 1", Data);

        Assert.NotNull(result);
        Assert.Equal(0x0110, Assert.Single(result.ObjectOffsets).Value.Value1);
    }

    /// <summary>
    /// The spec sets no upper bound on a field, so a field wider than needed is padded with leading zeros.
    /// </summary>
    [Fact]
    public void ReadsStreamWithWideField()
    {
        var data = new string('\u0000', 12)
                   + "\u0001" + new string('\u0000', 8) + "\u0001\u0010" + "\u0000";

        var result = Read("1 10 1", data);

        Assert.NotNull(result);
        Assert.Equal(0x0110, Assert.Single(result.ObjectOffsets).Value.Value1);
    }

    /// <summary>
    /// A zero first element means no type field and every entry is Type 1. A zero third element means
    /// no generation field and the generation defaults to 0.
    /// </summary>
    [Fact]
    public void ReadsStreamWithZeroFirstAndThirdField()
    {
        var result = Read("0 2 0", "\u0000\u0002\u0001\u0010");

        Assert.NotNull(result);
        Assert.Equal(2, result.ObjectOffsets.Count);
        Assert.Equal(0x0110, result.ObjectOffsets[new IndirectReference(1, 0)].Value1);
    }

    /// <summary>
    /// An entry longer than the data holds no entries, and nothing the size of the entry is allocated.
    /// </summary>
    [Fact]
    public void ReadsNoEntriesWhenLineIsLongerThanData()
    {
        var result = Read("1 2147483646 0", Data);

        Assert.NotNull(result);
        Assert.Empty(result.ObjectOffsets);
    }

    /// <summary>
    /// The spec forbids a zero second element, but it is tolerated: each entry is read with no offset
    /// field, so the entries in <see cref="Data"/> are all free and no offsets are found.
    /// </summary>
    [Fact]
    public void ReadsStreamWithZeroSecondField()
    {
        var result = Read("1 0 1", Data);

        Assert.NotNull(result);
        Assert.Empty(result.ObjectOffsets);
    }

    /// <summary>
    /// A negative line length used to reach stackalloc byte[LineLength] and overflow the stack,
    /// which kills the process rather than throwing.
    /// </summary>
    [Theory]
    [InlineData("-10 1 1")]
    [InlineData("1 -2 3")]
    [InlineData("2147483647 1 0")]
    [InlineData("2147483647 2147483647 2")]
    [InlineData("0 0 0")]
    public void InvalidWReturnsNull(string w)
    {
        Assert.Null(Read(w, Data));
    }

    /// <summary>
    /// The PNG predictor rows of a cross reference stream are its entries, so a /Columns that cannot
    /// be used is taken from /W rather than failing the decode and losing every entry.
    /// </summary>
    [Theory]
    [InlineData("4444444444444444444444444444444")]
    [InlineData("0")]
    [InlineData("-4")]
    public void ReadsPredictedStreamWithUnusableColumns(string columns)
    {
        var result = Read("1 2 1", FlateWithPngNone(Data, 4), $"/Filter /FlateDecode /DecodeParms << /Columns {columns} /Predictor 12 >>");

        Assert.NotNull(result);
        Assert.Equal(0x0110, Assert.Single(result.ObjectOffsets).Value.Value1);
    }

    [Fact]
    public void ReadsPredictedStreamWithValidColumns()
    {
        var result = Read("1 2 1", FlateWithPngNone(Data, 4), "/Filter /FlateDecode /DecodeParms << /Columns 4 /Predictor 12 >>");

        Assert.NotNull(result);
        Assert.Equal(0x0110, Assert.Single(result.ObjectOffsets).Value.Value1);
    }

    /// <summary>
    /// With a filter array the decode parameters are an array too, one entry per filter.
    /// </summary>
    [Theory]
    [InlineData("4444444444444444444444444444444")]
    [InlineData("4")]
    public void ReadsPredictedStreamWithDecodeParmsArray(string columns)
    {
        var result = Read("1 2 1", FlateWithPngNone(Data, 4), $"/Filter [/FlateDecode] /DecodeParms [<< /Columns {columns} /Predictor 12 >>]");

        Assert.NotNull(result);
        Assert.Equal(0x0110, Assert.Single(result.ObjectOffsets).Value.Value1);
    }

    [Theory]
    [InlineData("4444444444444444444444444444444")]
    [InlineData("4")]
    public void ReadsPredictedStreamWithDecodeParmsArrayAfterAnotherFilter(string columns)
    {
        var hex = string.Concat(OtherEncodings.StringAsLatin1Bytes(FlateWithPngNone(Data, 4)).Select(b => b.ToString("X2"))) + ">";

        var result = Read("1 2 1", hex, $"/Filter [/ASCIIHexDecode /FlateDecode] /DecodeParms [null << /Columns {columns} /Predictor 12 >>]");

        Assert.NotNull(result);
        Assert.Equal(0x0110, Assert.Single(result.ObjectOffsets).Value.Value1);
    }

    /// <summary>
    /// Flate encodes <paramref name="data"/> as rows of <paramref name="columns"/> bytes, each led by
    /// the PNG filter type None.
    /// </summary>
    private static string FlateWithPngNone(string data, int columns)
    {
        var rows = new List<byte>();
        var bytes = OtherEncodings.StringAsLatin1Bytes(data);

        for (var i = 0; i < bytes.Length; i += columns)
        {
            rows.Add(0);
            rows.AddRange(bytes.Skip(i).Take(columns));
        }

        using var stream = new MemoryStream(rows.ToArray());

        var encoded = new FlateFilter().Encode(stream, new DictionaryToken(new Dictionary<NameToken, IToken>()));

        return OtherEncodings.BytesAsLatin1String(encoded.ToArray());
    }

    private static XrefStream? Read(string w, string data, string extraEntries = "")
    {
        var content =
            $"1 0 obj\n<< /Type /XRef /Size 2 /W [{w}] /Length {data.Length} {extraEntries} >>\nstream\n{data}\nendstream\nendobj\n";

        var input = StringBytesTestConverter.Scanner(content);

        return XrefStreamParser.TryReadStreamAtOffset(
            new FileHeaderOffset(0),
            0,
            input.bytes,
            input.scanner,
            DefaultFilterProvider.Instance,
            new TestingLog());
    }
}
