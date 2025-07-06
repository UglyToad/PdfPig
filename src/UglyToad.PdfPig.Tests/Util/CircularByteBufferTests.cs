namespace UglyToad.PdfPig.Tests.Util;

using PdfPig.Util;

public class CircularByteBufferTests
{
    [Fact]
    public void CanExceedCapacity()
    {
        var buffer = new CircularByteBuffer(3);

        var input = "123456"u8;
        for (var i = 0; i < input.Length; i++)
        {
            buffer.Add(input[i]);
        }

        Assert.True(buffer.IsCurrentlyEqual("456"));

        Assert.True("456"u8.SequenceEqual(buffer.AsSpan()));

        Assert.True(buffer.EndsWith("6"));
        Assert.True(buffer.EndsWith("56"));
        Assert.True(buffer.EndsWith("456"));
        Assert.False(buffer.EndsWith("3456"));
    }

    [Fact]
    public void CanUndershootCapacity()
    {
        var buffer = new CircularByteBuffer(9);

        var input = "123456"u8;
        for (var i = 0; i < input.Length; i++)
        {
            buffer.Add(input[i]);
        }

        Assert.True(buffer.IsCurrentlyEqual("123456"));

        Assert.True(buffer.EndsWith("3456"));
        Assert.False(buffer.EndsWith("123"));

        Assert.True("123456"u8.SequenceEqual(buffer.AsSpan()));
    }
}
