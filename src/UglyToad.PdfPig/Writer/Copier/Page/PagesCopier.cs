namespace UglyToad.PdfPig.Writer.Copier.Page
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Core;
    using Tokens;

    /// <inheritdoc/>
    public class PagesCopier : IObjectCopier
    {
        private readonly ObjectCopier copier;

        private readonly IndirectReferenceToken rootPagesReferenceToken;

        /// <inheritdoc/>
        public PagesCopier(ObjectCopier mainCopier, IndirectReferenceToken rootPagesToken = null)
        {
            copier = mainCopier;
            rootPagesReferenceToken = rootPagesToken;
        }

        /// <inheritdoc/>
        public IToken CopyObject(IToken sourceToken, Func<IndirectReferenceToken, IToken> tokenScanner)
        {
            if (!(sourceToken is IndirectReferenceToken sourceReferenceToken))
            {
                return null;
            }

            // Check if this token haven't been copied before
            if (copier.TryGetNewReference(sourceReferenceToken, out var newReferenceToken))
            {
                return newReferenceToken;
            }

            // Make sure that we are copying a DictionaryToken
            var token = tokenScanner(sourceReferenceToken);
            if (!(token is DictionaryToken dictionaryToken))
            {
                return null;
            }

            // Make sure we are copying a `/Pages` Dictionary
            if (!dictionaryToken.TryGet(NameToken.Type, out var nameTypeToken) || !nameTypeToken.Equals(NameToken.Pages))
            {
                return null;
            }

            // We have reserve the reference before hand, because if we don't, we would fall in a loop.
            // The child `/Page` have a reference to the parent
            var tokenNumber = copier.ReserveTokenNumber();
            copier.SetNewReference(sourceReferenceToken, new IndirectReferenceToken(new IndirectReference(tokenNumber, 0)));

            // If `/Pages` is not the root page node, copy the token normally
            // We are testing to see if the `/Pages` have a parent || If the rootPagesReferenceToken is null
            if (dictionaryToken.TryGet(NameToken.Parent, out IndirectReferenceToken _) || rootPagesReferenceToken == null)
            {
                return copier.WriteToken(copier.CopyObject(dictionaryToken, tokenScanner), tokenNumber);
            }

            // If the token have no parent (root node) and we have a new parent
            return CopyPagesTree(dictionaryToken, tokenNumber, tokenScanner);
        }

        private IndirectReferenceToken CopyPagesTree(DictionaryToken pagesDictionary, int reservedNumber, Func<IndirectReferenceToken, IToken> tokenScanner)
        {
            Debug.Assert(rootPagesReferenceToken != null);

            var newContent = new Dictionary<NameToken, IToken>()
            {
                {NameToken.Parent, rootPagesReferenceToken}
            };

            foreach (var dataSet in pagesDictionary.Data)
            {
                newContent.Add(NameToken.Create(dataSet.Key), copier.CopyObject(dataSet.Value, tokenScanner));
            }

            var newPagesTree = new DictionaryToken(newContent);

            return copier.WriteToken(newPagesTree, reservedNumber);
        }

        /// <inheritdoc/>
        public void ClearReference()
        {
            // Nothing to do
        }
    }
}
