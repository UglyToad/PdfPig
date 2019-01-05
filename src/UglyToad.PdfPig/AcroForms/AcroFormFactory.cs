namespace UglyToad.PdfPig.AcroForms
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Content;
    using Exceptions;
    using Fields;
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

            var fields = new Dictionary<IndirectReference, AcroFieldBase>(fieldsArray.Length);

            foreach (var fieldToken in fieldsArray.Data)
            {
                if (!(fieldToken is IndirectReferenceToken fieldReferenceToken))
                {
                    throw new PdfDocumentFormatException($"The fields array should only contain indirect references, instead got: {fieldToken}.");
                }

                var fieldDictionary = DirectObjectFinder.Get<DictionaryToken>(fieldToken, tokenScanner);

                var field = GetAcroField(fieldDictionary);

                fields[fieldReferenceToken.Data] = field;
            }

            return new AcroForm(acroDictionary, signatureFlags, needAppearances, fields);
        }

        private AcroFieldBase GetAcroField(DictionaryToken fieldDictionary)
        {
            fieldDictionary.TryGet(NameToken.Ft, out NameToken fieldType);
            fieldDictionary.TryGet(NameToken.Ff, out NumericToken fieldFlagsToken);

            var kids = new List<DictionaryToken>();

            if (fieldDictionary.TryGetOptionalTokenDirect(NameToken.Kids, tokenScanner, out ArrayToken kidsToken))
            {
                foreach (var kid in kidsToken.Data)
                {
                    if (!(kid is IndirectReferenceToken kidReferenceToken))
                    {
                        throw new PdfDocumentFormatException($"AcroForm kids should only contain indirect reference, instead got: {kid}.");
                    }

                    var kidObject = tokenScanner.Get(kidReferenceToken.Data);

                    if (kidObject.Data is DictionaryToken kidDictionaryToken)
                    {
                        kids.Add(kidDictionaryToken);
                    }
                    else
                    {
                        throw new PdfDocumentFormatException($"Unexpected type of kid in AcroForm field. Expected dictionary but got: {kidObject.Data}.");
                    }
                }
            }

            fieldDictionary.TryGetOptionalStringDirect(NameToken.T, tokenScanner, out var partialFieldName);
            fieldDictionary.TryGetOptionalStringDirect(NameToken.Tu, tokenScanner, out var alternateFieldName);
            fieldDictionary.TryGetOptionalStringDirect(NameToken.Tm, tokenScanner, out var mappingName);
            fieldDictionary.TryGet(NameToken.Parent, out IndirectReferenceToken parentReferenceToken);
            var information = new AcroFieldCommonInformation(parentReferenceToken?.Data, partialFieldName, alternateFieldName, mappingName);


            var fieldFlags = (uint) (fieldFlagsToken?.Long ?? 0);

            AcroFieldBase result;
            if (fieldType == null)
            {
                var children = new List<AcroFieldBase>();
                foreach (var kid in kids)
                {
                    var kidField = GetAcroField(kid);
                    children.Add(kidField);
                }

                result = new NonTerminalAcroField(fieldDictionary, "Non-Terminal Field", fieldFlags, information, children);
            }
            else if (fieldType == NameToken.Btn)
            {
                var buttonFlags = (AcroButtonFieldFlags)fieldFlags;

                if (buttonFlags.HasFlag(AcroButtonFieldFlags.Radio))
                {
                    var field = new AcroRadioButtonsField(fieldDictionary, fieldType, buttonFlags, information);
                    result = field;
                }
                else if (buttonFlags.HasFlag(AcroButtonFieldFlags.PushButton))
                {
                    var field = new AcroPushButtonField(fieldDictionary, fieldType, buttonFlags, information);
                    result = field;
                }
                else
                {
                    if (!fieldDictionary.TryGetOptionalTokenDirect(NameToken.V, tokenScanner, out NameToken valueToken))
                    {
                        valueToken = NameToken.Off;
                    }

                    var field = new AcroCheckboxField(fieldDictionary, fieldType, buttonFlags, information, valueToken);
                    result = field;
                }
            }
            else if (fieldType == NameToken.Tx)
            {
                result = GetTextField(fieldDictionary, fieldType, fieldFlags, information);
            }
            else if (fieldType == NameToken.Ch)
            {
                result = GetChoiceField(fieldDictionary, fieldType, fieldFlags, information);
            }
            else if (fieldType == NameToken.Sig)
            {
                var field = new AcroSignatureField(fieldDictionary, fieldType, fieldFlags, information);
                result = field;
            }
            else
            {
                throw new PdfDocumentFormatException($"Unexpected type for field in AcroForm: {fieldType}.");
            }

            return result;
        }

        private AcroFieldBase GetTextField(DictionaryToken fieldDictionary, NameToken fieldType, uint fieldFlags, AcroFieldCommonInformation information)
        {
            var textFlags = (AcroTextFieldFlags)fieldFlags;

            var textValue = default(string);
            if (fieldDictionary.TryGet(NameToken.V, out var textValueToken))
            {
                if (DirectObjectFinder.TryGet(textValueToken, tokenScanner, out StringToken valueStringToken))
                {
                    textValue = valueStringToken.Data;
                }
                else if (DirectObjectFinder.TryGet(textValueToken, tokenScanner, out HexToken valueHexToken))
                {
                    textValue = valueHexToken.Data;
                }
                else if (DirectObjectFinder.TryGet(textValueToken, tokenScanner, out StreamToken valueStreamToken))
                {
                    textValue = OtherEncodings.BytesAsLatin1String(valueStreamToken.Decode(filterProvider).ToArray());
                }
            }

            var maxLength = default(int?);
            if (fieldDictionary.TryGetOptionalTokenDirect(NameToken.MaxLen, tokenScanner, out NumericToken maxLenToken))
            {
                maxLength = maxLenToken.Int;
            }

            var field = new AcroTextField(fieldDictionary, fieldType, textFlags, information, textValue, maxLength);

            return field;
        }

        private AcroFieldBase GetChoiceField(DictionaryToken fieldDictionary, NameToken fieldType, uint fieldFlags, AcroFieldCommonInformation information)
        {
            var selectedOptions = Array.Empty<string>();
            if (fieldDictionary.TryGet(NameToken.V, out var valueToken))
            {
                if (DirectObjectFinder.TryGet(valueToken, tokenScanner, out StringToken valueString))
                {
                    selectedOptions = new[] {valueString.Data};
                }
                else if (DirectObjectFinder.TryGet(valueToken, tokenScanner, out HexToken valueHex))
                {
                    selectedOptions = new[] {valueHex.Data};

                }
                else if (DirectObjectFinder.TryGet(valueToken, tokenScanner, out ArrayToken valueArray))
                {
                    selectedOptions = new string[valueArray.Length];
                    for (var i = 0; i < valueArray.Length; i++)
                    {
                        var valueOptToken = valueArray.Data[i];

                        if (DirectObjectFinder.TryGet(valueOptToken, tokenScanner, out StringToken valueOptString))
                        {
                            selectedOptions[i] = valueOptString.Data;
                        }
                        else if (DirectObjectFinder.TryGet(valueOptToken, tokenScanner, out HexToken valueOptHex))
                        {
                            selectedOptions[i] = valueOptHex.Data;
                        }
                    }
                }
            }

            var selectedIndices = default(int[]);
            if (fieldDictionary.TryGetOptionalTokenDirect(NameToken.I, tokenScanner, out ArrayToken indicesArray))
            {
                selectedIndices = new int[indicesArray.Length];
                for (var i = 0; i < indicesArray.Data.Count; i++)
                {
                    var token = indicesArray.Data[i];
                    var numericToken = DirectObjectFinder.Get<NumericToken>(token, tokenScanner);
                    selectedIndices[i] = numericToken.Int;
                }
            }

            var options = new List<AcroChoiceOption>();
            if (fieldDictionary.TryGetOptionalTokenDirect(NameToken.Opt, tokenScanner, out ArrayToken optionsArrayToken))
            {
                for (var i = 0; i < optionsArrayToken.Data.Count; i++)
                {
                    var optionToken = optionsArrayToken.Data[i];
                    if (DirectObjectFinder.TryGet(optionToken, tokenScanner, out StringToken optionStringToken))
                    {
                        var name = optionStringToken.Data;
                        var isSelected = IsChoiceSelected(selectedOptions, selectedIndices, i, name);
                        options.Add(new AcroChoiceOption(i, isSelected, optionStringToken.Data));
                    }
                    else if (DirectObjectFinder.TryGet(optionToken, tokenScanner, out HexToken optionHexToken))
                    {
                        var name = optionHexToken.Data;
                        var isSelected = IsChoiceSelected(selectedOptions, selectedIndices, i, name);
                        options.Add(new AcroChoiceOption(i, isSelected, optionHexToken.Data));
                    }
                    else if (DirectObjectFinder.TryGet(optionToken, tokenScanner, out ArrayToken optionArrayToken))
                    {
                        if (optionArrayToken.Length != 2)
                        {
                            throw new PdfDocumentFormatException($"An option array containing array elements should contain 2 strings, instead got: {optionArrayToken}.");
                        }

                        string exportValue;
                        if (DirectObjectFinder.TryGet(optionArrayToken.Data[0], tokenScanner, out StringToken exportValueStringToken))
                        {
                            exportValue = exportValueStringToken.Data;
                        }
                        else if (DirectObjectFinder.TryGet(optionArrayToken.Data[0], tokenScanner, out HexToken exportValueHexToken))
                        {
                            exportValue = exportValueHexToken.Data;
                        }
                        else
                        {
                            throw new PdfDocumentFormatException($"An option array array element's first value should be the export value string, instead got: {optionArrayToken.Data[0]}.");
                        }

                        string name;
                        if (DirectObjectFinder.TryGet(optionArrayToken.Data[1], tokenScanner, out StringToken nameStringToken))
                        {
                            name = nameStringToken.Data;
                        }
                        else if (DirectObjectFinder.TryGet(optionArrayToken.Data[1], tokenScanner, out HexToken nameHexToken))
                        {
                            name = nameHexToken.Data;
                        }
                        else
                        {
                            throw new PdfDocumentFormatException($"An option array array element's second value should be the option name string, instead got: {optionArrayToken.Data[1]}.");
                        }

                        var isSelected = IsChoiceSelected(selectedOptions, selectedIndices, i, name);
                        options.Add(new AcroChoiceOption(i, isSelected, name, exportValue));
                    }
                    else
                    {
                        throw new PdfDocumentFormatException($"An option array should contain either strings or 2 element arrays, instead got: {optionToken}.");
                    }
                }
            }
            
            var choiceFlags = (AcroChoiceFieldFlags)fieldFlags;
            
            if (choiceFlags.HasFlag(AcroChoiceFieldFlags.Combo))
            {
                var field = new AcroComboBoxField(fieldDictionary, fieldType, choiceFlags, information, options, selectedOptions, selectedIndices);
                return field;
            }

            var topIndex = default(int?);
            if (fieldDictionary.TryGetOptionalTokenDirect(NameToken.Ti, tokenScanner, out NumericToken topIndexToken))
            {
                topIndex = topIndexToken.Int;
            }

            return new AcroListBoxField(fieldDictionary, fieldType, choiceFlags, information, options, selectedOptions, selectedIndices, topIndex);
        }

        private static bool IsChoiceSelected(IReadOnlyList<string> selectedOptionNames, IReadOnlyList<int> selectedOptionIndices, int index, string name)
        {
            if (selectedOptionNames.Count == 0)
            {
                return false;
            }

            for (var i = 0; i < selectedOptionNames.Count; i++)
            {
                var optionName = selectedOptionNames[i];

                if (optionName != name)
                {
                    continue;
                }

                if (selectedOptionIndices == null)
                {
                    return true;
                }

                if (selectedOptionIndices.Contains(index))
                {
                    return true;
                }

                return false;
            }

            return false;
        }
    }
}