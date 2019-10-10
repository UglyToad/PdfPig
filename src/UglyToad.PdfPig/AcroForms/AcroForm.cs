namespace UglyToad.PdfPig.AcroForms
{
    using System;
    using System.Collections.Generic;
    using Fields;
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
        /// All root fields in this form with their corresponding references.
        /// </summary>
        public IReadOnlyDictionary<IndirectReference, AcroFieldBase> Fields { get; }

        /// <summary>
        /// Create a new <see cref="AcroForm"/>.
        /// </summary>
        public AcroForm(DictionaryToken dictionary, SignatureFlags signatureFlags, bool needAppearances, 
            IReadOnlyDictionary<IndirectReference, AcroFieldBase> fields)
        {
            Dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            SignatureFlags = signatureFlags;
            NeedAppearances = needAppearances;
            Fields = fields ?? throw new ArgumentNullException(nameof(fields));
        }

        /// <summary>
        /// Get the set of fields which appear on the given page number.
        /// </summary>
        public IEnumerable<AcroFieldBase> GetFieldsForPage(int pageNumber)
        {
            if (pageNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber), $"Page number starts at 1, instead got {pageNumber}.");
            }

            foreach (var field in Fields)
            {
                if (field.Value.PageNumber == pageNumber)
                {
                    yield return field.Value;
                }
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Dictionary.ToString();
        }
    }
}

