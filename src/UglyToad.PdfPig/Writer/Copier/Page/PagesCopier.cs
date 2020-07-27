namespace UglyToad.PdfPig.Writer.Copier.Page
{
    using System;
    using Core;
    using Tokens;

    /// <inheritdoc/>
    public class PagesCopier : IObjectCopier
    {
        private readonly ObjectCopier copier;

        /// <inheritdoc/>
        public PagesCopier(ObjectCopier mainCopier)
        {
            copier = mainCopier;
        }

        /// <inheritdoc/>
        public IToken CopyObject(IToken sourceToken, Func<IndirectReferenceToken, IToken> tokenScanner)
        {
            if (!(sourceToken is IndirectReferenceToken sourceReferenceToken))
            {
                return null;
            }

            if (copier.TryGetNewReference(sourceReferenceToken, out var newReferenceToken))
            {
                return newReferenceToken;
            }

            var token = tokenScanner(sourceReferenceToken);
            if (!(token is DictionaryToken dictionaryToken))
            {
                return null;
            }

            if (!dictionaryToken.TryGet(NameToken.Type, out var nameTypeToken) || !nameTypeToken.Equals(NameToken.Pages))
            {
                return null;
            }

            // We have reserve the reference before hand, because if we don't, we would fall in a loop.
            // The child `/Page` have a reference to the parent
            var tokenNumber = copier.ReserveTokenNumber();
            copier.SetNewReference(sourceReferenceToken, new IndirectReferenceToken(new IndirectReference(tokenNumber, 0)));
            return copier.WriteToken(copier.CopyObject(dictionaryToken, tokenScanner), tokenNumber);
        }

        /// <inheritdoc/>
        public void ClearReference()
        {
            // Nothing to do
        }
    }
}
