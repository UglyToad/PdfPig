namespace UglyToad.PdfPig.AcroForms.Fields
{
    using Geometry;
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    /// A push button responds immediately to user input without storing any state.
    /// </summary>
    public class AcroPushButtonField : AcroFieldBase
    {
        /// <summary>
        /// The <see cref="AcroButtonFieldFlags"/> which define the behaviour of this button type.
        /// </summary>
        public AcroButtonFieldFlags Flags { get; }

        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="AcroPushButtonField"/>.
        /// </summary>
        public AcroPushButtonField(DictionaryToken dictionary, string fieldType, 
            AcroButtonFieldFlags fieldFlags, 
            AcroFieldCommonInformation information,
            int? pageNumber,
            PdfRectangle? bounds) : 
            base(dictionary, fieldType, (uint)fieldFlags, AcroFieldType.PushButton, information, pageNumber, bounds)
        {
            Flags = fieldFlags;
        }
    }
}