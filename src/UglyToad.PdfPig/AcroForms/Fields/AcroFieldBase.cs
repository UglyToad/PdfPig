namespace UglyToad.PdfPig.AcroForms.Fields
{
    using System;
    using Tokens;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// A field in an interactive <see cref="AcroForm"/>.
    /// </summary>
    public abstract class AcroFieldBase
    {
        /// <summary>
        /// The raw PDF dictionary for this field.
        /// </summary>
        [NotNull]
        public DictionaryToken Dictionary { get; }

        /// <summary>
        /// The <see cref="string"/> representing the type of this field in PDF format.
        /// </summary>
        [NotNull]
        public string RawFieldType { get; }

        /// <summary>
        /// The actual <see cref="AcroFieldType"/> represented by this field.
        /// </summary>
        public AcroFieldType FieldType { get; }

        /// <summary>
        /// Specifies various characteristics of the field.
        /// </summary>
        public uint FieldFlags { get; }

        /// <summary>
        /// The optional information common to all types of field.
        /// </summary>
        [NotNull]
        public AcroFieldCommonInformation Information { get; }

        /// <summary>
        /// Create a new <see cref="AcroFieldBase"/>.
        /// </summary>
        /// <param name="dictionary">The dictionary for this field.</param>
        /// <param name="rawFieldType">The PDF string type of this field.</param>
        /// <param name="fieldFlags">The flags specifying behaviour for this field.</param>
        /// <param name="fieldType">The type of this field.</param>
        /// <param name="information">Additional information for this field.</param>
        protected AcroFieldBase(DictionaryToken dictionary, string rawFieldType,
            uint fieldFlags, 
            AcroFieldType fieldType,
            AcroFieldCommonInformation information)
        {
            Dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            RawFieldType = rawFieldType ?? throw new ArgumentNullException(nameof(rawFieldType));
            FieldFlags = fieldFlags;
            FieldType = fieldType;
            Information = information ?? new AcroFieldCommonInformation(null, null, null, null);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{FieldType}";
        }
    }
}
