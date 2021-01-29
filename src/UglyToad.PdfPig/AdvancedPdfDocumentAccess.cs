namespace UglyToad.PdfPig
{
    using System;
    using System.Collections.Generic;
    using Content;
    using Core;
    using Filters;
    using Parser.Parts;
    using System.Linq;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    /// <inheritdoc />
    /// <summary>
    /// Provides access to rare or advanced features from the PDF specification.
    /// </summary>
    public class AdvancedPdfDocumentAccess : IDisposable
    {
        private readonly IPdfTokenScanner pdfScanner;
        private readonly IFilterProvider filterProvider;
        private readonly Catalog catalog;

        private bool isDisposed;

        internal AdvancedPdfDocumentAccess(IPdfTokenScanner pdfScanner,
            IFilterProvider filterProvider,
            Catalog catalog)
        {
            this.pdfScanner = pdfScanner ?? throw new ArgumentNullException(nameof(pdfScanner));
            this.filterProvider = filterProvider ?? throw new ArgumentNullException(nameof(filterProvider));
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        }

        /// <summary>
        /// Get any embedded files contained in this PDF document.
        /// Since PDF 1.3 any external file referenced by the document may have its contents embedded within the referring PDF file, 
        /// allowing its contents to be stored or transmitted along with the PDF file.
        /// </summary>
        /// <param name="embeddedFiles">The set of embedded files in this document.</param>
        /// <returns><see langword="true"/> if this document contains more than zero embedded files, otherwise <see langword="false"/>.</returns>
        public bool TryGetEmbeddedFiles(out IReadOnlyList<EmbeddedFile> embeddedFiles)
        {
            GuardDisposed();

            embeddedFiles = null;

            if (!catalog.CatalogDictionary.TryGet(NameToken.Names, pdfScanner, out DictionaryToken namesDictionary)
                || !namesDictionary.TryGet(NameToken.EmbeddedFiles, pdfScanner, out DictionaryToken embeddedFileNamesDictionary))
            {
                return false;
            }

            var embeddedFileNames = NameTreeParser.FlattenNameTreeToDictionary(embeddedFileNamesDictionary, pdfScanner, x => x);

            if (embeddedFileNames.Count == 0)
            {
                return false;
            }

            var result = new List<EmbeddedFile>();

            foreach (var keyValuePair in embeddedFileNames)
            {
                if (!DirectObjectFinder.TryGet(keyValuePair.Value, pdfScanner, out DictionaryToken fileDescriptorDictionaryToken)
                    || !fileDescriptorDictionaryToken.TryGet(NameToken.Ef, pdfScanner, out DictionaryToken efDictionary)
                    || !efDictionary.TryGet(NameToken.F, pdfScanner, out StreamToken fileStreamToken))
                {
                    continue;
                }

                var fileSpecification = string.Empty;
                if (fileDescriptorDictionaryToken.TryGet(NameToken.F, pdfScanner, out IDataToken<string> fileSpecificationToken))
                {
                    fileSpecification = fileSpecificationToken.Data;
                }

                var fileBytes = fileStreamToken.Decode(filterProvider);

                result.Add(new EmbeddedFile(keyValuePair.Key, fileSpecification, fileBytes, fileStreamToken));
            }

            embeddedFiles = result;

            return embeddedFiles.Count > 0;
        }

        /// <summary>
        /// Replaces the token in an internal cache that will be returned instead of
        /// scanning the source PDF data for future requests.
        /// </summary>
        /// <param name="reference">The object number for the object to replace.</param>
        /// <param name="replacer">Func that takes existing token as input and return new token.</param>
        public void ReplaceIndirectObject(IndirectReference reference, Func<IToken, IToken> replacer)
        {
            var obj = pdfScanner.Get(reference);
            var replacement = replacer(obj.Data);
            pdfScanner.ReplaceToken(reference, replacement);
        }

        /// <summary>
        /// Replaces the token in an internal cache that will be returned instead of
        /// scanning the source PDF data for future requests.
        /// </summary>
        /// <param name="reference">The object number for the object to replace.</param>
        /// <param name="replacement">Replacement token to use.</param>
        public void ReplaceIndirectObject(IndirectReference reference, IToken replacement)
        {
            pdfScanner.ReplaceToken(reference, replacement);
        }

        /// <summary>
        /// EXPERIMENTAL
        /// Strips all non-text content from  PDF content streams.
        /// Can significantly improve text extraction performance if PDF includes large
        /// amounts of graphics operations.
        /// NOTE: All content stream will be loaded in memory uncompressed.
        /// </summary>
        /// <param name="pages">If page content streams should be stripped.</param>
        /// <param name="forms">If Xform content stream should be stripped</param>
        public void StripNonText(bool pages=true, bool forms=true)
        {
            var replaced = new HashSet<IndirectReference>();
            foreach (var page in WalkTree(catalog.PageTree))
            {
                if (pages && page.Item1.TryGet(NameToken.Contents, out IToken contents))
                {
                    switch (contents)
                    {
                        case IndirectReferenceToken refResult:
                            var stream = pdfScanner.Get(refResult.Data).Data as StreamToken;
                            ReplaceIndirectObject(refResult.Data, stream.StripNonText());
                            replaced.Add(refResult.Data);
                            break;
                        case ArrayToken array:
                            foreach (var ir in array.Data)
                            {
                                var refToken = ir as IndirectReferenceToken;
                                if (refToken == null)
                                {
                                    continue;
                                }
                                replaced.Add(refToken.Data);
                                var currentStream = pdfScanner.Get(refToken.Data).Data as StreamToken;
                                ReplaceIndirectObject(refToken.Data, currentStream.StripNonText());
                            }
                            break;
                    }
                }

                if (!forms)
                {
                    continue;
                }

                if (GetDict(page.Item1, NameToken.Resources, out DictionaryToken res))
                {
                    TrimContentStreams(res, replaced);
                }
            
                foreach (var parent in page.Item2)
                {
                    if (GetDict( parent, NameToken.Resources, out DictionaryToken parentRes))
                    {
                        TrimContentStreams(res, replaced);
                    }
                }
            }
        }

        internal static IEnumerable<(DictionaryToken, List<DictionaryToken>)> WalkTree(PageTreeNode node, List<DictionaryToken> parents=null)
        {
            if (parents == null)
            {
                parents = new List<DictionaryToken>();
            }
            
            if (node.IsPage)
            {
                yield return (node.NodeDictionary, parents);
                yield break;
            }

            parents = parents.ToList();
            parents.Add(node.NodeDictionary);
            foreach (var child in node.Children)
            {
                foreach (var item in WalkTree(child, parents))
                {
                    yield return item;
                }
            }
        }

        private void TrimContentStreams(DictionaryToken resources, HashSet<IndirectReference> replaced)
        {
            if (resources.TryGet(NameToken.Xobject, out DictionaryToken formDict))
            {
                foreach (var item in formDict.Data)
                {
                    var xobjRef = item.Value as IndirectReferenceToken;
                    if (xobjRef == null)
                    {
                        continue;
                    }
                    if (replaced.Contains(xobjRef.Data))
                    {
                        continue;
                    }

                    var xobjData = pdfScanner.Get(xobjRef.Data).Data as StreamToken;
                    if (xobjData == null) // ??
                    {
                        continue;
                    }

                    var xobj = (xobjData).StreamDictionary;
                    if (xobj.TryGet(NameToken.Subtype, out NameToken value) && value.Data == "Form")
                    {
                        if (xobj.TryGet(NameToken.Resources, out DictionaryToken resDict))
                        {
                            TrimContentStreams(resDict, replaced);
                        }

                        ReplaceIndirectObject(xobjRef.Data, xobjData.StripNonText());
                    }
                    replaced.Add(xobjRef.Data);
                }
            }
        }


        private bool GetDict(DictionaryToken dict, NameToken name, out DictionaryToken result)
        {
            if (dict.TryGet(name, out IToken token))
            {
                switch (token)
                {
                    case DictionaryToken dictResult:
                        result = dictResult;
                        return true;
                    case IndirectReferenceToken irResult:
                        result = pdfScanner.Get(irResult.Data).Data as DictionaryToken;
                        return true;
                }
            }
            result = null;
            return false;
        }
            

        private void GuardDisposed()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(AdvancedPdfDocumentAccess));
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            pdfScanner?.Dispose();
            isDisposed = true;
        }
    }
}