namespace UglyToad.PdfPig.Graphics.Colors.Icc
{
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
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// (Optional) A text string concisely identifying the intended output device or production condition in
        /// human-readable form. This is the preferred method of defining such a string for presentation to the user.
        /// </summary>
        public string? OutputCondition { get; }

        /// <summary>
        /// (Required) A text string identifying the intended output device or production condition in human- or
        /// machine-readable form. If human-readable, this string may be used in lieu of an OutputCondition string
        /// for presentation to the user.
        /// <para>
        /// A typical value for this entry may be the name of a production condition
        /// maintained in an industry-standard registry such as the ICC Characterization Data Registry. If the
        /// designated condition matches that in effect at production time, the production software is responsible
        /// for providing the corresponding ICC profile as defined in the registry.
        /// </para>
        /// If the intended production
        /// condition is not a recognised standard, the value of this entry may be Custom or an application-specific,
        /// machine-readable name. The DestOutputProfile entry defines the ICC profile, and the Info entry shall be
        /// used for further human-readable identification. 
        /// </summary>
        public string OutputConditionIdentifier { get; }

        /// <summary>
        /// (Optional) A text string (conventionally a uniform resource identifier, or URI) identifying the registry
        /// in which the condition designated by OutputConditionIdentifier is defined. 
        /// </summary>
        public string RegistryName { get; }

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

        internal OutputIntent(string name, string? outputCondition, string outputConditionIdentifier,
            string registryName, string? info, IIccProfile? destOutputProfile, IccProfileReference? destOutputProfileRef,
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
    }
}
