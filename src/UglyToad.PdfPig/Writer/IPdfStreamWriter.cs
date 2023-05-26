namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.IO;
    using Tokens;

    internal interface IPdfStreamWriter : IDisposable
    {
        /// <summary>
        /// Sets if the stream writer should attempt to deduplicate objects.
        /// May not have any affect if <see cref="IPdfStreamWriter"/> does not
        /// support deduplication.
        /// </summary>
        bool AttemptDeduplication { get; set; }

        /// <summary>
        /// The underlying stream used by the writer.
        /// </summary>
        Stream Stream { get; }

        /// <summary>
        /// Writes a single token to the stream.
        /// </summary>
        /// <param name="token">Token to write.</param>
        /// <returns>Indirect reference to the token.</returns>
        IndirectReferenceToken WriteToken(IToken token);

        /// <summary>
        /// Writes a token to a reserved object number.
        /// </summary>
        /// <param name="token">Token to write.</param>
        /// <param name="indirectReference">Reserved indirect reference.</param>
        /// <returns>Reserved indirect reference.</returns>
        IndirectReferenceToken WriteToken(IToken token, IndirectReferenceToken indirectReference);

        /// <summary>
        /// Reserves an object number for an object to be written.
        /// Useful with cyclic references where object number must be known before
        /// writing.
        /// </summary>
        /// <returns>A reserved indirect reference.</returns>
        IndirectReferenceToken ReserveObjectNumber();

        /// <summary>
        /// Initializes the PDF stream with pdf header.
        /// </summary>
        /// <param name="version">Version of PDF.</param>
        void InitializePdf(double version);

        /// <summary>
        /// Completes the PDF writing trailing PDF information.
        /// </summary>
        /// <param name="catalogReference">Indirect reference of catalog.</param>
        /// <param name="documentInformationReference">Reference to document information (optional)</param>
        void CompletePdf(IndirectReferenceToken catalogReference, IndirectReferenceToken documentInformationReference=null);
    }
}
