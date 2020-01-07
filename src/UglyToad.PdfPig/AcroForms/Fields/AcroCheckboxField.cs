namespace UglyToad.PdfPig.AcroForms.Fields
{
    using Core;
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    /// A checkbox which may be toggled on or off.
    /// </summary>
    public class AcroCheckboxField : AcroFieldBase
    {
        /// <summary>
        /// The <see cref="AcroButtonFieldFlags"/> which define the behaviour of this button type.
        /// </summary>
        public AcroButtonFieldFlags Flags { get; }

        /// <summary>
        /// The current value of this checkbox.
        /// </summary>
        public NameToken CurrentValue { get; }

        /// <summary>
        /// Whether this checkbox is currently checked/on.
        /// </summary>
        public bool IsChecked { get; }

        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="AcroCheckboxField"/>.
        /// </summary>
        public AcroCheckboxField(DictionaryToken dictionary, string fieldType, AcroButtonFieldFlags fieldFlags,
            AcroFieldCommonInformation information, NameToken currentValue,
            bool isChecked,
            int? pageNumber,
            PdfRectangle? bounds) :
            base(dictionary, fieldType, (uint)fieldFlags, AcroFieldType.Checkbox, information,
                pageNumber, bounds)
        {
            Flags = fieldFlags;
            CurrentValue = currentValue;
            IsChecked = isChecked;
        }
    }
}