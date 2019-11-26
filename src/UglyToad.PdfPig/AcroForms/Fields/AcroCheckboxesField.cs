namespace UglyToad.PdfPig.AcroForms.Fields
{
    using System.Collections.Generic;
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    /// A set of related checkboxes.
    /// </summary>
    public class AcroCheckboxesField : AcroNonTerminalField
    {
        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="AcroCheckboxesField"/>.
        /// </summary>
        internal AcroCheckboxesField(DictionaryToken dictionary, string fieldType, AcroButtonFieldFlags fieldFlags,
            AcroFieldCommonInformation information,
            IReadOnlyList<AcroFieldBase> children) : 
            base(dictionary, fieldType, (uint)fieldFlags, information, 
                AcroFieldType.Checkboxes, children)
        {
        }
    }
}