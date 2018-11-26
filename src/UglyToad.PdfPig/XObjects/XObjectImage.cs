namespace UglyToad.PdfPig.XObjects
{
    using System;
    using System.Collections.Generic;
    using Tokens;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// The raw stream from a PDF document representing an image XObject.
    /// </summary>
    public class XObjectImage
    {
        /// <summary>
        /// The width of the image in samples.
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// The height of the image in samples.
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// The JPX filter encodes data using the JPEG2000 compression method.
        /// A JPEG2000 data stream allows different versions of the image to be decoded
        /// allowing for thumbnails to be extracted.
        /// </summary>
        public bool IsJpxEncoded { get; }

        /// <summary>
        /// Whether this image should be treated as an image maske.
        /// </summary>
        public bool IsImageMask { get; }

        /// <summary>
        /// The full dictionary for this Image XObject.
        /// </summary>
        [NotNull]
        public DictionaryToken ImageDictionary { get; }

        /// <summary>
        /// The encoded bytes of this image, must be decoded via any
        /// filters defined in the <see cref="ImageDictionary"/> prior to consumption.
        /// </summary>
        [NotNull]
        public IReadOnlyList<byte> Bytes { get; }

        /// <summary>
        /// Creates a new <see cref="XObjectImage"/>.
        /// </summary>
        internal XObjectImage(int width, int height, bool isJpxEncoded, bool isImageMask, DictionaryToken imageDictionary, IReadOnlyList<byte> bytes)
        {
            Width = width;
            Height = height;
            IsJpxEncoded = isJpxEncoded;
            IsImageMask = isImageMask;
            ImageDictionary = imageDictionary ?? throw new ArgumentNullException(nameof(imageDictionary));
            Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return ImageDictionary.ToString();
        }
    }
}
