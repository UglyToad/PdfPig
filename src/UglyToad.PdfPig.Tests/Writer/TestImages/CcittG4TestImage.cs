namespace UglyToad.PdfPig.Tests.Writer.TestImages
{
    using System;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Helper for loading the CCITT Group 4 fixture used by PDF page builder tests.
    /// </summary>
    internal sealed class CcittG4TestImage
    {
        private CcittG4TestImage(int width, int height, byte[] rawCcittData, bool blackIs1)
        {
            Width = width;
            Height = height;
            RawCcittData = rawCcittData;
            BlackIs1 = blackIs1;
        }

        public int Width { get; }

        public int Height { get; }

        public byte[] RawCcittData { get; }

        public bool BlackIs1 { get; }

        /// <summary>
        /// Loads the CCITT Group 4 sample payload relative to the test output directory.
        /// The payload is already raw CCITT data; width, height and polarity come from the generated metadata
        /// derived from the original TIFF using the same extraction logic as the external converter.
        /// </summary>
        public static CcittG4TestImage Load()
        {
            var metadataPath = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Files", "Tif",
                "TiffCcittG4.fixture.json"));
            var base64Path = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "Files", "Tif",
                "TiffCcittG4.ccitt.base64"));
            var metadataJson = File.ReadAllText(metadataPath, Encoding.UTF8);
            var metadata = CcittG4FixtureMetadata.Parse(metadataJson);
            var base64 = File.ReadAllText(base64Path, Encoding.ASCII)
                .Replace("\r", string.Empty)
                .Replace("\n", string.Empty)
                .Trim();
            var rawCcittData = Convert.FromBase64String(base64);

            return new CcittG4TestImage(metadata.Width, metadata.Height, rawCcittData, metadata.BlackIs1);
        }

        private sealed class CcittG4FixtureMetadata
        {
            public int Width { get; private set; }

            public int Height { get; private set; }

            public bool BlackIs1 { get; private set; }

            public static CcittG4FixtureMetadata Parse(string json)
            {
                return new CcittG4FixtureMetadata
                {
                    Width = ReadInt(json, "Width"),
                    Height = ReadInt(json, "Height"),
                    BlackIs1 = ReadBool(json, "BlackIs1")
                };
            }

            private static int ReadInt(string json, string propertyName)
            {
                var match = Regex.Match(json, $"\"{propertyName}\"\\s*:\\s*(\\d+)");
                if (!match.Success)
                {
                    throw new InvalidOperationException($"Missing integer property '{propertyName}' in CCITT fixture metadata.");
                }

                return int.Parse(match.Groups[1].Value);
            }

            private static bool ReadBool(string json, string propertyName)
            {
                var match = Regex.Match(json, $"\"{propertyName}\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
                if (!match.Success)
                {
                    throw new InvalidOperationException($"Missing boolean property '{propertyName}' in CCITT fixture metadata.");
                }

                return bool.Parse(match.Groups[1].Value);
            }
        }
    }
}
