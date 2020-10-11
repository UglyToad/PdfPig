namespace UglyToad.PdfPig.Function
{
    using System;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// 3
    /// </summary>
    public class PdfFunctionType3 : PdfFunction
    {
        /// <summary>
        /// (Required) An array of k 1-input functions that shall make up the stitching function.
        /// The output dimensionality of all functions shall be the same, and compatible with the value of Range if Range is present.
        /// </summary>
        public PdfFunction[] Functions { get; }

        /// <summary>
        /// (Required) An array of k − 1 numbers that, in combination with Domain, shall define the intervals to which each function from the Functions array shall apply.
        /// Bounds elements shall be in order of increasing value, and each value shall be within the domain defined by Domain.
        /// </summary>
        public ArrayToken Bounds { get; }

        /// <summary>
        /// (Required) An array of 2 × k numbers that, taken in pairs, shall map each subset of the domain defined by Domain and the Bounds array to the domain of the corresponding function.
        /// </summary>
        public ArrayToken Encode { get; }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public override int FunctionType => 3;

        /// <summary>
        /// 3
        /// </summary>
        /// <param name="functionDictionary"></param>
        /// <param name="pdfTokenScanner"></param>
        public PdfFunctionType3(DictionaryToken functionDictionary, IPdfTokenScanner pdfTokenScanner)
            : base(functionDictionary, pdfTokenScanner)
        {
            if (functionDictionary.TryGet<ArrayToken>(NameToken.Functions, pdfTokenScanner, out var functions))
            {
                Functions = new PdfFunction[functions.Data.Count];

                for (int i = 0; i < functions.Data.Count; i++)
                {
                    var token = functions.Data[i];

                    if (token is IndirectReferenceToken indirectReferenceToken)
                    {
                        var function = pdfTokenScanner.Get(indirectReferenceToken.Data);
                        if (function.Data is StreamToken streamToken)
                        {
                            Functions[i] = Parse(streamToken.StreamDictionary, pdfTokenScanner);
                        }
                        else if (function.Data is DictionaryToken dictionaryToken)
                        {
                            Functions[i] = Parse(dictionaryToken, pdfTokenScanner);
                        }
                    }
                    else if (token is StreamToken stream)
                    {
                        Functions[i] = Parse(stream.StreamDictionary, pdfTokenScanner);
                    }
                    else if (token is DictionaryToken dictionary)
                    {
                        Functions[i] = Parse(dictionary, pdfTokenScanner);
                    }
                    else
                    {
                        throw new ArgumentException("Unknown type for function.");
                    }
                }
            }
            else
            {
                throw new ArgumentException("Functions is Required.");
            }

            if (functionDictionary.TryGet<ArrayToken>(NameToken.Bounds, pdfTokenScanner, out var bounds))
            {
                Bounds = bounds;
            }
            else
            {
                throw new ArgumentException("Bounds is Required.");
            }

            if (functionDictionary.TryGet<ArrayToken>(NameToken.Encode, pdfTokenScanner, out var encode))
            {
                Encode = encode;
            }
            else
            {
                throw new ArgumentException("Encode is Required.");
            }
        }

        /// <summary>
        /// PdfFunctionType3
        /// </summary>
        /// <param name="functionStream"></param>
        /// <param name="pdfTokenScanner"></param>
        public PdfFunctionType3(StreamToken functionStream, IPdfTokenScanner pdfTokenScanner)
            : this(functionStream.StreamDictionary, pdfTokenScanner)
        { }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public override float[] Eval(float[] input)
        {
            throw new NotImplementedException();
        }
    }
}
