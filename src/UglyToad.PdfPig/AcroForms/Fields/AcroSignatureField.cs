namespace UglyToad.PdfPig.AcroForms.Fields
{
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    /// A digital signature field.
    /// </summary>
    public class AcroSignatureField : AcroFieldBase
    {
        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="T:UglyToad.PdfPig.AcroForms.Fields.AcroSignatureField" />.
        /// </summary>
        public AcroSignatureField(DictionaryToken dictionary, string fieldType, uint fieldFlags, AcroFieldCommonInformation information) : 
            base(dictionary, fieldType, fieldFlags, AcroFieldType.Signature, information)
        {
        }
    }
}