namespace UglyToad.PdfPig.AcroForms.Fields
{
    /// <summary>
    /// An option in a choice field, either <see cref="AcroComboBoxField"/> or <see cref="AcroListBoxField"/>.
    /// </summary>
    public class AcroChoiceOption
    {
        /// <summary>
        /// The index of this option in the array.
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Whether this option is selected.
        /// </summary>
        public bool IsSelected { get; }

        /// <summary>
        /// The text of the option.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The value of the option when the form is exported.
        /// </summary>
        public string ExportValue { get; }

        /// <summary>
        /// Whether the field defined an export value for this option.
        /// </summary>
        public bool HasExportValue { get; }

        /// <summary>
        /// Create a new <see cref="AcroChoiceOption"/>.
        /// </summary>
        public AcroChoiceOption(int index, bool isSelected, string name, string exportValue = null)
        {
            Index = index;
            IsSelected = isSelected;
            Name = name;
            ExportValue = exportValue;
            HasExportValue = exportValue != null;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Index}: {Name} ({IsSelected}).";
        }
    }
}