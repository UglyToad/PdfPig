namespace UglyToad.PdfPig.Annotations
{
    using System;

    /// <summary>
    /// Specifies characteristics of an annotation in a PDF or FDF document.
    /// </summary>
    [Flags]
    public enum AnnotationFlags
    {
        /// <summary>
        /// Do not display the annotation if it is not one of the standard annotation types.
        /// </summary>
        Invisible = 1 << 0,
        /// <summary>
        /// Do not display or print the annotation irrespective of type. Do not allow interaction.
        /// </summary>
        Hidden = 1 << 1,
        /// <summary>
        /// The annotation should be included when the document is physically printed.
        /// </summary>
        Print = 1 << 2,
        /// <summary>
        /// Do not zoom/scale the annotation as the zoom of the document is changed.
        /// </summary>
        NoZoom = 1 << 3,
        /// <summary>
        /// Do not rotate the annotation as the page is rotated.
        /// </summary>
        NoRotate = 1 << 4,
        /// <summary>
        /// Do not display the annotation in viewer applications as with <see cref="Hidden"/>, however allow the annotation to be printed if <see cref="Print"/> is set.
        /// </summary>
        NoView = 1 << 5,
        /// <summary>
        /// Allow the annotation to be displayed/printed if applicable but do not respond to user interaction, e.g. mouse clicks.
        /// </summary>
        ReadOnly = 1 << 6,
        /// <summary>
        /// Do not allow deleting the annotation or changing size/position but allow the contents to be modified.
        /// </summary>
        Locked = 1 << 7,
        /// <summary>
        /// Invert the meaning of the <see cref="NoView"/> flag.
        /// </summary>
        ToggleNoView = 1 << 8,
        /// <summary>
        /// Allow the annotation to be deleted, resized, moved or restyled but disallow changes to the annotation contents. Opposite to <see cref="Locked"/>.
        /// </summary>
        LockedContents = 1 << 9
    }
}