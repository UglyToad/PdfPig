namespace UglyToad.PdfPig.Graphics.Colors.Icc
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Resolves raw ICC profile bytes into a reusable <see cref="IIccProfile"/> handle.
    /// Implementations should cache parsed profiles (recommended key: profile content hash)
    /// so the same profile is parsed at most once.
    /// <para>
    /// <b>Tolerating malformed profiles is the implementation's job.</b> PdfPig hands over the bytes as the
    /// document wrote them and does not repair them, so anything a colour engine is fussy about has to be
    /// dealt with here. The known case is a profile whose header declares a class other than <c>display</c>:
    /// PDFBox rewrites such a header before handing it to the platform engine, which would otherwise reject
    /// the profile outright (PDFBOX-4114). An implementation wrapping an engine with the same objection
    /// should do the same rather than fail, since PdfPig's only recourse is to abandon colour management for
    /// that colour space and fall back to its alternate.
    /// </para>
    /// <para>
    /// Returning <see langword="false"/>, or throwing, is nonetheless safe: a profile that cannot be parsed,
    /// or that parses and then fails to convert, is dropped and the colour space uses its alternate. What an
    /// implementation must not do is return a profile that reports success and then produces wrong colours.
    /// </para>
    /// </summary>
    public interface IIccProfileService
    {
        /// <summary>
        /// Try to build a parsed profile handle for the given bytes.
        /// </summary>
        bool TryGetProfile(ReadOnlyMemory<byte> profileBytes, [NotNullWhen(true)] out IIccProfile? profile);
    }
}
