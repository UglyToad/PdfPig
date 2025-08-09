namespace UglyToad.PdfPig.Tests.Util;

using PdfPig.Util;
using System;

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

    [Fact]
    public void CanAddReverse()
    {
        var bufferLen = "startxref".Length;

        const string s = "wibbly bibble startxref 2024";

        var buffer = new CircularByteBuffer(bufferLen);

        for (var i = s.Length - 1; i >= 0; i--)
        {
            var c = s[i];
            buffer.AddReverse((byte)c);

            if (i <= s.Length - bufferLen)
            {
                var str = s.Substring(i, bufferLen);

                Assert.True(buffer.IsCurrentlyEqual(str));
            }
        }
    }
}