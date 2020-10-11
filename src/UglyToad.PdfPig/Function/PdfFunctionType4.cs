namespace UglyToad.PdfPig.Function
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// This class represents a Type 4 (PostScript calculator) function in a PDF document.
    /// </summary>
    public class PdfFunctionType4 : PdfFunction
    {
        /// <inheritdoc/>
        public override int FunctionType => 4;

        /// <inheritdoc/>
        public PdfFunctionType4(DictionaryToken functionDictionary, IPdfTokenScanner pdfTokenScanner)
            : base(functionDictionary, pdfTokenScanner)
        {
            // https://github.com/apache/pdfbox/blob/1ed782b8ff98e79d01da6dd7486a5e67610bc0a4/pdfbox/src/main/java/org/apache/pdfbox/pdmodel/common/function/PDFunctionType4.java
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override float[] Eval(float[] input)
        {
            throw new NotImplementedException();
        }
    }
}
