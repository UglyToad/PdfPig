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
    /// dealt with here.
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

        /// <summary>
        /// Should device colours (DeviceGray, DeviceRGB, DeviceCMYK) be colour-managed through the
        /// document's output intent <c>/DestOutputProfile</c> when one is present (14.11.5, "Output
        /// intents", and 8.6.5.7)?
        /// <para>
        /// This lives here rather than alongside the other parsing options because applying an output intent
        /// is impossible without a service to parse its profile: the two travel together, so a service that
        /// answers <see langword="false"/> - or the absence of a service - is the only way to express "do not
        /// colour-manage", and the contradictory combination cannot be written down.
        /// </para>
        /// <para>
        /// 14.11.5 says the data in an output intent dictionary "shall be for informational purposes only,
        /// and PDF processors are free to disregard it", and sets "no expectation" that device colours are
        /// converted to the target space - conversion is "undesirable" in some workflows. An implementation
        /// should therefore answer <see langword="false"/> unless it is deliberately previewing or proofing.
        /// </para>
        /// </summary>
        bool UseOutputIntent { get; }

        /// <summary>
        /// Which output intent to colour-manage through when a document declares several: the <c>/S</c>
        /// subtype to rank ahead of all others, matched exactly. Only consulted when
        /// <see cref="UseOutputIntent"/> is <see langword="true"/>.
        /// <para>
        /// <see cref="OutputIntent.PdfXSubtype"/> is the usual answer - PDF/X exists to pin down device
        /// colour, which is the question being asked - and it is what
        /// <see cref="OutputIntent.SelectForColorManagement"/> ranks first anyway, so it selects what the
        /// built-in order would. <see langword="null"/> is that built-in order with no preference at all.
        /// </para>
        /// <para>
        /// A preference reorders and never filters: a document declaring no intent of this subtype falls
        /// back to the built-in order rather than losing colour management.
        /// </para>
        /// </summary>
        string? PreferredOutputIntentSubtype { get; }
    }
}
