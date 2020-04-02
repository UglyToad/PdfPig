namespace UglyToad.PdfPig.Graphics
{
    using System;
    using System.Collections.Generic;
    using Content;
    using Filters;
    using PdfPig.Core;
    using Tokenization.Scanner;
    using Tokens;
    using XObjects;

    /// <summary>
    /// Handles building <see cref="MarkedContentElement"/>s.
    /// </summary>
    internal class MarkedContentStack
    {
        private readonly Stack<MarkedContentElementActiveBuilder> builderStack = new Stack<MarkedContentElementActiveBuilder>();

        private int number = -1;
        private MarkedContentElementActiveBuilder top;

        public bool CanPop => top != null;

        public void Push(NameToken name, DictionaryToken properties)
        {
            if (builderStack.Count == 0) // only increase index if root
            {
                number++;
            }
            
            top = new MarkedContentElementActiveBuilder(number, name, properties);
            builderStack.Push(top);
        }

        public MarkedContentElement Pop(IPdfTokenScanner pdfScanner)
        {
            var builder = builderStack.Pop();
            
            var result = builder.Build(pdfScanner);

            if (builderStack.Count > 0)
            {
                top = builderStack.Peek();
                top.Children.Add(result);
                return null; // do not return child
            }
            else
            {
                top = null;
            }

            return result;
        }

        public void AddLetter(Letter letter)
        {
            top?.AddLetter(letter);
        }

        public void AddPath(PdfSubpath path)
        {
            top?.AddPath(path);
        }

        public void AddImage(IPdfImage image)
        {
            top?.AddImage(image);
        }

        public void AddXObject(XObjectContentRecord xObject,
            IPdfTokenScanner scanner,
            IFilterProvider filterProvider,
            IResourceStore resourceStore)
        {
            if (top != null && xObject.Type == XObjectType.Image)
            {
                var image = XObjectFactory.ReadImage(xObject, scanner, filterProvider, resourceStore);
                top?.AddImage(image);
            }
        }
        
        private class MarkedContentElementActiveBuilder
        {
            private readonly int number;
            private readonly NameToken name;
            private readonly DictionaryToken properties;

            private readonly List<Letter> letters = new List<Letter>();
            private readonly List<PdfSubpath> paths = new List<PdfSubpath>();
            private readonly List<IPdfImage> images = new List<IPdfImage>();

            public List<MarkedContentElement> Children { get; } = new List<MarkedContentElement>();

            public MarkedContentElementActiveBuilder(int number, NameToken name, DictionaryToken properties)
            {
                this.number = number;
                this.name = name;
                this.properties = properties ?? new DictionaryToken(new Dictionary<NameToken, IToken>());
            }

            public void AddLetter(Letter letter)
            {
                letters.Add(letter);
            }

            public void AddImage(IPdfImage image)
            {
                images.Add(image);
            }

            public void AddPath(PdfSubpath path)
            {
                paths.Add(path);
            }

            public MarkedContentElement Build(IPdfTokenScanner pdfScanner)
            {
                var mcid = -1;
                if (properties.TryGet(NameToken.Mcid, pdfScanner, out NumericToken mcidToken))
                {
                    mcid = mcidToken.Int;
                }

                var language = GetOptional(NameToken.Lang, pdfScanner);
                var actualText = GetOptional(NameToken.ActualText, pdfScanner);
                var alternateDescription = GetOptional(NameToken.Alternate, pdfScanner);
                var expandedForm = GetOptional(NameToken.E, pdfScanner);
                
                if (name != NameToken.Artifact)
                {
                    return new MarkedContentElement(mcid, name, properties,
                        language,
                        actualText,
                        alternateDescription,
                        expandedForm,
                        false,
                        Children,
                        letters,
                        paths,
                        images,
                        number);
                }

                var artifactType = ArtifactMarkedContentElement.ArtifactType.Unknown;
                if (properties.TryGet(NameToken.Type, pdfScanner, out IDataToken<string> typeToken)
                    && Enum.TryParse(typeToken.Data, true, out ArtifactMarkedContentElement.ArtifactType parsedType))
                {
                    artifactType = parsedType;
                }

                var subType = GetOptional(NameToken.Subtype, pdfScanner);
                var attributeOwners = GetOptional(NameToken.O, pdfScanner);

                var boundingBox = default(PdfRectangle?);
                if (properties.TryGet(NameToken.Bbox, pdfScanner, out ArrayToken arrayToken))
                {
                    NumericToken left = null;
                    NumericToken bottom = null;
                    NumericToken right = null;
                    NumericToken top = null;

                    if (arrayToken.Length == 4)
                    {
                        left = arrayToken[0] as NumericToken;
                        bottom = arrayToken[1] as NumericToken;
                        right = arrayToken[2] as NumericToken;
                        top = arrayToken[3] as NumericToken;
                    }
                    else if (arrayToken.Length == 6)
                    {
                        left = arrayToken[2] as NumericToken;
                        bottom = arrayToken[3] as NumericToken;
                        right = arrayToken[4] as NumericToken;
                        top = arrayToken[5] as NumericToken;
                    }

                    if (left != null && bottom != null && right != null && top != null)
                    {
                        boundingBox = new PdfRectangle(left.Double, bottom.Double, right.Double, top.Double);
                    }
                }

                var attached = new List<NameToken>();
                if (properties.TryGet(NameToken.Attached, out ArrayToken attachedToken))
                {
                    foreach (var token in attachedToken.Data)
                    {
                        if (token is NameToken aName)
                        {
                            attached.Add(aName);
                        }
                    }
                }

                return new ArtifactMarkedContentElement(mcid, name, properties, language,
                    actualText,
                    alternateDescription,
                    expandedForm,
                    artifactType,
                    subType,
                    attributeOwners,
                    boundingBox,
                    attached,
                    Children,
                    letters,
                    paths,
                    images,
                    number);
            }

            private string GetOptional(NameToken optionName, IPdfTokenScanner pdfScanner)
            {
                var result = default(string);
                if (properties.TryGet(optionName, pdfScanner, out IDataToken<string> token))
                {
                    result = token.Data;
                }

                return result;
            }
        }
    }
}