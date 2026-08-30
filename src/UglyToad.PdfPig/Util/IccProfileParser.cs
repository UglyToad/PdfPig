namespace UglyToad.PdfPig.Util
{
    using System;
    using Graphics.Colors.Icc;
    using Logging;

    /// <summary>
    /// Hands profile bytes to the configured <see cref="IIccProfileService"/>.
    /// </summary>
    internal static class IccProfileParser
    {
        /// <summary>
        /// Parse <paramref name="profileBytes"/> through <paramref name="service"/>, returning
        /// <see langword="null"/> rather than throwing when it cannot be parsed. The colour space that
        /// asked for it falls back to its alternate.
        /// </summary>
        public static IIccProfile? Parse(ReadOnlyMemory<byte> profileBytes, IIccProfileService service, ILog? log)
        {
            try
            {
                if (service.TryGetProfile(profileBytes, out var profile))
                {
                    return profile;
                }

                log?.Warn("An ICC profile could not be parsed by the configured IIccProfileService; " +
                          "the colour space will use its alternate.");
            }
            catch (Exception ex)
            {
                // TryGetProfile is third-party code. A profile that makes it throw is no worse than one it
                // declines, so it costs the colour space its profile and nothing more.
                log?.Error("The configured IIccProfileService threw while parsing an ICC profile; " +
                           "the colour space will use its alternate.", ex);
            }

            return null;
        }
    }
}
