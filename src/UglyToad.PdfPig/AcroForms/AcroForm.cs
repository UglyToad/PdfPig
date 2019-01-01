namespace UglyToad.PdfPig.AcroForms
{
    using System;
    using Tokens;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// A collection of interactive fields for gathering data from a user through dropdowns, textboxes, checkboxes, etc.
    /// Each <see cref="PdfDocument"/> with form functionality contains a single <see cref="AcroForm"/> spread across one or more pages.
    /// </summary>
    /// <remarks>
    /// The name AcroForm distinguishes this from the other form type called form XObjects which act as templates for repeated sections of content.
    /// </remarks>
    internal class AcroForm
    {
        /// <summary>
        /// The raw PDF dictionary which is the root form object.
        /// </summary>
        [NotNull]
        public DictionaryToken Dictionary { get; }

        /// <summary>
        /// Document-level characteristics related to signature fields.
        /// </summary>
        public SignatureFlags SignatureFlags { get; }

        /// <summary>
        /// Whether all widget annotations need appearance dictionaries and streams.
        /// </summary>
        public bool NeedAppearances { get; }

        /// <summary>
        /// Create a new <see cref="AcroForm"/>.
        /// </summary>
        public AcroForm(DictionaryToken dictionary, SignatureFlags signatureFlags, bool needAppearances)
        {
            Dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            SignatureFlags = signatureFlags;
            NeedAppearances = needAppearances;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Dictionary.ToString();
        }
    }
}

