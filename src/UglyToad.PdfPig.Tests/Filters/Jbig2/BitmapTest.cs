namespace UglyToad.PdfPig.Tests.Filters.Jbig2
{
    using System;
    using Xunit;
    using UglyToad.PdfPig.Filters.Jbig2;

    public class BitmapTest
    {
        [Fact]
        public void GetPixelAndSetPixelTest()
        {
            var bitmap = new Bitmap(37, 49);
            Assert.Equal(0, bitmap.GetPixel(3, 19));

            bitmap.SetPixel(3, 19, (byte)1);

            Assert.Equal(1, bitmap.GetPixel(3, 19));
        }

        [Fact]
        public void GetByteAndSetByteTest()
        {
            var bitmap = new Bitmap(16, 16);

            byte value = (byte)4;
            bitmap.SetByte(0, value);
            bitmap.SetByte(31, value);

            Assert.Equal(value, bitmap.GetByte(0));
            Assert.Equal(value, bitmap.GetByte(31));
        }

        [Fact]
        public void GetByteThrowsExceptionTest()
        {
            var bitmap = new Bitmap(16, 16);

            Action action = () => bitmap.GetByte(32);

            Assert.Throws<IndexOutOfRangeException>(action);
        }

        [Fact]
        public void SetByteThrowsExceptionTest()
        {
            var bitmap = new Bitmap(16, 16);

            Action action = () => bitmap.SetByte(32, (byte)0);

            Assert.Throws<IndexOutOfRangeException>(action);
        }

        [Fact]
        public void GetByteAsIntegerTest()
        {
            var bitmap = new Bitmap(16, 16);

            var byteValue = (byte)4;
            int integerValue = byteValue;
            bitmap.SetByte(0, byteValue);
            bitmap.SetByte(31, byteValue);

            Assert.Equal(integerValue, bitmap.GetByteAsInteger(0));
            Assert.Equal(integerValue, bitmap.GetByteAsInteger(31));

        }

        [Fact]
        public void GetHeightTest()
        {
            int height = 16;
            var bitmap = new Bitmap(1, height);

            Assert.Equal(height, bitmap.Height);
        }

        [Fact]
        public void GetWidthTest()
        {
            int width = 16;
            var bitmap = new Bitmap(width, 1);

            Assert.Equal(width, bitmap.Width);
        }
    }
}
