namespace UglyToad.PdfPig.Tests.Writer.TestImages
{
    using System;
    using System.IO;

    /// <summary>
    /// Helper for loading the CCITT Group 4 TIFF fixture used by PDF page builder tests.
    /// </summary>
    internal sealed class CcittG4TestImage
    {
        private CcittG4TestImage(int width, int height, byte[] rawCcittData, string sourcePath, bool blackIs1)
        {
            Width = width;
            Height = height;
            RawCcittData = rawCcittData;
            SourcePath = sourcePath;
            BlackIs1 = blackIs1;
        }

        public int Width { get; }

        public int Height { get; }

        public byte[] RawCcittData { get; }

        public string SourcePath { get; }

        public bool BlackIs1 { get; }

        /// <summary>
        /// Loads the CCITT Group 4 sample image relative to the test output directory.
        /// </summary>
        public static CcittG4TestImage Load()
        {
            var tiffPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..",
                "Images", "Files", "Tif", "TiffCcittG4.tif"));

            LibTiffSilencer.SuppressWarnings();

            var payload = CcittExtractor.FromTiff(tiffPath);
            return new CcittG4TestImage(payload.Width, payload.Height, payload.Data, tiffPath, payload.BlackIs1);
        }
    }
}