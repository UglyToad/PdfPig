namespace UglyToad.PdfPig.AcroForms.Fields
{
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    /// A set of radio buttons.
    /// </summary>
    public class AcroRadioButtonsField : AcroFieldBase
    {
        /// <summary>
        /// The <see cref="AcroButtonFieldFlags"/> which define the behaviour of this button type.
        /// </summary>
        public AcroButtonFieldFlags Flags { get; }

        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="AcroRadioButtonsField"/>.
        /// </summary>
        public AcroRadioButtonsField(DictionaryToken dictionary, string fieldType, AcroButtonFieldFlags fieldFlags, 
            AcroFieldCommonInformation information) : 
            base(dictionary, fieldType, (uint)fieldFlags, AcroFieldType.RadioButton, information)
        {
            Flags = fieldFlags;
        }
    }
}