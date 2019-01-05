namespace UglyToad.PdfPig.AcroForms.Fields
{
    using Tokens;

    internal class AcroCheckboxField : AcroFieldBase
    {
        public AcroButtonFieldFlags Flags { get; }

        public NameToken CurrentValue { get; }

        public AcroCheckboxField(DictionaryToken dictionary, string fieldType, AcroButtonFieldFlags fieldFlags,
            AcroFieldCommonInformation information, NameToken currentValue) :
            base(dictionary, fieldType, (uint)fieldFlags, information)
        {
            Flags = fieldFlags;
            CurrentValue = currentValue;
        }
    }
}