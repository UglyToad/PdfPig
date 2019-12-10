namespace UglyToad.PdfPig.AcroForms.Fields
{
    /// <summary>
    /// Indicates the type of field for a <see cref="AcroFieldBase"/>.
    /// </summary>
    public enum AcroFieldType
    {
        /// <summary>
        /// A button that immediately to user input without retaining state.
        /// </summary>
        PushButton,
        /// <summary>
        /// A set of checkboxes.
        /// </summary>
        Checkboxes,
        /// <summary>
        /// A checkbox which toggles between on and off states.
        /// </summary>
        Checkbox,
        /// <summary>
        /// A set of radio buttons.
        /// </summary>
        RadioButtons,
        /// <summary>
        /// A single radio button, as part of a set or on its own.
        /// </summary>
        RadioButton,
        /// <summary>
        /// A textbox allowing user input through the keyboard.
        /// </summary>
        Text,
        /// <summary>
        /// A dropdown list of options with optional user-editable textbox.
        /// </summary>
        ComboBox,
        /// <summary>
        /// A list of options for the user to select from.
        /// </summary>
        ListBox,
        /// <summary>
        /// A field containing a digital signature.
        /// </summary>
        Signature,
        /// <summary>
        /// The field type wasn't specified.
        /// </summary>
        Unknown
    }
}
