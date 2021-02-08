namespace UglyToad.PdfPig.Writer
{
    using Content;
    using Core;
    using Parser.Parts;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Tokenization.Scanner;
    using Tokens;

	internal static class WriterUtil
    {
        public static Dictionary<string, IToken> GetOrCreateDict(this Dictionary<NameToken, IToken> dict,  NameToken key)
        {
            if (dict.ContainsKey(key))
            {
                var item = dict[key];
                if (!(item is DictionaryToken dt))
                {
                    throw new ApplicationException("Expected dictionary token, got " + item.GetType());
                }

                if (dt.Data is Dictionary<string, IToken> mutable)
                {
                    return mutable;
                }

                mutable = dt.Data.
                    ToDictionary(x => x.Key, x => x.Value);
                dict[key] = DictionaryToken.With(mutable);
                return mutable;
            }

            var created = new Dictionary<string, IToken>();
            dict[key] = DictionaryToken.With(created);
            return created;
        }

        public static Dictionary<string, IToken> GetOrCreateDict(this Dictionary<string, IToken> dict,  string key)
        {
            if (dict.ContainsKey(key))
            {
                var item = dict[key];
                if (!(item is DictionaryToken dt))
                {
                    throw new ApplicationException("Expected dictionary token, got " + item.GetType());
                }

                if (dt.Data is Dictionary<string, IToken> mutable)
                {
                    return mutable;
                }

                mutable = dt.Data.
                    ToDictionary(x => x.Key, x => x.Value);
                dict[key] = DictionaryToken.With(mutable);
                return mutable;
            }

            var created = new Dictionary<string, IToken>();
            dict[key] = DictionaryToken.With(created);
            return created;
        }
        /// <summary>
        /// The purpose of this method is to resolve indirect reference. That mean copy the reference's content to the new document's stream
        /// and replace the indirect reference with the correct/new one
        /// </summary>
        /// <param name="writer">PDF stream writer</param>
        /// <param name="tokenToCopy">Token to inspect for reference</param>
        /// <param name="tokenScanner">scanner get the content from the original document</param>
        /// <param name="referencesFromDocument">Map of previously copied tokens for original document.</param>
        /// <param name="callstack">Call stack of indirect references</param>
        /// <returns>A reference of the token that was copied. With all the reference updated</returns>
        public static IToken CopyToken(IPdfStreamWriter writer, IToken tokenToCopy, IPdfTokenScanner tokenScanner,
            IDictionary<IndirectReference, IndirectReferenceToken> referencesFromDocument, Dictionary<IndirectReference, IndirectReferenceToken> callstack=null)
        {
            if (callstack == null)
            {
                callstack = new Dictionary<IndirectReference, IndirectReferenceToken>();
            }

            // This token need to be deep copied, because they could contain reference. So we have to update them.
            switch (tokenToCopy)
            {
                case DictionaryToken dictionaryToken:
                {
                        var newContent = new Dictionary<NameToken, IToken>();
                        foreach (var setPair in dictionaryToken.Data)
                        {
                            var name = setPair.Key;
                            var token = setPair.Value;
                            newContent.Add(NameToken.Create(name), CopyToken(writer, token, tokenScanner, referencesFromDocument, callstack));
                        }

                        return new DictionaryToken(newContent);
                    }
                case ArrayToken arrayToken:
                    {
                        var newArray = new List<IToken>(arrayToken.Length);
                        foreach (var token in arrayToken.Data)
                        {
                            newArray.Add(CopyToken(writer, token, tokenScanner, referencesFromDocument, callstack));
                        }

                        return new ArrayToken(newArray);
                    }
                case IndirectReferenceToken referenceToken:
                    {
                        if (referencesFromDocument.TryGetValue(referenceToken.Data, out var newReferenceToken))
                        {
                            return newReferenceToken;
                        }

                        if (callstack.ContainsKey(referenceToken.Data) && callstack[referenceToken.Data] == null)
                        {
                            newReferenceToken = writer.ReserveObjectNumber();
                            callstack[referenceToken.Data] = newReferenceToken;
                            referencesFromDocument.Add(referenceToken.Data, newReferenceToken);
                            return newReferenceToken;
                        }

                        callstack.Add(referenceToken.Data, null);

                        // we add the token to referencesFromDocument to prevent stackoverflow on references cycles 
                        // newReferenceToken = context.ReserveNumberToken();
                        // callstack.Add(newReferenceToken.Data.ObjectNumber);
                        // referencesFromDocument.Add(referenceToken.Data, newReferenceToken);
                        // 
                        var tokenObject = DirectObjectFinder.Get<IToken>(referenceToken.Data, tokenScanner);
                        Debug.Assert(!(tokenObject is IndirectReferenceToken));
                        var result = CopyToken(writer, tokenObject, tokenScanner, referencesFromDocument, callstack);

                        if (callstack[referenceToken.Data] != null)
                        {
                            return writer.WriteToken(result, callstack[referenceToken.Data]);
                        }

                        newReferenceToken = writer.WriteToken(result);
                        referencesFromDocument.Add(referenceToken.Data, newReferenceToken);
                        return newReferenceToken;
                    }
                case StreamToken streamToken:
                {
                        var properties = CopyToken(writer, streamToken.StreamDictionary, tokenScanner, referencesFromDocument, callstack) as DictionaryToken;
                        Debug.Assert(properties != null);

                        var bytes = streamToken.Data;
                        return new StreamToken(properties, bytes);
                    }

                case ObjectToken _:
                    {
                        // Since we don't write token directly to the stream.
                        // We can't know the offset. Therefore the token would be invalid
                        throw new NotSupportedException("Copying a Object token is not supported");
                    }
            }

            return tokenToCopy;
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
    }

}

