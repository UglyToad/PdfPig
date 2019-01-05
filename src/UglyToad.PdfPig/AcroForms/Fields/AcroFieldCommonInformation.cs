namespace UglyToad.PdfPig.AcroForms.Fields
{
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Information from the field dictionary which is common across all field types.
    /// All of this information is optional.
    /// </summary>
    public class AcroFieldCommonInformation
    {
        /// <summary>
        /// The reference to the field which is the parent of this one, if applicable.
        /// </summary>
        public IndirectReference? Parent { get; set; }

        /// <summary>
        /// The partial field name for this field. The fully qualified field name is the
        /// period '.' joined name of all parents' partial names and this field's partial name.
        /// </summary>
        [CanBeNull]
        public string PartialName { get; }

        /// <summary>
        /// The alternate field name to be used instead of the fully qualified field name where
        /// the field is being identified on the user interface or by screen readers.
        /// </summary>
        [CanBeNull]
        public string AlternateName { get; }

        /// <summary>
        /// The mapping name used when exporting form field data from the document.
        /// </summary>
        [CanBeNull]
        public string MappingName { get; }

        /// <summary>
        /// Create a new <see cref="AcroFieldCommonInformation"/>.
        /// </summary>
        public AcroFieldCommonInformation(IndirectReference? parent, string partialName, string alternateName, string mappingName)
        {
            Parent = parent;
            PartialName = partialName;
            AlternateName = alternateName;
            MappingName = mappingName;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Parent: {Parent}. Partial: {PartialName}. Alternate: {AlternateName}. Mapping: {MappingName}.";
        }
    }
}