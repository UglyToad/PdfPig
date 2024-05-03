namespace UglyToad.PdfPig.AcroForms.Fields
{
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    /// A non-leaf field in the form's structure.
    /// </summary>
    public class AcroNonTerminalField : AcroFieldBase
    {
        /// <summary>
        /// The child fields of this field.
        /// </summary>
        public IReadOnlyList<AcroFieldBase> Children { get; }

        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="AcroNonTerminalField"/>.
        /// </summary>
        internal AcroNonTerminalField(DictionaryToken dictionary, string fieldType, uint fieldFlags, 
            AcroFieldCommonInformation information,
            AcroFieldType acroFieldType,
            IReadOnlyList<AcroFieldBase> children) : 
            base(dictionary, fieldType, fieldFlags, acroFieldType, information,
                null, null)
        {
            Children = children ?? throw new ArgumentNullException(nameof(children));
        }
    }
}