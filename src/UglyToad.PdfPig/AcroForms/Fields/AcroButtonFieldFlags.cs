namespace UglyToad.PdfPig.AcroForms.Fields
{
    using System;

    /// <summary>
    /// Flags specifying various characteristics of a button type field in an <see cref="AcroFieldBase"/>.
    /// </summary>
    [Flags]
    public enum AcroButtonFieldFlags : uint
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
        /// For radio buttons, one radio button must be set at all times.
        /// </summary>
        NoToggleToOff = 1 << 14,
        /// <summary>
        /// The field is a set of radio buttons.
        /// </summary>
        Radio = 1 << 15,
        /// <summary>
        /// The field is a push button.
        /// </summary>
        PushButton = 1 << 16,
        /// <summary>
        /// For radio buttons a group of radio buttons will toggle on/off at the same time based on their initial value.
        /// </summary>
        RadiosInUnison = 1 << 25
    }
}