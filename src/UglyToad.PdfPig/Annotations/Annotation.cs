namespace UglyToad.PdfPig.Annotations
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Tokens;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// An annotation on a page in a PDF document.
    /// </summary>
    public class Annotation
    {
        /// <summary>
        /// The underlying PDF dictionary which this annotation was created from.
        /// </summary>
        [NotNull]
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
        [CanBeNull]
        public string Content { get; }

        /// <summary>
        /// The name of this annotation which should be unique per page. Optional.
        /// </summary>
        [CanBeNull]
        public string Name { get; }

        /// <summary>
        /// The date and time the annotation was last modified, can be in any format. Optional.
        /// </summary>
        [CanBeNull]
        public string ModifiedDate { get; }

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
        /// Create a new <see cref="Annotation"/>.
        /// </summary>
        public Annotation(DictionaryToken annotationDictionary, AnnotationType type, PdfRectangle rectangle, string content, string name, string modifiedDate,
            AnnotationFlags flags, AnnotationBorder border, IReadOnlyList<QuadPointsQuadrilateral> quadPoints)
        {
            AnnotationDictionary = annotationDictionary ?? throw new ArgumentNullException(nameof(annotationDictionary));
            Type = type;
            Rectangle = rectangle;
            Content = content;
            Name = name;
            ModifiedDate = modifiedDate;
            Flags = flags;
            Border = border;
            QuadPoints = quadPoints ?? EmptyArray<QuadPointsQuadrilateral>.Instance;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{Type} - {Content}";
        }
    }
}