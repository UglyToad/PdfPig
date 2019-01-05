namespace UglyToad.PdfPig.AcroForms.Fields
{
    using Tokens;

    internal class AcroRadioButtonsField : AcroFieldBase
    {
        public AcroButtonFieldFlags Flags { get; }

        public AcroRadioButtonsField(DictionaryToken dictionary, string fieldType, AcroButtonFieldFlags fieldFlags, 
            AcroFieldCommonInformation information) : 
            base(dictionary, fieldType, (uint)fieldFlags, information)
        {
            Flags = fieldFlags;
        }
    }
}