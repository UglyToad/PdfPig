namespace UglyToad.PdfPig.AcroForms.Fields
{
    using Geometry;
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

        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="AcroRadioButtonField"/>.
        /// </summary>
        public AcroRadioButtonField(DictionaryToken dictionary, string fieldType, AcroButtonFieldFlags fieldFlags,
            AcroFieldCommonInformation information,
            int? pageNumber,
            PdfRectangle? bounds) :
            base(dictionary, fieldType, (uint)fieldFlags, AcroFieldType.RadioButton, information, pageNumber, bounds)
        {
            Flags = fieldFlags;
        }
    }
}