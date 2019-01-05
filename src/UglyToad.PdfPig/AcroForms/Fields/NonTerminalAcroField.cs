namespace UglyToad.PdfPig.AcroForms.Fields
{
    using System;
    using System.Collections.Generic;
    using Tokens;

    /// <summary>
    /// A non-leaf field in the form's structure.
    /// </summary>
    internal class NonTerminalAcroField : AcroFieldBase
    {
        public IReadOnlyList<AcroFieldBase> Children { get; }

        public NonTerminalAcroField(DictionaryToken dictionary, string fieldType, uint fieldFlags, AcroFieldCommonInformation information,
            IReadOnlyList<AcroFieldBase> children) : 
            base(dictionary, fieldType, fieldFlags, information)
        {
            Children = children ?? throw new ArgumentNullException(nameof(children));
        }
    }
}