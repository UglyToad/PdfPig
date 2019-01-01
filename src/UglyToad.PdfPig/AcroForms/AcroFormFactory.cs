namespace UglyToad.PdfPig.AcroForms
{
    using System;
    using Content;
    using Exceptions;
    using Filters;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using Util;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Extracts the <see cref="AcroForm"/> from the document, if available.
    /// </summary>
    internal class AcroFormFactory
    {
        private readonly IPdfTokenScanner tokenScanner;
        private readonly IFilterProvider filterProvider;

        public AcroFormFactory(IPdfTokenScanner tokenScanner, IFilterProvider filterProvider)
        {
            this.tokenScanner = tokenScanner ?? throw new ArgumentNullException(nameof(tokenScanner));
            this.filterProvider = filterProvider ?? throw new ArgumentNullException(nameof(filterProvider));
        }

        /// <summary>
        /// Retrieve the <see cref="AcroForm"/> from the document, if applicable.
        /// </summary>
        /// <returns>The <see cref="AcroForm"/> if the document contains one.</returns>
        [CanBeNull]
        public AcroForm GetAcroForm(Catalog catalog)
        {
            if (!catalog.CatalogDictionary.TryGet(NameToken.AcroForm, out var acroRawToken) || !DirectObjectFinder.TryGet(acroRawToken, tokenScanner, out DictionaryToken acroDictionary))
            {
                return null;
            }

            var signatureFlags = (SignatureFlags)0;
            if (acroDictionary.TryGetOptionalTokenDirect(NameToken.SigFlags, tokenScanner, out NumericToken signatureToken))
            {
                signatureFlags = (SignatureFlags)signatureToken.Int;
            }

            var needAppearances = false;
            if (acroDictionary.TryGetOptionalTokenDirect(NameToken.NeedAppearances, tokenScanner, out BooleanToken appearancesToken))
            {
                needAppearances = appearancesToken.Data;
            }

            var calculationOrder = default(ArrayToken);
            acroDictionary.TryGetOptionalTokenDirect(NameToken.Co, tokenScanner, out calculationOrder);

            var formResources = default(DictionaryToken);
            acroDictionary.TryGetOptionalTokenDirect(NameToken.Dr, tokenScanner, out formResources);

            var da = default(string);
            if (acroDictionary.TryGetOptionalTokenDirect(NameToken.Da, tokenScanner, out StringToken daToken))
            {
                da = daToken.Data;
            }
            else if (acroDictionary.TryGetOptionalTokenDirect(NameToken.Da, tokenScanner, out HexToken daHexToken))
            {
                da = daHexToken.Data;
            }

            var q = default(int?);
            if (acroDictionary.TryGetOptionalTokenDirect(NameToken.Q, tokenScanner, out NumericToken qToken))
            {
                q = qToken.Int;
            }

            var fieldsToken = acroDictionary.Data[NameToken.Fields.Data];

            if (!DirectObjectFinder.TryGet(fieldsToken, tokenScanner, out ArrayToken fieldsArray))
            {
                throw new PdfDocumentFormatException($"Could not retrieve the fields array for an AcroForm: {acroDictionary}.");
            }

            foreach (var fieldToken in fieldsArray.Data)
            {
                var fieldDictionary = DirectObjectFinder.Get<DictionaryToken>(fieldToken, tokenScanner);


            }

            return new AcroForm(acroDictionary, signatureFlags, needAppearances);
        }
    }
}