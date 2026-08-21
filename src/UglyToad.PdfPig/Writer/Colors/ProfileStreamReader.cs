namespace UglyToad.PdfPig.Writer.Colors
{
    using System;
    using System.IO;
    using System.Linq;
    using PdfFonts.Parser;

    internal static class ProfileStreamReader
    {
        /// <summary>
        /// The number of colour components of the profile returned by <see cref="GetSRgb2014"/>.
        /// </summary>
        public const int SRgb2014NumberOfComponents = 3;

        public static byte[] GetSRgb2014()
        {
            var resources = typeof(ProfileStreamReader).Assembly.GetManifestResourceNames();

            var resource = resources.FirstOrDefault(x =>
                x.EndsWith("sRGB2014.icc", StringComparison.InvariantCultureIgnoreCase));

            if (resource is null)
            {
                throw new InvalidOperationException("Could not find the sRGB ICC color profile stream.");
            }

            byte[] bytes;
            using (var stream = typeof(CMapParser).Assembly.GetManifestResourceStream(resource))
            using (var memoryStream = new MemoryStream())
            {
                stream?.CopyTo(memoryStream);

                bytes = memoryStream.ToArray();
            }

            return bytes;
        }
    }
}
