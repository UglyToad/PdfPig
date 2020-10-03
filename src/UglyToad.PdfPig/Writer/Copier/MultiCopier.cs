namespace UglyToad.PdfPig.Writer.Copier
{
    using System;
    using System.Collections.Generic;
    using Tokens;
    using Writer;

    /// <inheritdoc/>
    internal class MultiCopier : ObjectCopier
    {
        private readonly List<IObjectCopier> copiers;

        /// <inheritdoc/>
        public MultiCopier(PdfStreamWriter destinationStream) : base(destinationStream)
        {
            copiers = new List<IObjectCopier>();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="copier"></param>
        public void AddCopier(IObjectCopier copier)
        {
            copiers.Add(copier);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="copier"></param>
        /// <returns></returns>
        public bool RemoveCopier(IObjectCopier copier)
        {
            return copiers.Remove(copier);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<IObjectCopier> GetCopiers()
        {
            return copiers;
        }

        /// <inheritdoc/>
        public override IToken CopyObject(IToken sourceToken, Func<IndirectReferenceToken, IToken> tokenScanner)
        {
            // We give the token to the child copiers, to see if they have a better way of copying the token
            foreach (var copier in copiers)
            {
                var newToken = copier.CopyObject(sourceToken, tokenScanner);
                if (newToken != null)
                {
                    return newToken;
                }
            }

            // If the token did not found a suitable copier, let just do a simple copy of the token
            return base.CopyObject(sourceToken, tokenScanner);
        }

        /// <inheritdoc/>
        public override void ClearReference()
        {
            foreach (var copier in copiers)
            {
                copier.ClearReference();
            }

            base.ClearReference();
        }
    }
}