namespace UglyToad.PdfPig.Annotations
{
    /// <summary>
    /// The standard annotation types in PDF documents.
    /// </summary>
    public enum AnnotationType
    {
        /// <summary>
        /// A 'sticky note' style annotation displaying some text with open/closed pop-up state.
        /// </summary>
        Text = 0,
        /// <summary>
        /// A link to elsewhere in the document or an external application/web link.
        /// </summary>
        Link = 1,
        /// <summary>
        /// Displays text on the page. Unlike <see cref="Text"/> there is no associated pop-up.
        /// </summary>
        FreeText = 2,
        /// <summary>
        /// Display a single straight line on the page with optional line ending styles.
        /// </summary>
        Line = 3,
        /// <summary>
        /// Display a rectangle on the page.
        /// </summary>
        Square = 4,
        /// <summary>
        /// Display an ellipse on the page.
        /// </summary>
        Circle = 5,
        /// <summary>
        /// Display a closed polygon on the page.
        /// </summary>
        Polygon = 6,
        /// <summary>
        /// Display a set of connected lines on the page which is not a closed polygon.
        /// </summary>
        PolyLine = 7,
        /// <summary>
        /// A highlight for text or content with associated annotation texyt.
        /// </summary>
        Highlight = 8,
        /// <summary>
        /// An underline under text with associated annotation text.
        /// </summary>
        Underline = 9,
        /// <summary>
        /// A jagged squiggly line under text with associated annotation text.
        /// </summary>
        Squiggly = 10,
        /// <summary>
        /// A strikeout through some text with associated annotation text.
        /// </summary>
        StrikeOut = 11,
        /// <summary>
        /// Text or graphics intended to display as if inserted by a rubber stamp.
        /// </summary>
        Stamp = 12,
        /// <summary>
        /// A visual symbol indicating the presence of text edits.
        /// </summary>
        Caret = 13,
        /// <summary>
        /// A freehand 'scribble' formed by one or more paths.
        /// </summary>
        Ink = 14,
        /// <summary>
        /// Displays text in a pop-up window for entry or editing.
        /// </summary>
        Popup = 15,
        /// <summary>
        /// A file.
        /// </summary>
        FileAttachment = 16,
        /// <summary>
        /// A sound to be played through speakers.
        /// </summary>
        Sound = 17,
        /// <summary>
        /// Embeds a movie from a file in a PDF document.
        /// </summary>
        Movie = 18,
        /// <summary>
        /// Used by interactive forms to represent field appearance and manage user interactions.
        /// </summary>
        Widget = 19,
        /// <summary>
        /// Specifies a page region for media clips to be played and actions to be triggered from.
        /// </summary>
        Screen = 20,
        /// <summary>
        /// Represents a symbol used during the physical printing process to maintain output quality, e.g. color bars or cut marks.
        /// </summary>
        PrinterMark = 21,
        /// <summary>
        /// Used during the physical printing process to prevent colors mixing.
        /// </summary>
        TrapNet = 22,
        /// <summary>
        /// Adds a watermark at a fixed size and position irrespective of page size.
        /// </summary>
        Watermark = 23,
        /// <summary>
        /// Represents a 3D model/artwork, for example from CAD, in a PDF document.
        /// </summary>
        Artwork3D = 24,
        /// <summary>
        /// A custom annotation type.
        /// </summary>
        Other = 25
    }
}