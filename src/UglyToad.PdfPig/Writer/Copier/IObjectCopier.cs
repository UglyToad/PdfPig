namespace UglyToad.PdfPig.Writer.Copier
{
    using System;
    using Tokens;

    /// <summary>
    /// An interface for copying token
    /// </summary>
    internal interface IObjectCopier
    {
        /// <summary>
        /// Copy the token to the destination stream
        /// </summary>
        /// <param name="sourceToken">Token to copy</param>
        /// <param name="tokenScanner">Function to resolve indirect reference identified in the token to copy</param>
        /// <returns></returns>
        public IToken CopyObject(IToken sourceToken, Func<IndirectReferenceToken, IToken> tokenScanner);

        /// <summary>
        /// Clear the references of the previously copied object
        /// </summary>
        public void ClearReference();
    }
}