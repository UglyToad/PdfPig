namespace UglyToad.PdfPig.Annotations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Logging;
    using Outline;
    using Outline.Destinations;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal class AnnotationProvider
    {
        private readonly IPdfTokenScanner tokenScanner;
        private readonly DictionaryToken pageDictionary;
        private readonly NamedDestinations namedDestinations;
        private readonly ILog log;
        private readonly TransformationMatrix matrix;

        public AnnotationProvider(IPdfTokenScanner tokenScanner, DictionaryToken pageDictionary,
            TransformationMatrix matrix, NamedDestinations namedDestinations, ILog log)
        {
            this.matrix = matrix;
            this.tokenScanner = tokenScanner ?? throw new ArgumentNullException(nameof(tokenScanner));
            this.pageDictionary = pageDictionary ?? throw new ArgumentNullException(nameof(pageDictionary));
            this.namedDestinations = namedDestinations;
            this.log = log;
        }

        public IEnumerable<Annotation> GetAnnotations()
        {
            if (!pageDictionary.TryGet(NameToken.Annots, tokenScanner, out ArrayToken annotationsArray))
            {
                yield break;
            }

            foreach (var token in annotationsArray.Data)
            {
                if (!DirectObjectFinder.TryGet(token, tokenScanner, out DictionaryToken annotationDictionary))
                {
                        continue;
                }

                var type = annotationDictionary.Get<NameToken>(NameToken.Subtype, tokenScanner);

                var annotationType = type.ToAnnotationType();

                if (!BookmarksProvider.TryGetDestination(annotationDictionary, NameToken.Dest, namedDestinations,
                        tokenScanner, log, false, out var destination))
                {
                    if (BookmarksProvider.TryGetAction(annotationDictionary, namedDestinations, tokenScanner, log,
                            out var actionResult))
                    {
                        destination = actionResult.destination;
                    }
                    else
                    {
                        destination = null;
                    }
                }

                var rectangle = matrix.Transform(annotationDictionary.Get<ArrayToken>(NameToken.Rect, tokenScanner).ToRectangle(tokenScanner));

                var contents = GetNamedString(NameToken.Contents, annotationDictionary);
                var name = GetNamedString(NameToken.Nm, annotationDictionary);
                // As indicated in PDF reference 8.4.1, the modified date can be anything, but is usually a date formatted according to sec. 3.8.3
                var modifiedDate = GetNamedString(NameToken.M, annotationDictionary);

                var flags = (AnnotationFlags)0;
                if (annotationDictionary.TryGet(NameToken.F, out var flagsToken) && DirectObjectFinder.TryGet(flagsToken, tokenScanner, out NumericToken flagsNumericToken))
                {
                    flags = (AnnotationFlags)flagsNumericToken.Int;
                }

                var border = AnnotationBorder.Default;
                if (annotationDictionary.TryGet(NameToken.Border, out var borderToken) && DirectObjectFinder.TryGet(borderToken, tokenScanner, out ArrayToken borderArray)
                    && borderArray.Length >= 3)
                {
                    var horizontal = borderArray.GetNumeric(0).Data;
                    var vertical = borderArray.GetNumeric(1).Data;
                    var width = borderArray.GetNumeric(2).Data;
                    var dashes = default(IReadOnlyList<decimal>);

                    if (borderArray.Length == 4 && borderArray.Data[4] is ArrayToken dashArray)
                    {
                        dashes = dashArray.Data.OfType<NumericToken>().Select(x => x.Data).ToList();
                    }

                    border = new AnnotationBorder(horizontal, vertical, width, dashes);
                }

                var quadPointRectangles = new List<QuadPointsQuadrilateral>();
                if (annotationDictionary.TryGet(NameToken.Quadpoints, tokenScanner, out ArrayToken quadPointsArray))
                {
                    var values = new List<decimal>();
                    for (var i = 0; i < quadPointsArray.Length; i++)
                    {
                        if (!(quadPointsArray[i] is NumericToken value))
                        {
                            continue;
                        }

                        values.Add(value.Data);

                        if (values.Count == 8)
                        {
                            quadPointRectangles.Add(new QuadPointsQuadrilateral(new[]
                            {
                                matrix.Transform(new PdfPoint(values[0], values[1])), 
                                matrix.Transform(new PdfPoint(values[2], values[3])), 
                                matrix.Transform(new PdfPoint(values[4], values[5])), 
                                matrix.Transform(new PdfPoint(values[6], values[7]))
                            }));

                            values.Clear();
                        }
                    }
                }

                StreamToken normalAppearanceStream = null, downAppearanceStream = null, rollOverAppearanceStream = null;
                if (annotationDictionary.TryGet(NameToken.Ap, out DictionaryToken appearanceDictionary))
                {
                    // The normal appearance of this annotation
                    if (appearanceDictionary.TryGet(NameToken.N, out IndirectReferenceToken normalAppearanceRef))
                    {
                        normalAppearanceStream = tokenScanner.Get(normalAppearanceRef.Data)?.Data as StreamToken;
                    }
                    // If present, the 'roll over' appearance of this annotation (when hovering the mouse pointer over this annotation)
                    if (appearanceDictionary.TryGet(NameToken.R, out IndirectReferenceToken rollOverAppearanceRef))
                    {
                        rollOverAppearanceStream = tokenScanner.Get(rollOverAppearanceRef.Data)?.Data as StreamToken;
                    }
                    // If present, the 'down' appearance of this annotation (when you click on it)
                    if (appearanceDictionary.TryGet(NameToken.D, out IndirectReferenceToken downAppearanceRef))
                    {
                        downAppearanceStream = tokenScanner.Get(downAppearanceRef.Data)?.Data as StreamToken;
                    }
                }

                yield return new Annotation(annotationDictionary, annotationType, rectangle, 
                    contents, name, modifiedDate, flags, border, quadPointRectangles, destination,
                    normalAppearanceStream, rollOverAppearanceStream, downAppearanceStream);
            }
        }

        private string GetNamedString(NameToken name, DictionaryToken dictionary)
        {
            string content = null;
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
                else if (DirectObjectFinder.TryGet(contentToken, tokenScanner, out StringToken indirectContentString))
                {
                    content = indirectContentString.Data;
                }
            }

            return content;
        }
    }
}