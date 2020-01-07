namespace UglyToad.PdfPig.AcroForms.Fields
{
    using Core;
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    /// A single radio button.
    /// </summary>
    public class AcroRadioButtonField : AcroFieldBase
    {
        /// <summary>
        /// The <see cref="AcroButtonFieldFlags"/> which define the behaviour of this button type.
        /// </summary>
        public AcroButtonFieldFlags Flags { get; }

        /// <summary>
        /// The current value of this radio button.
        /// </summary>
        public NameToken CurrentValue { get; }

        /// <summary>
        /// Whether the radio button is currently on/active.
        /// </summary>
        public bool IsSelected { get; }
        
        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="AcroRadioButtonField"/>.
        /// </summary>
        public AcroRadioButtonField(DictionaryToken dictionary, string fieldType, AcroButtonFieldFlags fieldFlags,
            AcroFieldCommonInformation information,
            int? pageNumber,
            PdfRectangle? bounds,
            NameToken currentValue,
            bool isSelected) :
            base(dictionary, fieldType, (uint)fieldFlags, AcroFieldType.RadioButton, information, pageNumber, bounds)
        {
            Flags = fieldFlags;
            CurrentValue = currentValue;
            IsSelected = isSelected;
        }
    }
}