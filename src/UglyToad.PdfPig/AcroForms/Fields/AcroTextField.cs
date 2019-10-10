namespace UglyToad.PdfPig.AcroForms.Fields
{
    using Geometry;
    using Tokens;

    /// <inheritdoc />
    /// <summary>
    /// A text field is a box or space in which the user can enter text from the keyboard.
    /// The text may be restricted to a single line or may be permitted to span multiple lines.
    /// </summary>
    public class AcroTextField : AcroFieldBase
    {
        /// <summary>
        /// The flags specifying the behaviour of this field.
        /// </summary>
        public AcroTextFieldFlags Flags { get; }

        /// <summary>
        /// The value of the text in this text field.
        /// This can be <see langword="null"/> if no value has been set.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// The optional maximum length of the text field.
        /// </summary>
        public int? MaxLength { get; }

        /// <summary>
        /// Whether the field supports rich text content.
        /// </summary>
        public bool IsRichText { get; }

        /// <summary>
        /// Whether the field allows multiline text.
        /// </summary>
        public bool IsMultiline { get;}

        /// <inheritdoc />
        /// <summary>
        /// Create a new <see cref="T:UglyToad.PdfPig.AcroForms.Fields.AcroTextField" />.
        /// </summary>
        /// <param name="dictionary">The dictionary for this field.</param>
        /// <param name="fieldType">The type of this field, must be <see cref="F:UglyToad.PdfPig.Tokens.NameToken.Ch" />.</param>
        /// <param name="fieldFlags">The flags specifying behaviour for this field.</param>
        /// <param name="information">Additional information for this field.</param>
        /// <param name="value">The text value.</param>
        /// <param name="maxLength">The maximum length.</param>
        /// <param name="pageNumber">The number of the page this field appears on.</param>
        /// <param name="bounds">The location of this field on the page.</param>
        public AcroTextField(DictionaryToken dictionary, string fieldType, AcroTextFieldFlags fieldFlags,
            AcroFieldCommonInformation information, 
            string value,
            int? maxLength,
            int? pageNumber,
            PdfRectangle? bounds) : 
            base(dictionary, fieldType, (uint)fieldFlags, AcroFieldType.Text, information, pageNumber, bounds)
        {
            Flags = fieldFlags;
            Value = value;
            MaxLength = maxLength;
            IsRichText = Flags.HasFlag(AcroTextFieldFlags.RichText);
            IsMultiline = Flags.HasFlag(AcroTextFieldFlags.Multiline);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{FieldType}: {Value ?? string.Empty}";
        }
    }
}