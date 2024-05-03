namespace UglyToad.PdfPig.AcroForms.Fields
{
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    /// A set of radio buttons.
    /// </summary>
    public class AcroRadioButtonsField : AcroNonTerminalField
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
            AcroFieldCommonInformation information,
            IReadOnlyList<AcroFieldBase> children) : 
            base(dictionary, fieldType, (uint)fieldFlags, information, AcroFieldType.RadioButtons, children)
        {
            Flags = fieldFlags;
        }
    }
}