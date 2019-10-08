namespace UglyToad.PdfPig.XObjects
{
    using System;
    using System.Collections.Generic;
    using Content;
    using Geometry;
    using Graphics.Colors;
    using Graphics.Core;
    using Tokens;
    using Util.JetBrains.Annotations;

    /// <inheritdoc />
    /// <summary>
    /// A PostScript image XObject.
    /// </summary>
    public class XObjectImage : IPdfImage
    {
        private readonly Lazy<IReadOnlyList<byte>> bytes;

        /// <inheritdoc />
        public PdfRectangle Bounds { get; }

        /// <inheritdoc />
        public int WidthInSamples { get; }

        /// <inheritdoc />
        public int HeightInSamples { get; }

        /// <inheritdoc />
        public ColorSpace? ColorSpace { get; }

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
        public IReadOnlyList<decimal> Decode { get; }

        /// <inheritdoc />
        public bool Interpolate { get; }

        /// <inheritdoc />
        public bool IsInlineImage { get; } = false;

        /// <summary>
        /// The full dictionary for this Image XObject.
        /// </summary>
        [NotNull]
        public DictionaryToken ImageDictionary { get; }

        /// <inheritdoc />
        public IReadOnlyList<byte> RawBytes { get; }

        /// <inheritdoc />
        [NotNull]
        public IReadOnlyList<byte> Bytes => bytes.Value;
        
        /// <summary>
        /// Creates a new <see cref="XObjectImage"/>.
        /// </summary>
        internal XObjectImage(PdfRectangle bounds, int widthInSamples, int heightInSamples, int bitsPerComponent,
            ColorSpace? colorSpace,
            bool isJpxEncoded,
            bool isImageMask,
            RenderingIntent renderingIntent,
            bool interpolate,
            IReadOnlyList<decimal> decode,
            DictionaryToken imageDictionary,
            IReadOnlyList<byte> rawBytes, 
            Lazy<IReadOnlyList<byte>> bytes)
        {
            Bounds = bounds;
            WidthInSamples = widthInSamples;
            HeightInSamples = heightInSamples;
            BitsPerComponent = bitsPerComponent;
            ColorSpace = colorSpace;
            IsJpxEncoded = isJpxEncoded;
            IsImageMask = isImageMask;
            RenderingIntent = renderingIntent;
            Interpolate = interpolate;
            Decode = decode;
            ImageDictionary = imageDictionary ?? throw new ArgumentNullException(nameof(imageDictionary));
            RawBytes = rawBytes;
            this.bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"XObject Image (w {Bounds.Width}, h {Bounds.Height}): {ImageDictionary}";
        }
    }
}
