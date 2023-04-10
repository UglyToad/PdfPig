namespace UglyToad.PdfPig.Actions;

/// <summary>
/// Action types (PDF reference 8.5.3)
/// </summary>
public enum ActionType
{
    /// <summary>
    /// Go to a destination in the current document.
    /// </summary>
    GoTo,
    /// <summary>
    /// (“Go-to remote”) Go to a destination in another document.
    /// </summary>
    GoToR,
    /// <summary>
    /// (“Go-to embedded”; PDF 1.6) Go to a destination in an embedded file.
    /// </summary>
    GoToE,
    /// <summary>
    /// Launch an application, usually to open a file.
    /// </summary>
    Launch,
    /// <summary>
    /// Begin reading an article thread.
    /// </summary>
    Thread,
    /// <summary>
    /// Resolve a uniform resource identifier.
    /// </summary>
    URI,
    /// <summary>
    /// (PDF 1.2) Play a sound.
    /// </summary>
    Sound,
    /// <summary>
    /// (PDF 1.2) Play a movie.
    /// </summary>
    Movie,
    /// <summary>
    /// (PDF 1.2) Set an annotation’s Hidden flag.
    /// </summary>
    Hide,
    /// <summary>
    /// (PDF 1.2) Execute an action predefined by the viewer application.
    /// </summary>
    Named,
    /// <summary>
    /// (PDF 1.2) Send data to a uniform resource locator.
    /// </summary>
    SubmitForm,
    /// <summary>
    /// (PDF 1.2) Set fields to their default values.
    /// </summary>
    ResetForm,
    /// <summary>
    /// (PDF 1.2) Import field values from a file.
    /// </summary>
    ImportData,
    /// <summary>
    /// (PDF 1.3) Execute a JavaScript script.
    /// </summary>
    JavaScript,
    /// <summary>
    /// (PDF 1.5) Set the states of optional content groups.
    /// </summary>
    SetOCGState,
    /// <summary>
    /// (PDF 1.5) Controls the playing of multimedia content.
    /// </summary>
    Rendition,
    /// <summary>
    /// (PDF 1.5) Updates the display of a document, using a transition dictionary.
    /// </summary>
    Trans,
    /// <summary>
    /// (PDF 1.6) Set the current view of a 3D annotation
    /// </summary>
    GoTo3DView
}