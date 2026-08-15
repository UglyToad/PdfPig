namespace UglyToad.PdfPig.Graphics.Colors.Icc
{
    using System;
    using System.Collections.Generic;
    using Tokens;

    /// <summary>
    /// Output intents (PDF 1.4) provide a means for matching the colour characteristics of page content in a PDF
    /// document with those of a target output device. The optional OutputIntents entry in the document catalog
    /// dictionary (see 7.7.2, "Document catalog dictionary") or a Page dictionary (see 7.7.3.3, "Page objects")
    /// holds an array of output intent dictionaries, each describing the colour reproduction characteristics of a
    /// possible output device. The contents of these dictionaries will often vary for different devices. The
    /// dictionary's S entry specifies an output intent subtype that determines the format and meaning of the
    /// remaining entries. 
    /// </summary>
    public sealed class OutputIntent
    {
        /// <summary>
        /// (Required) The output intent subtype. The value may be <c>GTS_PDFX</c>, <c>GTS_PDFA1</c>, <c>ISO_PDFE1</c>
        /// or a key defined by an ISO 32000 extension.
        /// <para>
        /// <see langword="null"/> when the entry is absent. It is required, so its absence is itself worth
        /// knowing - a conformance check cannot tell that from an entry present but empty.
        /// </para>
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// (Optional) A text string concisely identifying the intended output device or production condition in
        /// human-readable form. This is the preferred method of defining such a string for presentation to the user.
        /// </summary>
        public string? OutputCondition { get; }

        /// <summary>
        /// (Required) A text string identifying the intended output device or production condition in human- or
        /// machine-readable form. If human-readable, this string may be used in lieu of an OutputCondition string
        /// for presentation to the user. <see langword="null"/> when the entry is absent.
        /// </summary>
        public string? OutputConditionIdentifier { get; }

        /// <summary>
        /// (Optional) A text string (conventionally a uniform resource identifier, or URI) identifying the registry
        /// in which the condition designated by OutputConditionIdentifier is defined. 
        /// </summary>
        public string? RegistryName { get; }

        /// <summary>
        /// (Required if OutputConditionIdentifier does not specify a standard production condition; optional otherwise)
        /// A human-readable text string containing additional information or comments about the intended target device
        /// or production condition.
        /// </summary>
        public string? Info { get; }

        /// <summary>
        /// (Required if OutputConditionIdentifier does not specify a standard production condition; optional otherwise)
        /// An ICC profile stream defining the transformation from the PDF document's source colours to output device
        /// colourants. The format of the profile stream is the same as that used in specifying an ICCBased colour space
        /// (see 8.6.5.5, "ICCBased colour spaces"). The output transformation uses the profile's "from CIE" information
        /// (BToA in ICC terminology); the "to CIE" (AToB) information may optionally be used to remap source colour
        /// values to some other destination colour space, such as for screen preview or hardcopy proofing.
        /// <para>
        /// PdfPig parses this profile but does not itself apply it: no colour space converts through the output
        /// intent. It is here for a caller that wants to drive the transformation, or simply to inspect what the
        /// document targets.
        /// </para>
        /// </summary>
        public IIccProfile? DestOutputProfile { get; }

        /// <summary>
        /// (Optional; PDF 2. 0) A reference to an ICC profile that is not embedded in the document
        /// (ISO 32000-2 Table 402). PdfPig does not resolve externally referenced profiles, so this is
        /// exposed for inspection only and does not contribute to <see cref="DestOutputProfile"/>.
        /// </summary>
        public IccProfileReference? DestOutputProfileRef { get; }

        /// <summary>
        /// (Optional, PDF 2. 0) A DeviceN Mixing Hints dictionary ("Table 72 — Entries in a DeviceN mixing hints
        /// dictionary") which shall not contain a DotGain key. In addition, each key in the Solidities dictionary
        /// referenced from the MixingHints dictionary shall not also be present in the SpectralData dictionary within
        /// the same output intent.
        /// </summary>
        public DictionaryToken? MixingHints { get; }

        /// <summary>
        /// (Optional, PDF 2. 0) A dictionary where each key represents a colourant name as defined in 8.6.6.4,
        /// "Separation colour spaces" and where the value of each key shall be a stream whose contents shall represent
        /// CxF/ X-4 spot colour characterisation data that conform to ISO 17972-4. This stream shall contain exactly
        /// one SpotInkCharacterisation element whose SpotInkName matches the colourant name (see 7.3.5, "Name objects").
        /// In addition, this stream may contain zero or more further SpotInkCharacterisation elements, and/or other data.
        /// </summary>
        public DictionaryToken? SpectralData { get; }

        internal OutputIntent(string? name, string? outputCondition, string? outputConditionIdentifier,
            string? registryName, string? info, IIccProfile? destOutputProfile, IccProfileReference? destOutputProfileRef,
            DictionaryToken? mixingHints, DictionaryToken? spectralData)
        {
            Name = name;
            OutputCondition = outputCondition;
            OutputConditionIdentifier = outputConditionIdentifier;
            RegistryName = registryName;
            Info = info;
            DestOutputProfile = destOutputProfile;
            DestOutputProfileRef = destOutputProfileRef;
            MixingHints = mixingHints;
            SpectralData = spectralData;
        }

        /// <summary>
        /// The <c>/S</c> subtype of a PDF/X output intent (ISO 15930).
        /// </summary>
        public const string PdfXSubtype = "GTS_PDFX";

        /// <summary>
        /// The <c>/S</c> subtype of a PDF/A output intent (ISO 19005). Every PDF/A part uses this same value,
        /// not a per-part one.
        /// </summary>
        public const string PdfASubtype = "GTS_PDFA1";

        /// <summary>
        /// Which of the output intents a document declares characterizes the target output device, and so is
        /// the one a consumer should colour-manage device colours through (14.11.5, "Output intents").
        /// Returns <see langword="null"/> when none can: an empty or <see langword="null"/> list, or one in
        /// which no entry carries a <see cref="DestOutputProfile"/>.
        /// <para>
        /// 14.11.5 permits several output intents and does not say which wins, so this applies two rules:
        /// </para>
        /// <list type="number">
        /// <item><b>An embedded profile is required.</b> Only <see cref="DestOutputProfile"/> can drive a
        /// conversion - a <see cref="DestOutputProfileRef"/> names a profile this library does not resolve -
        /// so entries without one are not candidates at all. This outranks the subtype: a PDF/A entry
        /// carrying a profile beats a PDF/X entry that only references one.</item>
        /// <item><b>Then the subtype decides</b>, <c>GTS_PDFX</c> before <c>GTS_PDFA1</c> before anything
        /// else - an extension subtype, <c>ISO_PDFE1</c>, or a missing <c>/S</c>, none of which are excluded,
        /// only ranked last. PDF/X exists to pin down device colour, which is exactly the question being
        /// asked here, while PDF/A is about archiving and merely happens to carry a profile too.</item>
        /// </list>
        /// <para>
        /// Rule 2 is a default, not a policy: a caller with a subtype in mind passes
        /// <paramref name="preferredSubtype"/> and that one is ranked first instead. Rule 1 is not
        /// negotiable either way.
        /// </para>
        /// <para>
        /// Entries of equal rank keep the order the <c>/OutputIntents</c> array wrote them, so a file
        /// declaring one intent - which is all but every file - gets it. Nothing else about the array is
        /// reinterpreted: it is parsed unranked and in order, so a conformance check still sees exactly what
        /// the file declared and can apply its own rule instead of this one.
        /// </para>
        /// <para>
        /// A profile-less entry is deliberately <b>not</b> returned as a fallback. The result exists to be
        /// converted through, and handing back something that cannot be would only move the null check to
        /// the caller.
        /// </para>
        /// </summary>
        /// <param name="outputIntents">
        /// The output intents in effect, in array order - typically
        /// <see cref="Graphics.CurrentGraphicsState.OutputIntents"/>, where <see langword="null"/> and empty
        /// alike mean there is no output intent to honour.
        /// </param>
        /// <param name="preferredSubtype">
        /// (Optional) An <c>/S</c> subtype to rank ahead of all others - <c>GTS_PDFX</c>, <c>GTS_PDFA1</c>,
        /// <c>ISO_PDFE1</c> or an extension key - matched exactly, as the built-in subtypes are. For a caller
        /// asking something narrower than "which entry characterizes the output device", which is the only
        /// question the built-in order answers.
        /// <para>
        /// It reorders and never filters: when no entry carrying a profile declares it, selection falls back
        /// to the built-in order and returns what it would have without the preference. Rule 1 still stands
        /// above it, so a preferred entry without an embedded <see cref="DestOutputProfile"/> loses to one
        /// that has a profile, and entries of equal rank still keep array order.
        /// </para>
        /// <para><see langword="null"/>, the default, is the built-in order.</para>
        /// </param>
        public static OutputIntent? SelectForColorManagement(IReadOnlyList<OutputIntent>? outputIntents,
            string? preferredSubtype = null)
        {
            if (outputIntents is null)
            {
                return null;
            }

            // What a candidate can score at best, and therefore when the search can stop. A preference adds a
            // rank above PDF/X, so a PDF/X entry no longer ends it - a later entry may still be the preferred
            // one.
            int bestPossibleRank = preferredSubtype is null ? 0 : PreferredRank;

            OutputIntent? best = null;
            int bestRank = int.MaxValue;

            for (int i = 0; i < outputIntents.Count; i++)
            {
                var candidate = outputIntents[i];

                if (candidate?.DestOutputProfile is null)
                {
                    continue;
                }

                int rank = SubtypeRank(candidate.Name, preferredSubtype);

                // Strictly better, so entries of equal rank keep array order.
                if (rank < bestRank)
                {
                    best = candidate;
                    bestRank = rank;

                    if (rank == bestPossibleRank)
                    {
                        // Nothing outranks this, and a later one of equal rank would lose on array order.
                        break;
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// The rank of the caller's preferred subtype, above every built-in one.
        /// </summary>
        private const int PreferredRank = -1;

        /// <summary>
        /// The preference order of an output intent subtype, lower being preferred. See
        /// <see cref="SelectForColorManagement"/> for why.
        /// </summary>
        private static int SubtypeRank(string? subtype, string? preferredSubtype)
        {
            // Ahead of the built-in order, and checked first so that preferring PDF/A really does beat PDF/X.
            if (preferredSubtype is not null && string.Equals(subtype, preferredSubtype, StringComparison.Ordinal))
            {
                return PreferredRank;
            }

            if (string.Equals(subtype, PdfXSubtype, StringComparison.Ordinal))
            {
                return 0;
            }

            if (string.Equals(subtype, PdfASubtype, StringComparison.Ordinal))
            {
                return 1;
            }

            return 2;
        }
    }
}
