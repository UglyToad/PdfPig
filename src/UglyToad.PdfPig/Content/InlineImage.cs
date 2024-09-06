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
        private readonly Lazy<ReadOnlyMemory<byte>>? memoryFactory;

        /// <inheritdoc />
        public PdfRectangle Bounds { get; }

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
        public ReadOnlyMemory<byte> RawMemory { get; }

        /// <inheritdoc />
        public ReadOnlySpan<byte> RawBytes => RawMemory.Span;

        /// <inheritdoc />
        public ColorSpaceDetails ColorSpaceDetails { get; }

        /// <inheritdoc />
        public IPdfImage? SoftMaskImage { get; }

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
            ReadOnlyMemory<byte> rawMemory,
            ILookupFilterProvider filterProvider,
            IReadOnlyList<NameToken> filterNames,
            DictionaryToken streamDictionary,
            ColorSpaceDetails colorSpaceDetails,
            IPdfImage? softMaskImage)
        {
            Bounds = bounds;
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

            memoryFactory = supportsFilters ? new Lazy<ReadOnlyMemory<byte>>(() =>
            {
                var b = RawMemory;
                for (var i = 0; i < filters.Count; i++)
                {
                    var filter = filters[i];
                    b = filter.Decode(b.Span, streamDictionary, filterProvider, i);
                }

                return b;
            }) : null;

            SoftMaskImage = softMaskImage;
        }

        /// <inheritdoc />
        public bool TryGetBytesAsMemory(out ReadOnlyMemory<byte> bytes)
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
            return $"Inline Image (w {Bounds.Width}, h {Bounds.Height})";
        }
    }
}
