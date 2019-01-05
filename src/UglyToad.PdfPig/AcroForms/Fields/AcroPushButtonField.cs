namespace UglyToad.PdfPig.AcroForms.Fields
{
    using Tokens;

    internal class AcroPushButtonField : AcroFieldBase
    {
        public AcroButtonFieldFlags Flags { get; }

        public AcroPushButtonField(DictionaryToken dictionary, string fieldType, AcroButtonFieldFlags fieldFlags, 
            AcroFieldCommonInformation information) : 
            base(dictionary, fieldType, (uint)fieldFlags, information)
        {
            Flags = fieldFlags;
        }
    }
}