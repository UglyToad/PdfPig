﻿namespace UglyToad.PdfPig.XObjects
{
    using System;
    using System.Collections.Generic;
    using Content;
    using Core;
    using Graphics.Colors;
    using Graphics.Core;
    using Images.Png;
    using Tokens;
    using Util.JetBrains.Annotations;

    /// <inheritdoc />
    /// <summary>
    /// A PostScript image XObject.
    /// </summary>
    public class XObjectImage : IPdfImage
    {
        [CanBeNull]
        private readonly Lazy<IReadOnlyList<byte>> bytesFactory;

        /// <inheritdoc />
        public PdfRectangle Bounds { get; }

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
        public IReadOnlyList<decimal> Decode { get; }

        /// <inheritdoc />
        public bool Interpolate { get; }

        /// <inheritdoc />
        public bool IsInlineImage { get; } = false;

        /// <inheritdoc />
        [NotNull]
        public DictionaryToken ImageDictionary { get; }

        /// <inheritdoc />
        public IReadOnlyList<byte> RawBytes { get; }

        /// <inheritdoc />
        public ColorSpaceDetails ColorSpaceDetails { get; }

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
            IReadOnlyList<decimal> decode,
            DictionaryToken imageDictionary,
            IReadOnlyList<byte> rawBytes,
            Lazy<IReadOnlyList<byte>> bytes,
            ColorSpaceDetails colorSpaceDetails)
        {
            Bounds = bounds;
            WidthInSamples = widthInSamples;
            HeightInSamples = heightInSamples;
            BitsPerComponent = bitsPerComponent;
            IsJpxEncoded = isJpxEncoded;
            IsImageMask = isImageMask;
            RenderingIntent = renderingIntent;
            Interpolate = interpolate;
            Decode = decode;
            ImageDictionary = imageDictionary ?? throw new ArgumentNullException(nameof(imageDictionary));
            RawBytes = rawBytes;
            ColorSpaceDetails = colorSpaceDetails;
            bytesFactory = bytes;
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
            return $"XObject Image (w {Bounds.Width}, h {Bounds.Height}): {ImageDictionary}";
        }
    }
}
