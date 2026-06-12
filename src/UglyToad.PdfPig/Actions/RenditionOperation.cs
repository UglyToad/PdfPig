namespace UglyToad.PdfPig.Actions
{
    /// <summary>
    /// The operation a <see cref="RenditionAction"/> performs on the rendition associated with its
    /// target screen annotation. Corresponds to the <c>OP</c> entry of the rendition action dictionary.
    /// </summary>
    public enum RenditionOperation : byte
    {
        /// <summary>
        /// (<c>OP</c> 0) Associate the rendition with the annotation and start playing it. If a rendition
        /// is already associated, resume a paused one or restart a stopped one.
        /// </summary>
        PlayAndAssociate = 0,
        /// <summary>
        /// (<c>OP</c> 1) Stop any content associated with the annotation and remove the association.
        /// </summary>
        Stop = 1,
        /// <summary>
        /// (<c>OP</c> 2) Pause any content associated with the annotation.
        /// </summary>
        Pause = 2,
        /// <summary>
        /// (<c>OP</c> 3) Resume any content associated with the annotation that is currently paused.
        /// </summary>
        Resume = 3,
        /// <summary>
        /// (<c>OP</c> 4) Play any content associated with the annotation, using the existing association.
        /// </summary>
        Play = 4
    }
}
