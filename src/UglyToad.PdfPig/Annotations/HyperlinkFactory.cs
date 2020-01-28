namespace UglyToad.PdfPig.Annotations
{
    using System.Collections.Generic;
    using System.Linq;
    using Content;
    using Core;
    using Geometry;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal static class HyperlinkFactory
    {
        public static IReadOnlyList<Hyperlink> GetHyperlinks(Page page, IPdfTokenScanner pdfScanner, AnnotationProvider annotationProvider)
        {
            var result = new List<Hyperlink>();

            var annotations = annotationProvider.GetAnnotations();

            foreach (var annotation in annotations)
            {
                if (annotation.Type != AnnotationType.Link)
                {
                    continue;
                }

                // Must be a link annotation with an action of type /URI.
                if (!annotation.AnnotationDictionary.TryGet(NameToken.A, pdfScanner, out DictionaryToken actionDictionary)
                    || !actionDictionary.TryGet(NameToken.S, pdfScanner, out NameToken actionType)
                    || actionType != NameToken.Uri)
                {
                    continue;
                }

                // (Required) The uniform resource identifier to resolve, encoded in 7-bit ASCII. 
                if (!actionDictionary.TryGet(NameToken.Uri, pdfScanner, out IDataToken<string> uriStringToken))
                {
                    continue;
                }

                var bounds = annotation.Rectangle;

                // Build in tolerance for letters close to the link region.
                var tolerantBounds = new PdfRectangle(bounds.BottomLeft.Translate(-0.5, -0.5), bounds.TopRight.Translate(0.5, 0.5));

                var linkLetters = new List<Letter>();

                foreach (var letter in page.Letters)
                {
                    if (tolerantBounds.Contains(letter.Location, true))
                    {
                        linkLetters.Add(letter);
                    }
                }

                var words = DefaultWordExtractor.Instance.GetWords(linkLetters);

                var presentationText = string.Join(" ", words.Select(x => x.Text));

                result.Add(new Hyperlink(bounds, linkLetters, presentationText, uriStringToken.Data, annotation));
            }

            return result;
        }
    }
}
