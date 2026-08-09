namespace UglyToad.PdfPig.Graphics.Colors.Icc
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Resolves raw ICC profile bytes into a reusable <see cref="IIccProfile"/> handle.
    /// Implementations should cache parsed profiles (recommended key: profile content hash)
    /// so the same profile is parsed at most once.
    /// </summary>
    public interface IIccProfileService
    {
        /// <summary>
        /// Try to build a parsed profile handle for the given bytes.
        /// </summary>
        bool TryGetProfile(ReadOnlyMemory<byte> profileBytes, [NotNullWhen(true)] out IIccProfile? profile);
    }
}
