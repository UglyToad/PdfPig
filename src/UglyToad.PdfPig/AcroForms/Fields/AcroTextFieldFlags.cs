namespace UglyToad.PdfPig.AcroForms.Fields
{
    using System;

    /// <summary>
    /// Flags specifying various characteristics of a text type field in an <see cref="AcroFieldBase"/>.
    /// </summary>
    [Flags]
    public enum AcroTextFieldFlags : uint
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
        /// The field can contain multiple lines of text.
        /// </summary>
        Multiline = 1 << 12,
        /// <summary>
        /// The field is for a password and should not be displayed as text and should not be stored to file.
        /// </summary>
        Password = 1 << 13,
        /// <summary>
        /// The field represents a file path selection.
        /// </summary>
        FileSelect = 1 << 20,
        /// <summary>
        /// The text entered is not spell checked.
        /// </summary>
        DoNotSpellCheck = 1 << 22,
        /// <summary>
        /// The field does not scroll if the text exceeds the bounds of the field.
        /// </summary>
        DoNotScroll = 1 << 23,
        /// <summary>
        /// For a text field which is not a <see cref="Password"/>, <see cref="Multiline"/> or <see cref="FileSelect"/>
        /// the field text is evenly spaced by splitting into 'combs' based on the MaxLen entry in the field dictionary.
        /// </summary>
        Comb = 1 << 24,
        /// <summary>
        /// The value of the field is a rich text string.
        /// </summary>
        RichText = 1 << 25
    }
}