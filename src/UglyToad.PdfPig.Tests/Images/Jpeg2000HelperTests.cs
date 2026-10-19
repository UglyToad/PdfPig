namespace UglyToad.PdfPig.Tests.Images
{
    using PdfPig.Images;
    using System;
    using UglyToad.PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.Tests.Integration;

    public class Jpeg2000HelperTests
    {
        private static readonly ParsingOptions ParsingOptions = new();

        private static readonly Lazy<string> DocumentFolder = new Lazy<string>(() => Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Images", "Files", "Jpx")));

        public static IEnumerable<object[]> GetAllJp2Files
        {
            get
            {
                return Directory.GetFiles(DocumentFolder.Value, "*.jp2").Select(x => new object[] { Path.GetFileName(x) });
            }
        }

        [Theory]
        [MemberData(nameof(GetAllJp2Files))]
        public void GetJp2BitsPerComponent_ReturnsCorrectBitsPerComponent_WhenValidInput(string path)
        {
            byte[] image = File.ReadAllBytes(Path.Combine(DocumentFolder.Value, path));
            Assert.Equal(8, Jpeg2000Helper.GetBitsPerComponent(image));
        }

        [Fact]
        public void GetJp2BitsPerComponent_ThrowsException_WhenInputIsTooShort()
        {
            Assert.Throws<InvalidOperationException>(() => Jpeg2000Helper.GetBitsPerComponent(new byte[11]));
        }
        
        [Fact]
        public void GetJp2BitsPerComponent_ThrowsException_WhenSignatureBoxIsInvalid()
        {
            Assert.Throws<InvalidOperationException>(() => Jpeg2000Helper.GetBitsPerComponent(new byte[12]));
        }

        [Theory]
        [MemberData(nameof(GetAllJp2Files))]
        public void GetJp2NumberOfComponents_ReturnsDeviceComponentCount_WhenValidInput(string path)
        {
            byte[] image = File.ReadAllBytes(Path.Combine(DocumentFolder.Value, path));
            ushort components = Jpeg2000Helper.GetNumberOfComponents(image);
            Assert.Contains(components, new ushort[] { 1, 3, 4 });
        }

        [Fact]
        public void GetJp2NumberOfComponents_ThrowsException_WhenInputIsTooShort()
        {
            Assert.Throws<InvalidOperationException>(() => Jpeg2000Helper.GetNumberOfComponents(new byte[11]));
        }

        [Theory]
        [MemberData(nameof(GetAllJp2Files))]
        public void GetJpxColorSpaceDetails_ReturnsDeviceRgb_ForEnumeratedSrgbJp2(string path)
        {
            byte[] image = File.ReadAllBytes(Path.Combine(DocumentFolder.Value, path));
            var colorSpace = Jpeg2000Helper.GetJpxColorSpaceDetails(image, ParsingOptions);
            // The JP2 test files all declare enumerated colour space 16 (sRGB) with three components,
            // which maps to DeviceRGB.
            Assert.IsType<DeviceRgbColorSpaceDetails>(colorSpace);
        }

        [Fact]
        public void GetJpxColorSpaceDetails_ReturnsDeviceColorSpace_ForRawJ2KCodestream()
        {
            // A raw J2K codestream (no JP2 boxes) carries no colour space box, so the colour space is
            // inferred from the channel count (three channels here -> DeviceRGB).
            string path = IntegrationHelpers.GetSpecificTestDocumentPath("GHOSTSCRIPT-688999-2.pdf");

            using (var document = PdfDocument.Open(path))
            {
                var page1 = document.GetPage(1);
                var jpxImage = page1.GetImages().Single();

                Assert.IsType<DeviceRgbColorSpaceDetails>(Jpeg2000Helper.GetJpxColorSpaceDetails(jpxImage.RawMemory, ParsingOptions));
            }
        }

        [Fact]
        public void GetJpxColorSpaceDetails_ReturnsDeviceRgb_ForSrgbJp2WithAnOpacityChannel()
        {
            // Four components, the channel definition box making the fourth opacity: three colour channels, not CMYK.
            var cdef = new byte[] { 0, 4, 0, 0, 0, 0, 0, 1, 0, 1, 0, 0, 0, 2, 0, 2, 0, 0, 0, 3, 0, 3, 0, 1, 0, 0 };
            var image = Jp2(4, Box("cdef", cdef));

            Assert.IsType<DeviceRgbColorSpaceDetails>(Jpeg2000Helper.GetJpxColorSpaceDetails(image, ParsingOptions));
        }

        [Fact]
        public void GetJpxColorSpaceDetails_ReturnsDeviceRgb_ForSrgbJp2WithAPalette()
        {
            // One component mapped through three palette columns: three colour channels, not grey.
            var cmap = new byte[] { 0, 0, 1, 0, 0, 0, 1, 1, 0, 0, 1, 2 };
            var image = Jp2(1, Box("cmap", cmap));

            Assert.IsType<DeviceRgbColorSpaceDetails>(Jpeg2000Helper.GetJpxColorSpaceDetails(image, ParsingOptions));
        }

        /// <summary>
        /// A JP2 file as far as the helper reads it: its signature, an sRGB header holding <paramref name="headerBoxes"/>, and a
        /// codestream of its SOC and SIZ markers alone, for 2x2 samples of <paramref name="components"/> 8-bit components.
        /// </summary>
        private static byte[] Jp2(int components, params byte[][] headerBoxes)
        {
            var siz = new List<byte> { 0xFF, 0x4F, 0xFF, 0x51 };
            siz.AddRange(BigEndian16(38 + 3 * components));
            siz.AddRange(BigEndian16(0));
            foreach (var value in new[] { 2, 2, 0, 0, 2, 2, 0, 0 })
            {
                siz.AddRange(BigEndian32(value));
            }

            siz.AddRange(BigEndian16(components));
            for (var i = 0; i < components; i++)
            {
                siz.AddRange(new byte[] { 7, 1, 1 });
            }

            var header = new List<byte>(Box("colr", new byte[] { 1, 0, 0, 0, 0, 0, 16 }));
            foreach (var box in headerBoxes)
            {
                header.AddRange(box);
            }

            var file = new List<byte> { 0, 0, 0, 0x0C, 0x6A, 0x50, 0x20, 0x20, 0x0D, 0x0A, 0x87, 0x0A };
            file.AddRange(Box("jp2h", header.ToArray()));
            file.AddRange(Box("jp2c", siz.ToArray()));
            return file.ToArray();
        }

        private static byte[] Box(string type, byte[] content)
        {
            var box = new List<byte>(BigEndian32(8 + content.Length));
            box.AddRange(System.Text.Encoding.ASCII.GetBytes(type));
            box.AddRange(content);
            return box.ToArray();
        }

        private static byte[] BigEndian32(int value) => new[] { (byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value };

        private static byte[] BigEndian16(int value) => new[] { (byte)(value >> 8), (byte)value };

        [Fact]
        public void GetJpxColorSpaceDetails_ThrowsException_WhenInputIsTooShort()
        {
            Assert.Throws<InvalidOperationException>(() => Jpeg2000Helper.GetJpxColorSpaceDetails(new byte[11], ParsingOptions));
        }

        [Fact]
        public void GetJp2BitsPerComponentJ2K()
        {
            string path = IntegrationHelpers.GetSpecificTestDocumentPath("GHOSTSCRIPT-688999-2.pdf");

            using (var document = PdfDocument.Open(path))
            {
                var page1 = document.GetPage(1);
                var jpxImage = page1.GetImages().Single();

                var bpc = Jpeg2000Helper.GetBitsPerComponent(jpxImage.RawBytes);

                Assert.Equal(8, bpc);
            }
        }
    }
}
