using System;
using UglyToad.PdfPig.Geometry;
using UglyToad.PdfPig.Logging;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Outline
{
    internal class BookmarksProvider
    {
        private readonly ILog log;
        private readonly Structure structure;

        public BookmarksProvider(ILog log, Structure structure)
        {
            this.log = log;
            this.structure = structure;
        }

        /// <summary>
        /// Extract bookmarks, if any.
        /// </summary>
        public Bookmarks GetBookmarks()
        {
            if (structure.Catalog.CatalogDictionary.Data.TryGetValue(NameToken.Outlines, out IToken outlinesToken))
            {
                var outlines = this.structure.GetObject(((IndirectReferenceToken)outlinesToken).Data).Data as DictionaryToken;
                if (outlines.TryGet(NameToken.First, out IndirectReferenceToken firstToken))
                {
                    var rootNode = new BookmarkNode();
                    RecursiveBookmarks(firstToken, ref rootNode);
                    return new Bookmarks(rootNode.Children);
                }
            }
            return null;
        }

        /// <summary>
        /// Extract bookmarks recursively.
        /// </summary>
        /// <param name="locationToken">The outlines' location token, e.g. First, Next.</param>
        /// <param name="node">The current <see cref="BookmarkNode"/>.</param>
        private void RecursiveBookmarks(IndirectReferenceToken locationToken, ref BookmarkNode node)
        {
            // 12.3 Document-Level Navigation
            BookmarkNode newNode = new BookmarkNode() { Level = node.Level + 1 };
            node.Children.Add(newNode);

            var dictionary = structure.GetObject(locationToken.Data).Data as DictionaryToken;
            if (dictionary == null)
            {
                throw new ArgumentNullException("BookmarksProvider.RecursiveBookmarks(): DictionaryToken is null.");
            }

            // 12.3.3 Document Outline - Title
            // (Required) The text that shall be displayed on the screen for this item.
            newNode.Title = GetString(NameToken.Title, locationToken);

            // 12.3.2 Destinations
            if (dictionary.TryGet(NameToken.Dest, out ArrayToken destToken))
            {
                // 12.3.2.2 Explicit Destinations
                GetDestination(destToken, newNode);
            }
            else if (dictionary.TryGet(NameToken.Dest, out IDataToken<string> destStringToken))
            {
                // 12.3.2.3 Named Destinations
                GetNamedDestination(destStringToken, ref newNode);
            }
            else if (dictionary.TryGet(NameToken.A, out IToken actionToken))
            {
                // 12.6 Actions
                GetActions(actionToken, ref newNode);
            }
            else
            {
                log.Error("BookmarksProvider.RecursiveBookmark(): No 'Dest' or 'Action' token found.");
            }

            // Look for children
            if (dictionary.TryGet(NameToken.First, out IndirectReferenceToken firstToken))
            {
                RecursiveBookmarks(firstToken, ref newNode);
            }

            // Move to next
            if (dictionary.TryGet(NameToken.Next, out IndirectReferenceToken nextToken))
            {
                RecursiveBookmarks(nextToken, ref node);
            }
        }

        private string GetString(NameToken nameToken, IToken locationToken)
        {
            if (locationToken is IDataToken<string> stringDataToken)
            {
                return stringDataToken.Data;
            }
            else if (locationToken is DictionaryToken dictionaryToken)
            {
                if (dictionaryToken.TryGet(nameToken, out IToken token))
                {
                    return GetString(nameToken, token);
                }
                else
                {
                    throw new NotImplementedException("BookmarksProvider.GetString(): Unknown nameToken '" + nameToken + "'.");
                }
            }
            else if (locationToken is IndirectReferenceToken indirectReferenceToken)
            {
                var tempToken = structure.GetObject(indirectReferenceToken.Data)?.Data;

                if (tempToken == null)
                {
                    throw new ArgumentNullException("BookmarksProvider.GetString(): Cannot find '" + indirectReferenceToken.Data + "'.");
                }
                return GetString(nameToken, tempToken);
            }
            else
            {
                throw new NotImplementedException("BookmarksProvider.GetString(): Unknown string type '" + locationToken.GetType() + "'.");
            }
        }

        private static int ParsePageNumber(string goToStr)
        {
            if (int.TryParse(System.Text.RegularExpressions.Regex.Match(goToStr, "[0-9]+").Value, out int number))
            {
                return number + 1;
            }
            return 0;
        }

        #region Destinations
        private void GetDestination(ArrayToken destToken, BookmarkNode currentNode)
        {
            if (destToken == null || destToken.Length == 0)
            {
                throw new ArgumentNullException(nameof(destToken), "BookmarksProvider.GetDestination()");
            }

            // 12.3.2.2 Explicit Destinations
            // Table 151 – Destination syntax
            var pageToken = destToken[0];
            if (pageToken is IndirectReferenceToken pageIndirectReferenceToken)
            {
                var pageNumber = structure.Catalog.GetPageByReference(pageIndirectReferenceToken.Data).PageNumber;
                if (pageNumber.HasValue)
                {
                    currentNode.PageNumber = pageNumber.Value;
                }
                else
                {
                    log.Error("BookmarksProvider.GetDestination(): Cannot find page number.");
                }
            }
            else if (pageToken is NumericToken pageNumericToken)
            {
                currentNode.PageNumber = pageNumericToken.Int + 1;
            }
            else
            {
                log.Error("BookmarksProvider.GetDestination(): No page number given in 'Dest': '" + destToken + "'.");
            }

            var destTypeToken = destToken[1] as NameToken;
            if (destTypeToken == null) return;

            if (destTypeToken.Equals(NameToken.XYZ))
            {
                // [page /XYZ left top zoom]
                var left = destToken[2] as NumericToken;
                var top = destToken[3] as NumericToken;
                var zoom = destToken[4] as NumericToken;
                currentNode.TopLeft = new PdfPoint(left?.Data ?? 0, top?.Data ?? 0);
            }
            else if (destTypeToken.Equals(NameToken.Fit))
            {
                // [page /Fit]
            }
            else if (destTypeToken.Equals(NameToken.FitH))
            {
                // [page /FitH top]
                var top = destToken[2] as NumericToken;
                currentNode.TopLeft = new PdfPoint(0, top?.Data ?? 0);
            }
            else if (destTypeToken.Equals(NameToken.FitV))
            {
                // [page /FitV left]
                var left = destToken[2] as NumericToken;
                currentNode.TopLeft = new PdfPoint(left?.Data ?? 0, 0);
            }
            else if (destTypeToken.Equals(NameToken.FitR))
            {
                // [page /FitR left bottom right top]
                var left = destToken[2] as NumericToken;
                var bottom = destToken[3] as NumericToken;
                var right = destToken[4] as NumericToken;
                var top = destToken[5] as NumericToken;
                currentNode.TopLeft = new PdfPoint(left?.Data ?? 0, top?.Data ?? 0);
                currentNode.BoundingBox = new PdfRectangle(left?.Data ?? 0,
                                                           bottom?.Data ?? 0,
                                                           right?.Data ?? 0,
                                                           top?.Data ?? 0);
            }
            else if (destTypeToken.Equals(NameToken.FitB))
            {
                // [page /FitB]
            }
            else if (destTypeToken.Equals(NameToken.FitBH))
            {
                // [page /FitBH top]
            }
            else if (destTypeToken.Equals(NameToken.FitBV))
            {
                // [page /FitBV left]
                var top = destToken[2] as NumericToken;
                currentNode.TopLeft = new PdfPoint(0, top?.Data ?? 0);
            }
            else
            {
                throw new NotImplementedException("BookmarksProvider.GetDestination(): Unknown type '" + destTypeToken + "'.");
            }
        }

        private void GetNamedDestination(IDataToken<string> destStringToken, ref BookmarkNode currentNode)
        {
            if (destStringToken == null)
            {
                throw new ArgumentNullException(nameof(destStringToken), "BookmarksProvider.GetNamedDestination()");
            }

            // 12.3.2.3 Named Destinations
            if (structure.Catalog.CatalogDictionary.TryGet(NameToken.Dests, out IndirectReferenceToken destsToken11))
            {
                // In PDF 1.1, the correspondence between name objects and destinations shall be defined by the 
                // Dests entry in the document catalogue (see 7.7.2, “Document Catalog”). The value of this entry 
                // shall be a dictionary in which each key is a destination name and the corresponding value is 
                // either an array defining the destination, using the syntax shown in Table 151, or a dictionary
                // with a D entry whose value is such an array.
                throw new NotImplementedException("BookmarksProvider.GetNamedDestination(): PDF 1.1.");
            }
            else if (structure.Catalog.CatalogDictionary.TryGet(NameToken.Names, out IndirectReferenceToken namesToken))
            {
                // In PDF 1.2 and later, the correspondence between strings and destinations may alternatively be
                // defined by the Dests entry in the document’s name dictionary (see 7.7.4, “Name Dictionary”). 
                // The value of this entry shall be a name tree (7.9.6, “Name Trees”) mapping name strings to 
                // destinations. (The keys in the name tree may be treated as text strings for display purposes.) 
                // The destination value associated with a key in the name tree may be either an array or a 
                // dictionary, as described in the preceding paragraph.
                var namesDictionary = structure.GetObject(namesToken.Data).Data as DictionaryToken;
                if (namesDictionary == null)
                {
                    throw new ArgumentNullException(nameof(namesDictionary), "BookmarksProvider.GetNamedDestination()");
                }

                if (namesDictionary.TryGet(NameToken.Dests, out IndirectReferenceToken destsToken))
                {
                    var destsDictionary = structure.GetObject(destsToken.Data).Data as DictionaryToken;
                    if (destsDictionary == null)
                    {
                        throw new ArgumentNullException(nameof(destsDictionary), "BookmarksProvider.GetNamedDestination()");
                    }

                    IToken found = FindInNameTree(destStringToken, destsDictionary);
                    if (found != null)
                    {
                        ArrayToken destToken = null;
                        if (found is IndirectReferenceToken indirect)
                        {
                            var pageObject = structure.GetObject(indirect.Data);
                            if (pageObject.Data is DictionaryToken dictionaryToken)
                            {
                                if (!dictionaryToken.TryGet(NameToken.D, out destToken))
                                {
                                    throw new ArgumentException("BookmarksProvider.GetNamedDestination(): Cannot find token 'D'.");
                                }
                            }
                            else if (pageObject.Data is ArrayToken arrayToken)
                            {
                                destToken = arrayToken;
                            }
                            else
                            {
                                throw new NotImplementedException("BookmarksProvider.GetNamedDestination(): Token type '" + pageObject.Data + "'.");
                            }
                        }
                        else if (found is ArrayToken arrayToken)
                        {
                            destToken = arrayToken;
                        }
                        else if (found is DictionaryToken)
                        {
                            throw new NotImplementedException("BookmarksProvider.GetNamedDestination(): Token type 'DictionaryToken'.");
                        }
                        else
                        {
                            throw new NotImplementedException("BookmarksProvider.GetNamedDestination(): Token type '" + found.GetType() + "'.");
                        }

                        var pageNumber = structure.Catalog.GetPageByReference(((IndirectReferenceToken)destToken[0]).Data).PageNumber;
                        if (pageNumber.HasValue)
                        {
                            currentNode.PageNumber = pageNumber.Value;
                        }
                        GetDestination(destToken, currentNode);
                    }
                }
            }
        }

        private IToken FindInNameTree<T>(T find, DictionaryToken dictionaryToken) where T : IDataToken<string>
        {
            // 7.9.6 Name Trees
            // Intermediate node
            if (dictionaryToken.TryGet(NameToken.Kids, out ArrayToken kidsToken))
            {
                foreach (var kid in kidsToken.Data)
                {
                    var dictionary = structure.GetObject(((IndirectReferenceToken)kid).Data).Data as DictionaryToken;
                    if (dictionary != null && dictionary.TryGet(NameToken.Limits, out ArrayToken limits))
                    {
                        // (Intermediate and leaf nodes only; required) Shall be an array of two strings,
                        // that shall specify the (lexically) least and greatest keys included in the
                        // Names array of a leaf node or in the Names arrays of any leaf nodes that are
                        // descendants of an intermediate node.
                        var least = limits[0] as IDataToken<string>;
                        var greatest = limits[1] as IDataToken<string>;

                        if (IsStringBetween(find.Data, least.Data, greatest.Data))
                        {
                            var indRef = FindInNameTree(find, dictionary);
                            if (indRef != null)
                            {
                                return indRef;
                            }
                            else
                            {
                                throw new ArgumentException("BookmarksProvider.FindNamedDestination(): Did no find the key '" + find.Data + "' in Name Tree.");
                            }
                        }
                    }
                }
            }
            else
            {
                // Leaf node
                if (dictionaryToken.TryGet(NameToken.Names, out ArrayToken names))
                {
                    // Names
                    // Shall be an array of the form [key_1, value_1, key_2, value_2, …, key_n, value_n]
                    // where each key_i shall be a string and the corresponding value_i shall be the object 
                    // associated with that key. The keys shall be sorted in lexical order, as described below.
                    for (int i = 0; i < names.Length; i += 2)
                    {
                        if (names[i] is IDataToken<string> n && n.Data.Equals(find.Data))
                        {
                            return names[i + 1];
                        }
                    }
                }
                else
                {
                    throw new ArgumentNullException("BookmarksProvider.FindNamedDestination(): Could not find ArrayToken 'Names' in dictionary.");
                }
            }
            throw new ArgumentException("BookmarksProvider.FindNamedDestination(): Did no find the key '" + find.Data + "' in Name Tree.");
        }

        private bool IsStringBetween(string str, string least, string greatest)
        {
            return (string.Compare(str, least, StringComparison.Ordinal) >= 0 &&
                    string.Compare(str, greatest, StringComparison.Ordinal) <= 0);
        }
        #endregion

        #region Actions
        private void GetActions(IToken actionToken, ref BookmarkNode currentNode)
        {
            if (actionToken is DictionaryToken dictionaryToken)
            {
                if (dictionaryToken.TryGet(NameToken.S, out NameToken sToken))
                {
                    if (sToken.Equals(NameToken.GoTo)) // 12.6.4.2, Go-To Actions
                    {
                        if (dictionaryToken.TryGet(NameToken.D, out IToken goToToken))
                        {
                            HandleGoToAction(goToToken, ref currentNode);
                        }
                        else
                        {
                            throw new ArgumentException("BookmarksProvider.GetActions(): Could not find token 'D' in 'GoTo'.");
                        }
                    }
                    else if (sToken.Equals(NameToken.GoToR)) // 12.6.4.3, Remote Go-To Actions
                    {
                        if (dictionaryToken.TryGet(NameToken.D, out IToken goToRToken))
                        {
                            if (dictionaryToken.TryGet(NameToken.F, out IToken remoteFileToken))
                            {
                                currentNode.ExternalLink = GetString(NameToken.F, remoteFileToken);
                            }
                            HandleGoToRAction(goToRToken, ref currentNode);
                        }
                        else
                        {
                            throw new ArgumentException("BookmarksProvider.GetActions(): Could not find token 'D' in 'GoToR'.");
                        }
                    }
                    else
                    {
                        currentNode.IsExternal = true;
                        log.Debug("BookmarksProvider.GetActions(): Ignoring unknown token '" + sToken.Data + "'.");
                    }
                }
                else
                {
                    throw new ArgumentException("BookmarksProvider.GetActions(): Could not find token 'S' in 'Action'.");
                }
            }
            else if (actionToken is IndirectReferenceToken indirectReferenceToken)
            {
                var tempToken = structure.GetObject(indirectReferenceToken.Data).Data;
                if (tempToken is DictionaryToken dictionaryAction)
                {
                    GetActions(dictionaryAction, ref currentNode);
                }
                else
                {
                    throw new NotImplementedException("BookmarksProvider.GetActions(): " + nameof(tempToken) + " of type " + tempToken.GetType() + ".");
                }
            }
            else
            {
                throw new NotImplementedException("BookmarksProvider.GetActions(): " + nameof(actionToken) + " of type " + actionToken.GetType() + ".");
            }
        }

        private void HandleGoToRAction(IToken goToRToken, ref BookmarkNode currentNode)
        {
            currentNode.IsExternal = true;
            HandleGoToAction(goToRToken, ref currentNode);
        }

        private void HandleGoToAction(IToken goToToken, ref BookmarkNode currentNode)
        {
            if (goToToken is ArrayToken arrayToken)
            {
                GetDestination(arrayToken, currentNode);
            }
            else if (goToToken is IDataToken<string> stringToken)
            {
                GetNamedDestination(stringToken, ref currentNode);
                if (currentNode.PageNumber == 0)
                {
                    currentNode.PageNumber = ParsePageNumber(stringToken.Data);
                }
            }
            else if (goToToken is IndirectReferenceToken indirectReferenceToken)
            {
                HandleGoToAction(structure.GetObject(indirectReferenceToken.Data).Data, ref currentNode);
            }
            else
            {
                throw new NotImplementedException("BookmarksProvider.HandleGoToAction(): " + nameof(goToToken) + " of type " + goToToken.GetType());
            }
        }
        #endregion
    }
}
