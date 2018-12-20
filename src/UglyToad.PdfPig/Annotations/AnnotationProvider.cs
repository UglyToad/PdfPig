namespace UglyToad.PdfPig.Annotations
{
    using System;
    using System.Collections.Generic;
    using Exceptions;
    using Geometry;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using Util;
    using Util.JetBrains.Annotations;

    internal class AnnotationProvider
    {
        private readonly IPdfTokenScanner tokenScanner;
        private readonly DictionaryToken pageDictionary;
        private readonly bool isLenientParsing;

        public AnnotationProvider(IPdfTokenScanner tokenScanner, DictionaryToken pageDictionary, bool isLenientParsing)
        {
            this.tokenScanner = tokenScanner ?? throw new ArgumentNullException(nameof(tokenScanner));
            this.pageDictionary = pageDictionary ?? throw new ArgumentNullException(nameof(pageDictionary));
            this.isLenientParsing = isLenientParsing;
        }

        public IEnumerable<Annotation> GetAnnotations()
        {
            if (!pageDictionary.TryGet(NameToken.Annots, out IToken annotationsToken)
            || !DirectObjectFinder.TryGet(annotationsToken, tokenScanner, out ArrayToken annotationsArray))
            {
                yield break;
            }

            foreach (var token in annotationsArray.Data)
            {
                if (!DirectObjectFinder.TryGet(token, tokenScanner, out DictionaryToken annotationDictionary))
                {
                    if (isLenientParsing)
                    {
                        continue;
                    }

                    throw new PdfDocumentFormatException($"The annotations dictionary contained an annotation which wasn't a dictionary: {token}.");
                }

                if (!isLenientParsing && annotationDictionary.TryGet(NameToken.Type, out NameToken dictionaryType))
                {
                    if (dictionaryType != NameToken.Annot)
                    {
                        throw new PdfDocumentFormatException($"The annotations dictionary contained a non-annotation type dictionary: {annotationDictionary}.");
                    }
                }

                var type = annotationDictionary.Get<NameToken>(NameToken.Subtype, tokenScanner);

                var annotationType = type.ToAnnotationType();
                var rectangle = annotationDictionary.Get<ArrayToken>(NameToken.Rect, tokenScanner).ToRectangle();

                string content = null;
                if (annotationDictionary.TryGet(NameToken.Contents, out var contentToken) && DirectObjectFinder.TryGet(contentToken, tokenScanner, out StringToken contentString))
                {
                    content = contentString.Data;
                }

                yield return new Annotation(annotationDictionary, rectangle, content, null, null);
            }
        }
    }

    internal class Annotation
    {
        /// <summary>
        /// The underlying PDF dictionary which this annotation was created from.
        /// </summary>
        [NotNull]
        public DictionaryToken AnnotationDictionary { get; }

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

        public AnnotationFlags Flags { get; } = (AnnotationFlags)0;

        /// <summary>
        /// Create a new <see cref="Annotation"/>.
        /// </summary>
        public Annotation(DictionaryToken annotationDictionary, PdfRectangle rectangle, string content, string name, string modifiedDate)
        {
            AnnotationDictionary = annotationDictionary ?? throw new ArgumentNullException(nameof(annotationDictionary));
            Rectangle = rectangle;
            Content = content;
            Name = name;
            ModifiedDate = modifiedDate;
        }
    }

    /// <summary>
    /// The standard annotation types in PDF documents.
    /// </summary>
    internal enum AnnotationType
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

    internal static class AnnotationExtensions
    {
        public static AnnotationType ToAnnotationType(this NameToken name)
        {
            if (name.Data == NameToken.Text.Data)
            {
                return AnnotationType.Text;
            }

            if (name.Data == NameToken.Link.Data)
            {
                return AnnotationType.Link;
            }

            if (name.Data == NameToken.FreeText.Data)
            {
                return AnnotationType.FreeText;
            }

            if (name.Data == NameToken.Line.Data)
            {
                return AnnotationType.Line;
            }

            if (name.Data == NameToken.Square.Data)
            {
                return AnnotationType.Square;
            }

            if (name.Data == NameToken.Circle.Data)
            {
                return AnnotationType.Circle;
            }

            if (name.Data == NameToken.Polygon.Data)
            {
                return AnnotationType.Polygon;
            }

            if (name.Data == NameToken.PolyLine.Data)
            {
                return AnnotationType.PolyLine;
            }

            if (name.Data == NameToken.Highlight.Data)
            {
                return AnnotationType.Highlight;
            }

            if (name.Data == NameToken.Underline.Data)
            {
                return AnnotationType.Underline;
            }

            if (name.Data == NameToken.Squiggly.Data)
            {
                return AnnotationType.Squiggly;
            }

            if (name.Data == NameToken.StrikeOut.Data)
            {
                return AnnotationType.StrikeOut;
            }

            if (name.Data == NameToken.Stamp.Data)
            {
                return AnnotationType.Stamp;
            }

            if (name.Data == NameToken.Caret.Data)
            {
                return AnnotationType.Caret;
            }

            if (name.Data == NameToken.Ink.Data)
            {
                return AnnotationType.Ink;
            }

            if (name.Data == NameToken.Popup.Data)
            {
                return AnnotationType.Popup;
            }

            if (name.Data == NameToken.FileAttachment.Data)
            {
                return AnnotationType.FileAttachment;
            }

            if (name.Data == NameToken.Sound.Data)
            {
                return AnnotationType.Sound;
            }

            if (name.Data == NameToken.Movie.Data)
            {
                return AnnotationType.Movie;
            }

            if (name.Data == NameToken.Widget.Data)
            {
                return AnnotationType.Widget;
            }

            if (name.Data == NameToken.Screen.Data)
            {
                return AnnotationType.Screen;
            }

            if (name.Data == NameToken.PrinterMark.Data)
            {
                return AnnotationType.PrinterMark;
            }

            if (name.Data == NameToken.TrapNet.Data)
            {
                return AnnotationType.TrapNet;
            }

            if (name.Data == NameToken.Watermark.Data)
            {
                return AnnotationType.Watermark;
            }

            if (name.Data == NameToken.Annotation3D.Data)
            {
                return AnnotationType.Artwork3D;
            }

            return AnnotationType.Other;
        }
    }
}

