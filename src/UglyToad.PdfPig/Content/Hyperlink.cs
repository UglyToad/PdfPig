namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using Annotations;
    using Geometry;

    /// <summary>
    /// Full details for a link annotation which references an external resource.
    /// A link to an external resource in a document.
    /// </summary>
    public class Hyperlink
    {
        /// <summary>
        /// The area on the page which when clicked will open the hyperlink.
        /// </summary>
        public PdfRectangle Bounds { get; }

        /// <summary>
        /// The text in the link region (if any).
        /// </summary>
        public string Text { get; }

        /// <summary>
        /// The letters in the link region.
        /// </summary>
        public IReadOnlyList<Letter> Letters { get; }

        /// <summary>
        /// The URI the link directs to.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// The underlying link annotation.
        /// </summary>
        public Annotation Annotation { get; }

        /// <summary>
        /// Create a new <see cref="Hyperlink"/>.
        /// </summary>
        public Hyperlink(PdfRectangle bounds, IReadOnlyList<Letter> letters, string text, string uri, Annotation annotation)
        {
            Bounds = bounds;
            Text = text ?? string.Empty;
            Letters = letters ?? throw new ArgumentNullException(nameof(letters));
            Uri = uri ?? string.Empty;
            Annotation = annotation ?? throw new ArgumentNullException(nameof(annotation));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Link: {Text} ({Uri})";
        }
    }
}
