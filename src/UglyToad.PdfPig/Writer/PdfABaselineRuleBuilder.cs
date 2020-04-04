using System;
using System.Collections.Generic;
using UglyToad.PdfPig.Tokens;
using UglyToad.PdfPig.Writer.Colors;
using UglyToad.PdfPig.Writer.Xmp;

namespace UglyToad.PdfPig.Writer
{
    internal static class PdfABaselineRuleBuilder
    {
        public static void Obey(Dictionary<NameToken, IToken> catalog, Func<IToken, ObjectToken> writerFunc,
            PdfDocumentBuilder.DocumentInformationBuilder documentInformationBuilder,
            PdfAStandard archiveStandard)
        {
            catalog[NameToken.OutputIntents] = OutputIntentsFactory.GetOutputIntentsArray(writerFunc);
            var xmpStream = XmpWriter.GenerateXmpStream(documentInformationBuilder, 1.7m, archiveStandard);
            var xmpObj = writerFunc(xmpStream);
            catalog[NameToken.Metadata] = new IndirectReferenceToken(xmpObj.Number);
        }
    }
}