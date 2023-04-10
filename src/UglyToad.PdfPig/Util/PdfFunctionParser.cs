namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using UglyToad.PdfPig.Filters;
    using UglyToad.PdfPig.Functions;
    using UglyToad.PdfPig.Parser.Parts;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;

    internal static class PdfFunctionParser
    {
        public static PdfFunction Create(IToken function, IPdfTokenScanner scanner, ILookupFilterProvider filterProvider)
        {
            DictionaryToken functionDictionary;
            StreamToken functionStream = null;

            if (function is StreamToken fs)
            {
                functionDictionary = fs.StreamDictionary;
                functionStream = new StreamToken(fs.StreamDictionary, fs.Decode(filterProvider, scanner));
            }
            else if (function is DictionaryToken fd)
            {
                functionDictionary = fd;
            }
            else
            {
                throw new ArgumentException(nameof(function));
            }

            int functionType = (functionDictionary.Data[NameToken.FunctionType] as NumericToken).Int;

            switch (functionType)
            {
                case 0:
                    if (functionStream == null)
                    {
                        throw new NotImplementedException("PdfFunctionType0 not stream");
                    }
                    return new PdfFunctionType0(functionStream);

                case 2:
                    return new PdfFunctionType2(functionDictionary);

                case 3:
                    var functions = new List<PdfFunction>();
                    if (functionDictionary.TryGet<ArrayToken>(NameToken.Functions, scanner, out var functionsToken))
                    {
                        foreach (IToken token in functionsToken.Data)
                        {
                            if (DirectObjectFinder.TryGet<StreamToken>(token, scanner, out var strTk))
                            {
                                functions.Add(Create(strTk, scanner, filterProvider));
                            }
                            else if (DirectObjectFinder.TryGet<DictionaryToken>(token, scanner, out var dicTk))
                            {
                                functions.Add(Create(dicTk, scanner, filterProvider));
                            }
                            else
                            {
                                throw new ArgumentException($"Could not find function for token '{token}' inside type 3 function.");
                            }
                        }
                    }
                    return new PdfFunctionType3(functionDictionary, functions);

                case 4:
                    if (functionStream == null)
                    {
                        throw new NotImplementedException("PdfFunctionType0 not stream");
                    }
                    return new PdfFunctionType4(functionStream);

                default:
                    throw new IOException("Error: Unknown function type " + functionType);
            }
        }
    }
}
