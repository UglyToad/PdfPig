﻿namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Filters;
    using Graphics.Colors;
    using Graphics.Core;
    using Tokens;
    using Images.Png;
    using UglyToad.PdfPig.Util.JetBrains.Annotations;

    /// <inheritdoc />
    /// <summary>
    /// A small image that is completely defined directly inline within a <see cref="T:UglyToad.PdfPig.Content.Page" />'s content stream.
    /// </summary>
    public class InlineImage : IPdfImage
    {
        private readonly Lazy<IReadOnlyList<byte>> bytesFactory;

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
        public IReadOnlyList<decimal> Decode { get; }

        /// <inheritdoc />
        public bool IsInlineImage { get; } = true;

        /// <inheritdoc />
        [NotNull]
        public DictionaryToken ImageDictionary { get; }

        /// <inheritdoc />
        public RenderingIntent RenderingIntent { get; }

        /// <inheritdoc />
        public bool Interpolate { get; }

        /// <inheritdoc />
        public IReadOnlyList<byte> RawBytes { get; }

        /// <inheritdoc />
        public ColorSpaceDetails ColorSpaceDetails { get; }

        /// <summary>
        /// Create a new <see cref="InlineImage"/>.
        /// </summary>
        internal InlineImage(PdfRectangle bounds, int widthInSamples, int heightInSamples, int bitsPerComponent, bool isImageMask,
            RenderingIntent renderingIntent,
            bool interpolate,
            IReadOnlyList<decimal> decode,
            IReadOnlyList<byte> bytes,
            IReadOnlyList<IFilter> filters,
            DictionaryToken streamDictionary,
            ColorSpaceDetails colorSpaceDetails)
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

            RawBytes = bytes;
            ColorSpaceDetails = colorSpaceDetails;

            var supportsFilters = true;
            foreach (var filter in filters)
            {
                if (!filter.IsSupported)
                {
                    supportsFilters = false;
                    break;
                }
            }

            bytesFactory = supportsFilters ? new Lazy<IReadOnlyList<byte>>(() =>
            {
                var b = bytes.ToArray();
                for (var i = 0; i < filters.Count; i++)
                {
                    var filter = filters[i];
                    b = filter.Decode(b, streamDictionary, i);
                }

                return b;
            }) : null;
        }

        /// <inheritdoc />
        public bool TryGetBytes(out IReadOnlyList<byte> bytes)
        {
            bytes = null;
            if (bytesFactory == null)
            {
                return false;
            }

            bytes = bytesFactory.Value;

            return true;
        }

        /// <inheritdoc />
        public bool TryGetPng(out byte[] bytes) => PngFromPdfImageFactory.TryGenerate(this, out bytes);

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Inline Image (w {Bounds.Width}, h {Bounds.Height})";
        }
    }
}
