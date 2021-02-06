namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Tokens;

    internal interface IPdfStreamWriter : IDisposable
    {
        /// <summary>
        /// 
        /// </summary>
        Stream Stream { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        IndirectReferenceToken WriteToken(IToken token);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="token"></param>
        /// <param name="indirectReference"></param>
        /// <returns></returns>
        IndirectReferenceToken WriteToken(IToken token, IndirectReferenceToken indirectReference);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        IndirectReferenceToken ReserveObjectNumber();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="version"></param>
        void InitializePdf(decimal version);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="catalogReference"></param>
        /// <param name="documentInformationReference"></param>
        void CompletePdf(IndirectReferenceToken catalogReference, IndirectReferenceToken documentInformationReference=null);

    }
}
