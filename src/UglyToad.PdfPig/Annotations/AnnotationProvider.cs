namespace UglyToad.PdfPig.Annotations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

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

                var contents = GetNamedString(NameToken.Contents, annotationDictionary);
                var name = GetNamedString(NameToken.Nm, annotationDictionary);
                var modifiedDate = GetNamedString(NameToken.M, annotationDictionary);

                var flags = (AnnotationFlags) 0;
                if (annotationDictionary.TryGet(NameToken.F, out var flagsToken) && DirectObjectFinder.TryGet(flagsToken, tokenScanner, out NumericToken flagsNumericToken))
                {
                    flags = (AnnotationFlags) flagsNumericToken.Int;
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

                yield return new Annotation(annotationDictionary, annotationType, rectangle, contents, name, modifiedDate, flags, border);
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