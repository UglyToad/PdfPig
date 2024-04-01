namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
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
            StreamToken? functionStream = null;

            if (DirectObjectFinder.TryGet(function, scanner, out StreamToken? fs))
            {
                functionDictionary = fs.StreamDictionary;
                functionStream = new StreamToken(fs.StreamDictionary, fs.Decode(filterProvider, scanner).ToArray());
            }
            else if (DirectObjectFinder.TryGet(function, scanner, out DictionaryToken? fd))
            {
                functionDictionary = fd;
            }
            else
            {
                throw new ArgumentException(nameof(function));
            }

            if (!functionDictionary.TryGet(NameToken.Domain, scanner, out ArrayToken? domain))
            {
                throw new ArgumentNullException(NameToken.Domain);
            }

            functionDictionary.TryGet(NameToken.Range, scanner, out ArrayToken? range);

            int functionType = ((NumericToken)functionDictionary.Data[NameToken.FunctionType]).Int;

            switch (functionType)
            {
                case 0:
                    if (functionStream is null)
                    {
                        throw new NotImplementedException("PdfFunctionType0 not stream");
                    }
                    return CreatePdfFunctionType0(functionStream, domain, range!, scanner);

                case 2:
                    return CreatePdfFunctionType2(functionDictionary, domain, range, scanner);

                case 3:
                    return CreatePdfFunctionType3(functionDictionary, domain, range, scanner, filterProvider);

                case 4:
                    if (functionStream is null)
                    {
                        throw new NotImplementedException("PdfFunctionType4 not stream");
                    }
                    return CreatePdfFunctionType4(functionStream, domain, range!, scanner);

                default:
                    throw new IOException("Error: Unknown function type " + functionType);
            }
        }

        private static PdfFunctionType0 CreatePdfFunctionType0(StreamToken functionStream, ArrayToken domain, ArrayToken range, IPdfTokenScanner scanner)
        {
            if (range is null)
            {
                throw new ArgumentException("Could not retrieve Range in type 0 function.");
            }

            if (!functionStream.StreamDictionary.TryGet<ArrayToken>(NameToken.Size, scanner, out var size))
            {
                throw new ArgumentNullException(NameToken.Size);
            }

            if (!functionStream.StreamDictionary.TryGet<NumericToken>(NameToken.BitsPerSample, scanner, out var bps))
            {
                throw new ArgumentNullException(NameToken.BitsPerSample);
            }

            int order = 1; // Default value
            if (functionStream.StreamDictionary.TryGet<NumericToken>(NameToken.Order, scanner, out var orderToken))
            {
                order = orderToken.Int;
            }

            if (!functionStream.StreamDictionary.TryGet<ArrayToken>(NameToken.Encode, scanner, out var encode) || encode is null)
            {
                // The default value is [0 (size[0]-1) 0 (size[1]-1) ...]
                var values = new List<NumericToken>();
                int sizeValuesSize = size.Length;
                for (int i = 0; i < sizeValuesSize; i++)
                {
                    values.Add(new NumericToken(0));
                    values.Add(new NumericToken(((NumericToken)size[i]).Int - 1));
                }
                encode = new ArrayToken(values);
            }

            if (!functionStream.StreamDictionary.TryGet<ArrayToken>(NameToken.Decode, scanner, out var decode) || decode is null)
            {
                // if decode is null, the default values are the range values
                decode = range;
            }

            return new PdfFunctionType0(functionStream, domain, range, size, bps.Int, order, encode, decode);
        }

        private static PdfFunctionType2 CreatePdfFunctionType2(DictionaryToken functionDictionary, ArrayToken domain, ArrayToken? range, IPdfTokenScanner scanner)
        {
            if (!functionDictionary.TryGet(NameToken.C0, scanner, out ArrayToken? array0) || array0.Length == 0)
            {
                array0 = new ArrayToken(new List<NumericToken>() { new NumericToken(0) }); // Default value: [0.0].
            }

            if (!functionDictionary.TryGet(NameToken.C1, scanner, out ArrayToken? array1) || array1.Length == 0)
            {
                array1 = new ArrayToken(new List<NumericToken>() { new NumericToken(1) }); // Default value: [1.0].
            }

            if (!functionDictionary.TryGet(NameToken.N, scanner, out NumericToken? exp))
            {
                throw new ArgumentNullException(NameToken.N);
            }

            return new PdfFunctionType2(functionDictionary, domain, range, array0, array1, exp.Double);
        }

        private static PdfFunctionType3 CreatePdfFunctionType3(
            DictionaryToken functionDictionary,
            ArrayToken domain,
            ArrayToken? range,
            IPdfTokenScanner scanner,
            ILookupFilterProvider filterProvider)
        {
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
            else
            {
                throw new ArgumentNullException(NameToken.Functions);
            }

            if (!functionDictionary.TryGet<ArrayToken>(NameToken.Bounds, scanner, out var bounds))
            {
                throw new ArgumentNullException(NameToken.Bounds);
            }

            if (!functionDictionary.TryGet<ArrayToken>(NameToken.Encode, scanner, out var encode))
            {
                throw new ArgumentNullException(NameToken.Encode);
            }

            return new PdfFunctionType3(functionDictionary, domain, range, functions, bounds, encode);
        }

        private static PdfFunctionType4 CreatePdfFunctionType4(StreamToken functionStream, ArrayToken domain, ArrayToken range, IPdfTokenScanner scanner)
        {
            if (range is null)
            {
                throw new ArgumentException("Could not retrieve Range in type 4 function.");
            }

            return new PdfFunctionType4(functionStream, domain, range);
        }
    }
}
