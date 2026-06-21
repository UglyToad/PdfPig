namespace UglyToad.PdfPig.Graphics.Colors.Icc
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Resolves raw ICC profile bytes into a reusable <see cref="IIccProfile"/> handle.
    /// Implementations should cache parsed profiles (recommended key: profile content hash
    /// plus component count) so the same profile is parsed at most once.
    /// </summary>
    /// <remarks>
    /// When <see cref="ParsingOptions.IccProfileService"/> is <c>null</c>
    /// or this method returns <c>false</c>, <see cref="ICCBasedColorSpaceDetails"/>
    /// falls back silently to its declared
    /// <see cref="ICCBasedColorSpaceDetails.AlternateColorSpace"/>.
    /// <para>
    /// Per-intent transforms are obtained from the returned <see cref="IIccProfile"/> via
    /// <see cref="IIccProfile.TryGetTransform(Core.RenderingIntent, out IIccTransform)"/>.
    /// Intent is not bound at profile-resolution time because a single
    /// PDF color space may be used with multiple intents on the same page.
    /// </para>
    /// </remarks>
    public interface IIccProfileService
    {
        /// <summary>
        /// Try to build a parsed profile handle for the given bytes.
        /// </summary>
        bool TryGetProfile(
            Memory<byte> profileBytes,
            int numberOfColorComponents,
            [NotNullWhen(true)] out IIccProfile? profile);
    }
}