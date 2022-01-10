namespace UglyToad.PdfPig.Encryption
{
    using System;

    [Flags]
    internal enum UserAccessPermissions : long
    {
        /// <summary>
        /// (Revision 2) Print the document.
        /// (Revision 3 or greater) Print the document (possibly not at the highest quality level, see <see cref="PrintHighQuality"/>).
        /// </summary>
        Print = 1 << 2,
        /// <summary>
        /// Modify the contents of the document by operations other than those
        /// controlled by <see cref="AddOrModifyTextAnnotationsAndFillFormFields"/>, <see cref="FillExistingFormFields"/> and <see cref="AssembleDocument"/>. 
        /// </summary>
        Modify = 1 << 3,
        /// <summary>
        /// (Revision 2) Copy or otherwise extract text and graphics from the document, including extracting text and graphics
        /// (in support of accessibility to users with disabilities or for other purposes).
        /// (Revision 3 or greater) Copy or otherwise extract text and graphics from the document by operations other
        /// than that controlled by <see cref="ExtractTextAndGraphics"/>. 
        /// </summary>
        CopyTextAndGraphics = 1 << 4,
        /// <summary>
        /// Add or modify text annotations, fill in interactive form fields, and, if <see cref="Modify"/> is also set,
        /// create or modify interactive form fields (including signature fields). 
        /// </summary>
        AddOrModifyTextAnnotationsAndFillFormFields = 1 << 5,
        /// <summary>
        /// (Revision 3 or greater) Fill in existing interactive form fields (including signature fields),
        /// even if <see cref="AddOrModifyTextAnnotationsAndFillFormFields"/> is clear. 
        /// </summary>
        FillExistingFormFields = 1 << 8,
        /// <summary>
        /// (Revision 3 or greater) Extract text and graphics (in support of accessibility to users with disabilities or for other purposes). 
        /// </summary>
        ExtractTextAndGraphics = 1 << 9,
        /// <summary>
        /// (Revision 3 or greater) Assemble the document (insert, rotate, or delete pages and create bookmarks or thumbnail images),
        /// even if <see cref="Modify"/> is clear. 
        /// </summary>
        AssembleDocument = 1 << 10,
        /// <summary>
        /// (Revision 3 or greater) Print the document to a representation from  which a faithful digital copy of the PDF content could be generated.
        /// When this is clear (and <see cref="Print"/> is set), printing is limited to a low-level representation of the appearance,
        /// possibly of degraded quality. 
        /// </summary>
        PrintHighQuality = 1 << 12
    }
}