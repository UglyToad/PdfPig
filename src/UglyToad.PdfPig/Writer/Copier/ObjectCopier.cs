namespace UglyToad.PdfPig.Writer.Copier
{
    using System;
    using System.Collections.Generic;
    using PdfPig;
    using Tokenization.Scanner;
    using Tokens;
    using Writer;

    /// <inheritdoc/>
    internal class ObjectCopier : IObjectCopier
    {
        private readonly PdfStreamWriter pdfStream;

        private readonly Dictionary<IndirectReferenceToken, IndirectReferenceToken> newReferenceMap;

        /// <inheritdoc/>
        public ObjectCopier(PdfStreamWriter destinationStream)
        {
            pdfStream = destinationStream ?? throw new ArgumentNullException(nameof(destinationStream));
            newReferenceMap = new Dictionary<IndirectReferenceToken, IndirectReferenceToken>();
        }

        /// <inheritdoc/>
        public IToken CopyObject(IToken sourceToken, PdfDocument sourceDocument)
        {
            IToken tokenScanner(IndirectReferenceToken referenceToken)
            {
                var objToken = sourceDocument.Structure.GetObject(referenceToken.Data);
                return objToken.Data;
            }

            return CopyObject(sourceToken, tokenScanner);
        }

        /// <inheritdoc/>
        public IToken CopyObject(IToken sourceToken, IPdfTokenScanner tokenScanner)
        {
            IToken tokenGetter(IndirectReferenceToken referenceToken)
            {
                var objToken = tokenScanner.Get(referenceToken.Data);
                return objToken.Data;
            }

            return CopyObject(sourceToken, tokenGetter);
        }

        /// <inheritdoc/>
        public virtual IToken CopyObject(IToken sourceToken, Func<IndirectReferenceToken, IToken> tokenScanner)
        {
            // This token need to be deep copied, because they could contain reference. So we have to update them.
            switch (sourceToken)
            {
                case DictionaryToken dictionaryToken:
                    {
                        var newContent = new Dictionary<NameToken, IToken>();
                        foreach (var setPair in dictionaryToken.Data)
                        {
                            var name = setPair.Key;
                            var token = setPair.Value;

                            newContent.Add(NameToken.Create(name), CopyObject(token, tokenScanner));
                        }

                        return new DictionaryToken(newContent);
                    }
                case ArrayToken arrayToken:
                    {
                        var newArray = new List<IToken>(arrayToken.Length);
                        foreach (var token in arrayToken.Data)
                        {
                            newArray.Add(CopyObject(token, tokenScanner));
                        }

                        return new ArrayToken(newArray);
                    }
                case IndirectReferenceToken referenceToken:
                    {
                        if (TryGetNewReference(referenceToken, out var newReferenceToken))
                        {
                            return newReferenceToken;
                        }

                        var referencedToken = tokenScanner(referenceToken);
                        var newReferencedToken = CopyObject(referencedToken, tokenScanner);

                        var newToken = WriteToken(newReferencedToken);
                        SetNewReference(referenceToken, newToken);
                        return newToken;
                    }

                case StreamToken streamToken:
                    {
                        var properties = CopyObject(streamToken.StreamDictionary, tokenScanner);
                        var bytes = streamToken.Data;
                        return new StreamToken(properties as DictionaryToken, bytes);
                    }

                case ObjectToken _:
                    {

                        // This is because, since we don't write token directly to the stream. So we can't know the offset.
                        // The token would be invalid. Although I don't think the copy of an object token would ever happen
                        throw new NotSupportedException("Copying a Object token is not supported");
                    }
            }

            return sourceToken;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sourceReferenceToken"></param>
        /// <param name="newReferenceToken"></param>
        /// <returns></returns>
        public virtual bool TryGetNewReference(IndirectReferenceToken sourceReferenceToken, out IndirectReferenceToken newReferenceToken)
        {
            newReferenceToken = default;
            foreach (var referenceSet in newReferenceMap)
            {
                if (!referenceSet.Key.Equals(sourceReferenceToken))
                {
                    continue;
                }

                newReferenceToken = referenceSet.Value;
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public virtual void ClearReference()
        {
            newReferenceMap.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="oldToken"></param>
        /// <param name="newToken"></param>
        public void SetNewReference(IndirectReferenceToken oldToken, IndirectReferenceToken newToken)
        {
            newReferenceMap.Add(oldToken, newToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int ReserveTokenNumber()
        {
            return pdfStream.ReserveNumber();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="reservedNumber"></param>
        /// <returns></returns>
        public IndirectReferenceToken WriteToken(IToken token, int? reservedNumber = null)
        {
            return pdfStream.WriteToken(token, reservedNumber);
        }
    }
}
