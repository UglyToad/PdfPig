namespace UglyToad.PdfPig.AcroForms.Fields
{
    /// <summary>
    /// Flags specifying various characteristics of a choice type field in an <see cref="AcroFieldBase"/>.
    /// </summary>
    [Flags]
    public enum AcroChoiceFieldFlags : uint
    {
        /// <summary>
        /// The user may not change the value of the field.
        /// </summary>
        ReadOnly = 1 << 0,
        /// <summary>
        /// The field must have a value before the form can be submitted.
        /// </summary>
        Required = 1 << 1,
        /// <summary>
        /// Must not be exported by the submit form action.
        /// </summary>
        NoExport = 1 << 2,
        /// <summary>
        /// The field is a combo box.
        /// </summary>
        Combo = 1 << 17,
        /// <summary>
        /// The combo box includes an editable text box. <see cref="Combo"/> must be set.
        /// </summary>
        Edit = 1 << 18,
        /// <summary>
        /// The options should be sorted alphabetically, this should be ignored by viewer applications.
        /// </summary>
        Sort = 1 << 19,
        /// <summary>
        /// The field allows multiple options to be selected.
        /// </summary>
        MultiSelect = 1 << 21,
        /// <summary>
        /// The text entered in the field is not spell checked. <see cref="Combo"/> and <see cref="Edit"/> must be set.
        /// </summary>
        DoNotSpellCheck = 1 << 22,
        /// <summary>
        /// Any associated field action is fired when the selection is changed rather than on losing focus.
        /// </summary>
        CommitOnSelectionChange = 1 << 26
    }
}