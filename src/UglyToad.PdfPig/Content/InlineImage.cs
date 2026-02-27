namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Core;
    using Filters;
    using Graphics.Colors;
    using Graphics.Core;
    using Tokens;
    using Images.Png;

    /// <inheritdoc />
    /// <summary>
    /// A small image that is completely defined directly inline within a <see cref="T:UglyToad.PdfPig.Content.Page" />'s content stream.
    /// </summary>
    public class InlineImage : IPdfImage
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

        /// <inheritdoc />
        public bool IsImageMask { get; }

        /// <inheritdoc />
        public IReadOnlyList<double> Decode { get; }

        /// <inheritdoc />
        public bool IsInlineImage { get; } = true;

        /// <inheritdoc />
        public DictionaryToken ImageDictionary { get; }

        /// <inheritdoc />
        public RenderingIntent RenderingIntent { get; }

        /// <inheritdoc />
        public bool Interpolate { get; }

        /// <inheritdoc />
        public Memory<byte> RawMemory { get; }

        /// <inheritdoc />
        public Span<byte> RawBytes => RawMemory.Span;

        /// <inheritdoc />
        public ColorSpaceDetails ColorSpaceDetails { get; }

        /// <inheritdoc />
        public IPdfImage? MaskImage { get; }

        /// <summary>
        /// Create a new <see cref="InlineImage"/>.
        /// </summary>
        internal InlineImage(PdfRectangle bounds,
            int widthInSamples,
            int heightInSamples,
            int bitsPerComponent,
            bool isImageMask,
            RenderingIntent renderingIntent,
            bool interpolate,
            IReadOnlyList<double> decode,
            Memory<byte> rawMemory,
            ILookupFilterProvider filterProvider,
            IReadOnlyList<NameToken> filterNames,
            DictionaryToken streamDictionary,
            ColorSpaceDetails colorSpaceDetails,
            IPdfImage? softMaskImage)
        {
            IsInlineImage = true;
            BoundingBox = bounds;
            WidthInSamples = widthInSamples;
            HeightInSamples = heightInSamples;
            Decode = decode;
            BitsPerComponent = bitsPerComponent;
            IsImageMask = isImageMask;
            RenderingIntent = renderingIntent;
            Interpolate = interpolate;
            ImageDictionary = streamDictionary;
            RawMemory = rawMemory;
            ColorSpaceDetails = colorSpaceDetails;

            var filters = filterProvider.GetNamedFilters(filterNames);
            
            var supportsFilters = true;
            foreach (var filter in filters)
            {
                if (!filter.IsSupported)
                {
                    supportsFilters = false;
                    break;
                }
            }

            memoryFactory = supportsFilters ? new Lazy<Memory<byte>>(() =>
            {
                var b = RawMemory;
                for (var i = 0; i < filters.Count; i++)
                {
                    var filter = filters[i];
                    b = filter.Decode(b, streamDictionary, filterProvider, i);
                }

                return b;
            }) : null;

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
            return $"Inline Image (w {BoundingBox.Width}, h {BoundingBox.Height})";
        }
    }
}
