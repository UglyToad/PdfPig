﻿namespace UglyToad.PdfPig.Images.Png
{
    internal class PngOpenerSettings
    {
        /// <summary>
        /// The code to execute whenever a chunk is read. Can be <see langword="null"/>.
        /// </summary>
        public IChunkVisitor? ChunkVisitor { get; set; }

        /// <summary>
        /// Whether to throw if the image contains data after the image end marker.
        /// <see langword="false"/> by default.
        /// </summary>
        public bool DisallowTrailingData { get; set; }
    }
}