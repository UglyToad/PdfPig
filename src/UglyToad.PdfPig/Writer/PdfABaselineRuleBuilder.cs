using System;
using System.Collections.Generic;
using UglyToad.PdfPig.Tokens;
using UglyToad.PdfPig.Writer.Colors;
using UglyToad.PdfPig.Writer.Xmp;

namespace UglyToad.PdfPig.Writer
{
    internal static class PdfABaselineRuleBuilder
    {
        public static void Obey(
            Dictionary<NameToken, IToken> catalog,
            Func<IToken, IndirectReferenceToken> writerFunc,
            PdfDocumentBuilder.DocumentInformationBuilder documentInformationBuilder,
            PdfAStandard archiveStandard,
            decimal version)
        {
            catalog[NameToken.OutputIntents] = OutputIntentsFactory.GetOutputIntentsArray(writerFunc);
            var xmpStream = XmpWriter.GenerateXmpStream(documentInformationBuilder, version, archiveStandard);
            var xmpObj = writerFunc(xmpStream);
            catalog[NameToken.Metadata] = xmpObj;
        }
    }
}