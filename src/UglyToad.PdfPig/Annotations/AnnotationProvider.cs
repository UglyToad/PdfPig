namespace UglyToad.PdfPig.Annotations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Actions;
    using Core;
    using Logging;
    using Outline.Destinations;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    /// <summary>
    /// Annotation provider.
    /// </summary>
    public class AnnotationProvider
    {
        private readonly IPdfTokenScanner tokenScanner;
        private readonly DictionaryToken pageDictionary;
        private readonly NamedDestinations namedDestinations;
        private readonly ILog log;
        private readonly TransformationMatrix matrix;

        /// <summary>
        /// Create a <see cref="AnnotationProvider"/>.
        /// </summary>
        public AnnotationProvider(IPdfTokenScanner tokenScanner,
            DictionaryToken pageDictionary,
            TransformationMatrix matrix,
            NamedDestinations namedDestinations,
            ILog log)
        {
            this.matrix = matrix;
            this.tokenScanner = tokenScanner ?? throw new ArgumentNullException(nameof(tokenScanner));
            this.pageDictionary = pageDictionary ?? throw new ArgumentNullException(nameof(pageDictionary));
            this.namedDestinations = namedDestinations;
            this.log = log;
        }

        /// <summary>
        /// Get the annotations.
        /// </summary>
        public IEnumerable<Annotation> GetAnnotations()
        {
            var lookupAnnotations = new Dictionary<IndirectReference, Annotation>();

            if (!pageDictionary.TryGet(NameToken.Annots, tokenScanner, out ArrayToken? annotationsArray))
            {
                yield break;
            }

            foreach (var token in annotationsArray.Data)
            {
                if (!DirectObjectFinder.TryGet(token, tokenScanner, out DictionaryToken? annotationDictionary))
                {
                    continue;
                }

                Annotation? replyTo = null;
                if (annotationDictionary.TryGet(NameToken.Irt, out IndirectReferenceToken? referencedAnnotation)
                    && lookupAnnotations.TryGetValue(referencedAnnotation!.Data, out var linkedAnnotation))
                {
                    replyTo = linkedAnnotation;
                }

                var type = annotationDictionary.Get<NameToken>(NameToken.Subtype, tokenScanner);
                var annotationType = type.ToAnnotationType();
                var action = GetAction(annotationDictionary);
                var rectangle = matrix.Transform(annotationDictionary.Get<ArrayToken>(NameToken.Rect, tokenScanner)
                    .ToRectangle(tokenScanner));
                var contents = GetNamedString(NameToken.Contents, annotationDictionary);
                var name = GetNamedString(NameToken.Nm, annotationDictionary);
                // As indicated in PDF reference 8.4.1, the modified date can be anything, but is usually a date formatted according to sec. 3.8.3
                var modifiedDate = GetNamedString(NameToken.M, annotationDictionary);

                var flags = (AnnotationFlags)0;
                if (annotationDictionary.TryGet(NameToken.F, out var flagsToken) &&
                    DirectObjectFinder.TryGet(flagsToken, tokenScanner, out NumericToken? flagsNumericToken))
                {
                    flags = (AnnotationFlags)flagsNumericToken.Int;
                }

                var border = AnnotationBorder.Default;
                if (annotationDictionary.TryGet(NameToken.Border, out var borderToken) &&
                    DirectObjectFinder.TryGet(borderToken, tokenScanner, out ArrayToken? borderArray)
                    && borderArray.Length >= 3)
                {
                    var horizontal = borderArray.GetNumeric(0).Data;
                    var vertical = borderArray.GetNumeric(1).Data;
                    var width = borderArray.GetNumeric(2).Data;
                    var dashes = default(IReadOnlyList<double>);

                    if (borderArray.Length == 4 && borderArray.Data[3] is ArrayToken dashArray)
                    {
                        // PDFBOX-624-2.pdf
                        dashes = dashArray.Data.OfType<NumericToken>().Select(x => x.Data).ToArray();
                    }

                    border = new AnnotationBorder(horizontal, vertical, width, dashes);
                }

                var quadPointRectangles = new List<QuadPointsQuadrilateral>();
                if (annotationDictionary.TryGet(NameToken.Quadpoints, tokenScanner, out ArrayToken? quadPointsArray))
                {
                    var values = new List<double>();
                    for (var i = 0; i < quadPointsArray.Length; i++)
                    {
                        if (!(quadPointsArray[i] is NumericToken value))
                        {
                            continue;
                        }

                        values.Add(value.Data);

                        if (values.Count == 8)
                        {
                            quadPointRectangles.Add(new QuadPointsQuadrilateral(
                            [
                                matrix.Transform(new PdfPoint(values[0], values[1])),
                                matrix.Transform(new PdfPoint(values[2], values[3])),
                                matrix.Transform(new PdfPoint(values[4], values[5])),
                                matrix.Transform(new PdfPoint(values[6], values[7]))
                            ]));

                            values.Clear();
                        }
                    }
                }

                AppearanceStream? normalAppearanceStream = null;
                AppearanceStream? downAppearanceStream = null;
                AppearanceStream? rollOverAppearanceStream = null;

                if (annotationDictionary.TryGet(NameToken.Ap, out DictionaryToken appearanceDictionary))
                {
                    // The normal appearance of this annotation
                    if (AppearanceStreamFactory.TryCreate(appearanceDictionary, NameToken.N, tokenScanner, out AppearanceStream? stream))
                    {
                        normalAppearanceStream = stream;
                    }

                    // If present, the 'roll over' appearance of this annotation (when hovering the mouse pointer over this annotation)
                    if (AppearanceStreamFactory.TryCreate(appearanceDictionary, NameToken.R, tokenScanner, out stream))
                    {
                        rollOverAppearanceStream = stream;
                    }

                    // If present, the 'down' appearance of this annotation (when you click on it)
                    if (AppearanceStreamFactory.TryCreate(appearanceDictionary, NameToken.D, tokenScanner, out stream))
                    {
                        downAppearanceStream = stream;
                    }
                }

                string? appearanceState = null;
                if (annotationDictionary.TryGet(NameToken.As, out NameToken appearanceStateToken))
                {
                    appearanceState = appearanceStateToken.Data;
                }

                var annotation = new Annotation(
                    annotationDictionary,
                    annotationType,
                    rectangle,
                    contents,
                    name,
                    modifiedDate,
                    flags,
                    border,
                    quadPointRectangles,
                    action,
                    normalAppearanceStream,
                    rollOverAppearanceStream,
                    downAppearanceStream,
                    appearanceState,
                    replyTo);

                if (token is IndirectReferenceToken indirectReference)
                {
                    lookupAnnotations[indirectReference.Data] = annotation;
                }

                yield return annotation;
            }
        }

        internal PdfAction? GetAction(DictionaryToken annotationDictionary)
        {
            // If this annotation returns a direct destination, turn it into a GoTo action.
            if (DestinationProvider.TryGetDestination(annotationDictionary,
                    NameToken.Dest,
                    namedDestinations,
                    tokenScanner,
                    log,
                    false,
                    out var destination))
            {
                return new GoToAction(destination);
            }

            // Try get action from the dictionary.
            if (ActionProvider.TryGetAction(annotationDictionary, namedDestinations, tokenScanner, log, out var action))
            {
                return action;
            }

            // No action or destination found, return null
            return null;
        }

        private string? GetNamedString(NameToken name, DictionaryToken dictionary)
        {
            string? content = null;
            if (dictionary.TryGet(name, out var contentToken))
            {
                if (contentToken is StringToken contentString)
                {
                    content = contentString.Data;
                }
                else if (contentToken is HexToken contentHex)
                {
                    content = contentHex.Data;
                }
                else if (DirectObjectFinder.TryGet(contentToken, tokenScanner, out StringToken? indirectContentString))
                {
                    content = indirectContentString.Data;
                }
            }

            return content;
        }
    }
}