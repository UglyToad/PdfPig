namespace UglyToad.PdfPig.XObjects
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Content;
    using Core;
    using Graphics.Colors;
    using Graphics.Core;
    using Images.Png;
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    /// A PostScript image XObject.
    /// </summary>
    public class XObjectImage : IPdfImage
    {
        private readonly Lazy<Memory<byte>>? memoryFactory;

        /// <inheritdoc />
        public PdfRectangle BoundingBox { get; }

        /// <inheritdoc />

        [Obsolete("Use BoundingBox instead.")]
        public PdfRectangle Bounds => BoundingBox;

        /// <inheritdoc />
        public int WidthInSamples { get; }

        /// <inheritdoc />
        public int HeightInSamples { get; }

        /// <inheritdoc />
        public int BitsPerComponent { get; }

        /// <summary>
        /// The JPX filter encodes data using the JPEG2000 compression method.
        /// A JPEG2000 data stream allows different versions of the image to be decoded
        /// allowing for thumbnails to be extracted.
        /// </summary>
        public bool IsJpxEncoded { get; }

        /// <inheritdoc />
        public RenderingIntent RenderingIntent { get; }

        /// <inheritdoc />
        public bool IsImageMask { get; }

        /// <inheritdoc />
        public IReadOnlyList<double> Decode { get; }

        /// <inheritdoc />
        public bool Interpolate { get; }

        /// <inheritdoc />
        public bool IsInlineImage { get; } = false;

        /// <inheritdoc />
        public DictionaryToken ImageDictionary { get; }

        /// <inheritdoc />
        public Memory<byte> RawMemory { get; }

        /// <inheritdoc />
        public Span<byte> RawBytes => RawMemory.Span;

        /// <inheritdoc />
        public ColorSpaceDetails? ColorSpaceDetails { get; }

        /// <inheritdoc />
        public IPdfImage? MaskImage { get; }

        /// <summary>
        /// Creates a new <see cref="XObjectImage"/>.
        /// </summary>
        internal XObjectImage(PdfRectangle bounds,
            int widthInSamples,
            int heightInSamples,
            int bitsPerComponent,
            bool isJpxEncoded,
            bool isImageMask,
            RenderingIntent renderingIntent,
            bool interpolate,
            IReadOnlyList<double> decode,
            DictionaryToken imageDictionary,
            Memory<byte> rawMemory,
            Lazy<Memory<byte>>? bytes,
            ColorSpaceDetails? colorSpaceDetails,
            IPdfImage? softMaskImage)
        {
            BoundingBox = bounds;
            WidthInSamples = widthInSamples;
            HeightInSamples = heightInSamples;
            BitsPerComponent = bitsPerComponent;
            IsJpxEncoded = isJpxEncoded;
            IsImageMask = isImageMask;
            RenderingIntent = renderingIntent;
            Interpolate = interpolate;
            Decode = decode;
            ImageDictionary = imageDictionary ?? throw new ArgumentNullException(nameof(imageDictionary));
            RawMemory = rawMemory;
            ColorSpaceDetails = colorSpaceDetails;
            memoryFactory = bytes;
            MaskImage = softMaskImage;
        }

        /// <inheritdoc />
        public bool TryGetBytesAsMemory(out Memory<byte> bytes)
        {
            bytes = null;
            if (memoryFactory is null)
            {
                return false;
            }

            bytes = memoryFactory.Value;

            return true;
        }

        /// <inheritdoc />
        public bool TryGetPng([NotNullWhen(true)] out byte[]? bytes) => PngFromPdfImageFactory.TryGenerate(this, out bytes);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"XObject Image (w {BoundingBox.Width}, h {BoundingBox.Height}): {ImageDictionary}";
        }
    }
}
