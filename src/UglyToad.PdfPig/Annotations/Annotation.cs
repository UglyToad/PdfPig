namespace UglyToad.PdfPig.Annotations
{
    using Core;
    using Actions;
    using Tokens;

    /// <summary>
    /// An annotation on a page in a PDF document.
    /// </summary>
    public class Annotation
    {
        internal readonly AppearanceStream? normalAppearanceStream;
        internal readonly AppearanceStream? rollOverAppearanceStream;
        internal readonly AppearanceStream? downAppearanceStream;
        internal readonly string? appearanceState;

        /// <summary>
        /// The underlying PDF dictionary which this annotation was created from.
        /// </summary>
        public DictionaryToken AnnotationDictionary { get; }

        /// <summary>
        /// The type of this annotation.
        /// </summary>
        public AnnotationType Type { get; }

        /// <summary>
        /// The rectangle in user space units specifying the location to place this annotation on the page.
        /// </summary>
        public PdfRectangle Rectangle { get; }

        /// <summary>
        /// The annotation text, or if the annotation does not display text, a description of the annotation's contents. Optional.
        /// </summary>
        public string? Content { get; }

        /// <summary>
        /// The name of this annotation which should be unique per page. Optional.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// The date and time the annotation was last modified, can be in any format. Optional.
        /// </summary>
        public string? ModifiedDate { get; }

        /// <summary>
        /// Flags defining the appearance and behaviour of this annotation.
        /// </summary>
        public AnnotationFlags Flags { get; }

        /// <summary>
        /// Defines the annotation's border.
        /// </summary>
        public AnnotationBorder Border { get; }

        /// <summary>
        /// Rectangles defined using QuadPoints, for <see cref="AnnotationType.Link"/> these are the regions used to activate the link,
        /// for text markup annotations these are the text regions to apply the markup to.
        /// See <see cref="QuadPointsQuadrilateral.Points"/> for more information regarding the order of the points.
        /// </summary>
        public IReadOnlyList<QuadPointsQuadrilateral> QuadPoints { get; }

        /// <summary>
        /// Action for this annotation, if any (can be null)
        /// </summary>
        public PdfAction? Action { get; }

        /// <summary>
        /// Indicates if a normal appearance is present for this annotation
        /// </summary>
        public bool HasNormalAppearance => normalAppearanceStream != null;

        /// <summary>
        /// Indicates if a roll over appearance is present for this annotation (shown when you hover over this annotation)
        /// </summary>
        public bool HasRollOverAppearance => rollOverAppearanceStream != null;

        /// <summary>
        /// Indicates if a down appearance is present for this annotation (shown when you click on this annotation)
        /// </summary>
        public bool HasDownAppearance => downAppearanceStream != null;

        /// <summary>
        /// The <see cref="Annotation"/> this annotation was in reply to. Can be <see langword="null" />
        /// </summary>
        public Annotation? InReplyTo { get; }

        /// <summary>
        /// Create a new <see cref="Annotation"/>.
        /// </summary>
        public Annotation(
            DictionaryToken annotationDictionary,
            AnnotationType type,
            PdfRectangle rectangle,
            string? content,
            string? name,
            string? modifiedDate,
            AnnotationFlags flags,
            AnnotationBorder border,
            IReadOnlyList<QuadPointsQuadrilateral> quadPoints,
            PdfAction? action,
            AppearanceStream? normalAppearanceStream,
            AppearanceStream? rollOverAppearanceStream,
            AppearanceStream? downAppearanceStream,
            string? appearanceState,
            Annotation? inReplyTo)
        {
            AnnotationDictionary = annotationDictionary ?? throw new ArgumentNullException(nameof(annotationDictionary));
            Type = type;
            Rectangle = rectangle;
            Content = content;
            Name = name;
            ModifiedDate = modifiedDate;
            Flags = flags;
            Border = border;
            QuadPoints = quadPoints ?? Array.Empty<QuadPointsQuadrilateral>();
            Action = action;
            this.normalAppearanceStream = normalAppearanceStream;
            this.rollOverAppearanceStream = rollOverAppearanceStream;
            this.downAppearanceStream = downAppearanceStream;
            this.appearanceState = appearanceState;
            InReplyTo = inReplyTo;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Type} - {Content}";
        }
    }
}