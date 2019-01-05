namespace UglyToad.PdfPig.AcroForms.Fields
{
    using Tokens;

    internal class AcroSignatureField : AcroFieldBase
    {
        public AcroSignatureField(DictionaryToken dictionary, string fieldType, uint fieldFlags, AcroFieldCommonInformation information) : 
            base(dictionary, fieldType, fieldFlags, information)
        {
        }
    }
}