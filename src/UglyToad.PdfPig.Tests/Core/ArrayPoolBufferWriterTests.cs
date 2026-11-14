namespace UglyToad.PdfPig.Tests.Core
{
    using System;
    using UglyToad.PdfPig.Core;

    public class ArrayPoolBufferWriterTests
    {
        [Fact]
        public void GrowsGeometricallyPastOneMegabyte()
        {
            const int initialSize = 1024 * 1024;
            using var writer = new ArrayPoolBufferWriter<byte>(initialSize);

            var capacityBeforeGrowth = writer.GetSpan(0).Length;
            for (var i = 0; i < capacityBeforeGrowth; i++)
            {
                writer.Write((byte)(i & 0xFF));
            }

            // Trigger growth by 1 byte; geometric growth should allocate at least 2x capacity.
            var nextSpan = writer.GetSpan(1);
            Assert.True(nextSpan.Length >= capacityBeforeGrowth,
                $"Expected geometric growth to provide at least {capacityBeforeGrowth} remaining bytes, got {nextSpan.Length}.");

            writer.Write(0x42);
            Assert.Equal(capacityBeforeGrowth + 1, writer.WrittenCount);
            Assert.Equal(0x42, writer.WrittenSpan[capacityBeforeGrowth]);
        }
    }
}
