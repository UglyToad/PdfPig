namespace UglyToad.PdfPig.Writer.Copier
{
    using System;
    using System.Collections.Generic;
    using Page;
    using Tokens;
    using Writer;

    /// <inheritdoc/>
    public class MultiCopier : ObjectCopier
    {
        private readonly List<IObjectCopier> copiers;

        /// <inheritdoc/>
        public MultiCopier(PdfStreamWriter destinationStream) : base(destinationStream)
        {
            copiers = new List<IObjectCopier>() { new PagesCopier(this) };
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
            foreach (var copier in copiers)
            {
                var newToken = copier.CopyObject(sourceToken, tokenScanner);
                if (newToken != null)
                {
                    return newToken;
                }
            }

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
