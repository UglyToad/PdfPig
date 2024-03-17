namespace UglyToad.PdfPig.AcroForms
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Fields;
    using Tokens;

    /// <summary>
    /// A collection of interactive fields for gathering data from a user through dropdowns, textboxes, checkboxes, etc.
    /// Each <see cref="PdfDocument"/> with form functionality contains a single <see cref="AcroForm"/> spread across one or more pages.
    /// </summary>
    /// <remarks>
    /// The name AcroForm distinguishes this from the other form type called form XObjects which act as templates for repeated sections of content.
    /// </remarks>
    public class AcroForm
    {
        private readonly IReadOnlyDictionary<IndirectReference, AcroFieldBase> fieldsWithReferences;

        /// <summary>
        /// The raw PDF dictionary which is the root form object.
        /// </summary>
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
        /// All root fields in this form.
        /// </summary>
        public IReadOnlyList<AcroFieldBase> Fields { get; }

        /// <summary>
        /// Create a new <see cref="AcroForm"/>.
        /// </summary>
        internal AcroForm(DictionaryToken dictionary, SignatureFlags signatureFlags, bool needAppearances,
            IReadOnlyDictionary<IndirectReference, AcroFieldBase> fieldsWithReferences)
        {
            Dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            SignatureFlags = signatureFlags;
            NeedAppearances = needAppearances;
            this.fieldsWithReferences = fieldsWithReferences ?? throw new ArgumentNullException(nameof(fieldsWithReferences));
            Fields = fieldsWithReferences.Values.ToList();
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
                if (field.PageNumber == pageNumber)
                {
                    yield return field;
                }
                else if (field is AcroNonTerminalField parent
                && parent.Children.Any(x => x.PageNumber == pageNumber))
                {
                    yield return field;
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

