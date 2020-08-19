namespace UglyToad.PdfPig.AcroForms
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Content;
    using Core;
    using CrossReference;
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
        private static readonly HashSet<NameToken> InheritableFields = new HashSet<NameToken>
        {
            NameToken.Ft,
            NameToken.Ff,
            NameToken.V,
            NameToken.Dv,
            NameToken.Aa
        };

        private readonly IPdfTokenScanner tokenScanner;
        private readonly IFilterProvider filterProvider;
        private readonly CrossReferenceTable crossReferenceTable;

        public AcroFormFactory(IPdfTokenScanner tokenScanner, IFilterProvider filterProvider, CrossReferenceTable crossReferenceTable)
        {
            this.tokenScanner = tokenScanner ?? throw new ArgumentNullException(nameof(tokenScanner));
            this.filterProvider = filterProvider ?? throw new ArgumentNullException(nameof(filterProvider));
            this.crossReferenceTable = crossReferenceTable ?? throw new ArgumentNullException(nameof(crossReferenceTable));
        }

        /// <summary>
        /// Retrieve the <see cref="AcroForm"/> from the document, if applicable.
        /// </summary>
        /// <returns>The <see cref="AcroForm"/> if the document contains one.</returns>
        [CanBeNull]
        public AcroForm GetAcroForm(Catalog catalog)
        {
            if (!catalog.CatalogDictionary.TryGet(NameToken.AcroForm, out var acroRawToken) )
            {
                return null;
            }

            if (!DirectObjectFinder.TryGet(acroRawToken, tokenScanner, out DictionaryToken acroDictionary))
            {
                var fieldsRefs = new List<IndirectReferenceToken>();

                // Invalid reference, try constructing the form from a Brute Force scan.
                foreach (var reference in crossReferenceTable.ObjectOffsets.Keys)
                {
                    var referenceToken = new IndirectReferenceToken(reference);
                    if (!DirectObjectFinder.TryGet(referenceToken, tokenScanner, out DictionaryToken dict))
                    {
                        continue;
                    }

                    if (dict.TryGet(NameToken.Kids, tokenScanner, out ArrayToken _) && dict.TryGet(NameToken.T, tokenScanner, out StringToken _))
                    {
                        fieldsRefs.Add(referenceToken);
                    }
                }

                if (fieldsRefs.Count == 0)
                {
                    return null;
                }

                acroDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.Fields, new ArrayToken(fieldsRefs) }
                });
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
            
            if (!acroDictionary.TryGet(NameToken.Fields, tokenScanner, out ArrayToken fieldsArray))
            {
                return null;
            }

            var fields = new Dictionary<IndirectReference, AcroFieldBase>(fieldsArray.Length);

            foreach (var fieldToken in fieldsArray.Data)
            {
                if (!(fieldToken is IndirectReferenceToken fieldReferenceToken))
                {
                    throw new PdfDocumentFormatException($"The fields array should only contain indirect references, instead got: {fieldToken}.");
                }

                var fieldDictionary = DirectObjectFinder.Get<DictionaryToken>(fieldToken, tokenScanner);

                var field = GetAcroField(fieldDictionary, catalog, new List<DictionaryToken>(0));

                fields[fieldReferenceToken.Data] = field;
            }

            return new AcroForm(acroDictionary, signatureFlags, needAppearances, fields);
        }

        private AcroFieldBase GetAcroField(DictionaryToken fieldDictionary, Catalog catalog,
            IReadOnlyList<DictionaryToken> parentDictionaries)
        {
            var (combinedFieldDictionary, inheritsValue) = CreateInheritedDictionary(fieldDictionary, parentDictionaries);

            fieldDictionary = combinedFieldDictionary;

            fieldDictionary.TryGet(NameToken.Ft, tokenScanner, out NameToken fieldType);
            fieldDictionary.TryGet(NameToken.Ff, tokenScanner, out NumericToken fieldFlagsToken);

            var kids = new List<(bool hasParent, DictionaryToken dictionary)>();

            if (fieldDictionary.TryGetOptionalTokenDirect(NameToken.Kids, tokenScanner, out ArrayToken kidsToken))
            {
                foreach (var kid in kidsToken.Data)
                {
                    if (!(kid is IndirectReferenceToken kidReferenceToken))
                    {
                        throw new PdfDocumentFormatException($"AcroForm kids should only contain indirect reference, instead got: {kid}.");
                    }

                    var kidObject = tokenScanner.Get(kidReferenceToken.Data);
                    if (kidObject is null)
                    {
                        throw new InvalidOperationException($"Could not find the object with reference: {kidReferenceToken.Data}.");
                    }

                    if (kidObject.Data is DictionaryToken kidDictionaryToken)
                    {
                        var hasParent = kidDictionaryToken.TryGet(NameToken.Parent, out IndirectReferenceToken _);
                        kids.Add((hasParent, kidDictionaryToken));
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

            int? pageNumber = null;
            if (fieldDictionary.TryGet(NameToken.P, tokenScanner, out IndirectReferenceToken pageReference))
            {
                pageNumber = catalog.GetPageByReference(pageReference.Data)?.PageNumber;
            }

            PdfRectangle? bounds = null;
            if (fieldDictionary.TryGet(NameToken.Rect, tokenScanner, out ArrayToken rectArray) && rectArray.Length == 4)
            {
                bounds = rectArray.ToRectangle(tokenScanner);
            }

            var newParentDictionaries = new List<DictionaryToken>(parentDictionaries) {fieldDictionary};

            var children = new List<AcroFieldBase>(kids.Count);
            foreach (var kid in kids)
            {
                if (!kid.hasParent)
                {
                    // Is a widget annotation dictionary.
                    continue;
                }

                children.Add(GetAcroField(kid.dictionary, catalog, newParentDictionaries));
            }

            var fieldFlags = (uint) (fieldFlagsToken?.Long ?? 0);

            AcroFieldBase result;
            if (fieldType == null)
            {
                result = new AcroNonTerminalField(fieldDictionary, "Non-Terminal Field", fieldFlags, information, AcroFieldType.Unknown, children);
            }
            else if (fieldType == NameToken.Btn)
            {
                var buttonFlags = (AcroButtonFieldFlags)fieldFlags;

                if (buttonFlags.HasFlag(AcroButtonFieldFlags.Radio))
                {
                    if (children.Count > 0)
                    {
                        result = new AcroRadioButtonsField(fieldDictionary, fieldType, buttonFlags, information,
                            children);
                    }
                    else
                    {
                        var (isChecked, valueToken) = GetCheckedState(fieldDictionary, inheritsValue);

                        var field = new AcroRadioButtonField(fieldDictionary, fieldType, buttonFlags, information,
                            pageNumber,
                            bounds,
                            valueToken,
                            isChecked);
                        
                        result = field;
                    }
                }
                else if (buttonFlags.HasFlag(AcroButtonFieldFlags.PushButton))
                {
                    var field = new AcroPushButtonField(fieldDictionary, fieldType, buttonFlags, information,
                        pageNumber,
                        bounds);
                    result = field;
                }
                else
                {
                    if (children.Count > 0)
                    {
                        result = new AcroCheckboxesField(fieldDictionary, fieldType, buttonFlags, information,
                            children);
                    }
                    else
                    {
                        var (isChecked, valueToken) = GetCheckedState(fieldDictionary, inheritsValue);
                        var field = new AcroCheckboxField(fieldDictionary, fieldType, buttonFlags, information,
                            valueToken,
                            isChecked,
                            pageNumber,
                            bounds);

                        result = field;
                    }
                }
            }
            else if (fieldType == NameToken.Tx)
            {
                result = GetTextField(fieldDictionary, fieldType, fieldFlags, information, pageNumber, bounds);
            }
            else if (fieldType == NameToken.Ch)
            {
                result = GetChoiceField(fieldDictionary, fieldType, fieldFlags, information, 
                    pageNumber,
                    bounds);
            }
            else if (fieldType == NameToken.Sig)
            {
                var field = new AcroSignatureField(fieldDictionary, fieldType, fieldFlags, information,
                    pageNumber,
                    bounds);
                result = field;
            }
            else
            {
                throw new PdfDocumentFormatException($"Unexpected type for field in AcroForm: {fieldType}.");
            }

            return result;
        }

        private AcroFieldBase GetTextField(DictionaryToken fieldDictionary, NameToken fieldType, uint fieldFlags, 
            AcroFieldCommonInformation information, 
            int? pageNumber,
            PdfRectangle? bounds)
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

            var field = new AcroTextField(fieldDictionary, fieldType, textFlags, information, 
                textValue, 
                maxLength,
                pageNumber,
                bounds);

            return field;
        }

        private AcroFieldBase GetChoiceField(DictionaryToken fieldDictionary, NameToken fieldType,
            uint fieldFlags, 
            AcroFieldCommonInformation information,
            int? pageNumber,
            PdfRectangle? bounds)
        {
            var selectedOptions = EmptyArray<string>.Instance;
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
                var field = new AcroComboBoxField(fieldDictionary, fieldType, choiceFlags, information,
                    options,
                    selectedOptions, 
                    selectedIndices,
                    pageNumber,
                    bounds);
                return field;
            }

            var topIndex = default(int?);
            if (fieldDictionary.TryGetOptionalTokenDirect(NameToken.Ti, tokenScanner, out NumericToken topIndexToken))
            {
                topIndex = topIndexToken.Int;
            }

            return new AcroListBoxField(fieldDictionary, fieldType, choiceFlags, information, 
                options,
                selectedOptions, 
                selectedIndices,
                topIndex,
                pageNumber,
                bounds);
        }

        private (bool isChecked, NameToken stateName) GetCheckedState(DictionaryToken fieldDictionary, bool inheritsValue)
        {
            var isChecked = false;
            if (!fieldDictionary.TryGetOptionalTokenDirect(NameToken.V, tokenScanner, out NameToken valueToken))
            {
                valueToken = NameToken.Off;
            }
            else if (inheritsValue && fieldDictionary.TryGet(NameToken.As, tokenScanner, out NameToken appearanceStateName))
            {
                // The parent field's V entry holds a name object corresponding to the 
                // appearance state of whichever child field is currently in the on state.
                isChecked = appearanceStateName.Equals(valueToken);
                valueToken = appearanceStateName;
            }
            else
            {
                isChecked = !string.Equals(valueToken.Data, NameToken.Off, StringComparison.OrdinalIgnoreCase);
            }

            return (isChecked, valueToken);
        }
        
        private static (DictionaryToken dictionary, bool inheritsValue) CreateInheritedDictionary(DictionaryToken fieldDictionary, 
            IReadOnlyList<DictionaryToken> parents)
        {
            if (parents.Count == 0)
            {
                return (fieldDictionary, false);
            }

            var inheritsValue = false;

            var inheritedDictionary = new Dictionary<NameToken, IToken>();
            foreach (var parent in parents)
            {
                foreach (var kvp in parent.Data)
                {
                    var key = NameToken.Create(kvp.Key);
                    if (InheritableFields.Contains(key))
                    {
                        inheritedDictionary[key] = kvp.Value;

                        if (NameToken.V.Equals(key))
                        {
                            inheritsValue = true;
                        }
                    }
                }
            }

            foreach (var kvp in fieldDictionary.Data)
            {
                var key = NameToken.Create(kvp.Key);
                inheritedDictionary[key] = kvp.Value;
                if (NameToken.V.Equals(key))
                {
                    inheritsValue = false;
                }
            }

            return (new DictionaryToken(inheritedDictionary), inheritsValue);
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