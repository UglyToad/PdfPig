namespace UglyToad.PdfPig.Tests
{
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Graphics.Colors;
    using UglyToad.PdfPig.Graphics.Core;
    using UglyToad.PdfPig.Images.Png;
    using UglyToad.PdfPig.Tokens;

    public class TestPdfImage : IPdfImage
    {
        public PdfRectangle Bounds { get; set; }

        public int WidthInSamples { get; set; }

        public int HeightInSamples { get; set; }

        public int BitsPerComponent { get; set; } = 8;

        public IReadOnlyList<byte> RawBytes { get; }

        public RenderingIntent RenderingIntent { get; set; } = RenderingIntent.RelativeColorimetric;

        public bool IsImageMask { get; set; }

        public IReadOnlyList<double> Decode { get; set; }

        public bool Interpolate { get; set; }

        public bool IsInlineImage { get; set; }

        public DictionaryToken ImageDictionary { get; set; }

        public ColorSpaceDetails ColorSpaceDetails { get; set; }

        public IReadOnlyList<byte> DecodedBytes { get; set; }

        public bool TryGetBytes(out IReadOnlyList<byte> bytes)
        {
            bytes = DecodedBytes;
            return bytes != null;
        }

        public bool TryGetPng(out byte[] bytes) => PngFromPdfImageFactory.TryGenerate(this, out bytes);
    }
}
