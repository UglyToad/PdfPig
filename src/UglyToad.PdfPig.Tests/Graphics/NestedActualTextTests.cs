using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace UglyToad.PdfPig.Tests.Graphics
{
    /// <summary>
    /// Replacement text (<c>/ActualText</c>, PDF specification 14.9.4) applies to the marked-content
    /// sequence that carries it. Sequences nest, so beginning or ending an inner one must not
    /// disturb the replacement text of the sequence enclosing it.
    /// </summary>
    public class NestedActualTextTests
    {
        [Fact]
        public void ReplacementAppliesToItsOwnSequenceOnly()
        {
            Assert.Equal("XYB", Extract("/Span <</ActualText (XY)>> BDC\n"
                                        + "(A) Tj\n"
                                        + "EMC\n"
                                        + "(B) Tj\n"));
        }

        [Fact]
        public void EndingAnInnerSequenceDoesNotEndTheEnclosingReplacement()
        {
            Assert.Equal("XY", Extract("/Span <</ActualText (XY)>> BDC\n"
                                       + "/Inner BMC\n"
                                       + "EMC\n"
                                       + "(A) Tj\n"
                                       + "EMC\n"));
        }

        [Fact]
        public void AnInnerSequenceWithoutReplacementInheritsTheEnclosingOne()
        {
            Assert.Equal("XY", Extract("/Span <</ActualText (XY)>> BDC\n"
                                       + "/Inner BMC\n"
                                       + "(A) Tj\n"
                                       + "EMC\n"
                                       + "EMC\n"));
        }

        /// <summary>
        /// The enclosing replacement already stands for the whole of its content, the inner sequence
        /// included, so an inner one would say the same region twice.
        /// </summary>
        [Fact]
        public void AnInnerReplacementIsSubsumedByTheEnclosingOne()
        {
            Assert.Equal("AA", Extract("/A <</ActualText (AA)>> BDC\n"
                                       + "/B <</ActualText (BB)>> BDC\n"
                                       + "(x) Tj\n"
                                       + "EMC\n"
                                       + "(y) Tj\n"
                                       + "EMC\n"));
        }

        /// <summary>
        /// A sequence that follows one carrying replacement text is not enclosed by it, so it brings
        /// its own.
        /// </summary>
        [Fact]
        public void ASiblingSequenceBringsItsOwnReplacement()
        {
            Assert.Equal("AABB", Extract("/A <</ActualText (AA)>> BDC\n"
                                         + "(x) Tj\n"
                                         + "EMC\n"
                                         + "/B <</ActualText (BB)>> BDC\n"
                                         + "(y) Tj\n"
                                         + "EMC\n"));
        }

        /// <summary>
        /// The replacement text stands for the whole sequence, so it is emitted once however many
        /// glyphs the sequence holds, including those in sequences nested inside it.
        /// </summary>
        [Fact]
        public void ReplacementIsEmittedOnceAcrossNestedSequences()
        {
            Assert.Equal("XY", Extract("/Span <</ActualText (XY)>> BDC\n"
                                       + "(A) Tj\n"
                                       + "/Inner BMC\n"
                                       + "(B) Tj\n"
                                       + "EMC\n"
                                       + "(C) Tj\n"
                                       + "EMC\n"));
        }

        private static string Extract(string markedContent)
        {
            var contents = Encoding.ASCII.GetBytes("BT\n/F1 12 Tf\n10 50 Td\n" + markedContent + "ET\n");

            using var document = PdfDocument.Open(BuildSinglePagePdf(contents),
                new ParsingOptions { UseActualText = true });

            return string.Concat(document.GetPage(1).Letters.Select(l => l.Value));
        }

        private static byte[] BuildSinglePagePdf(byte[] contentStream)
        {
            const string Resources = "<< /Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >> >>";

            using var ms = new MemoryStream();
            var offsets = new long[5];

            void Write(string s)
            {
                var b = Encoding.ASCII.GetBytes(s);
                ms.Write(b, 0, b.Length);
            }

            Write("%PDF-1.7\n");

            offsets[1] = ms.Position;
            Write("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            offsets[2] = ms.Position;
            Write("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

            offsets[3] = ms.Position;
            Write($"3 0 obj\n<< /Type /Page /Parent 2 0 R /Resources {Resources} "
                  + "/MediaBox [0 0 100 100] /Contents 4 0 R >>\nendobj\n");

            offsets[4] = ms.Position;
            Write($"4 0 obj\n<< /Length {contentStream.Length.ToString(CultureInfo.InvariantCulture)} >>\nstream\n");
            ms.Write(contentStream, 0, contentStream.Length);
            Write("\nendstream\nendobj\n");

            var xref = ms.Position;
            Write("xref\n0 5\n");
            Write("0000000000 65535 f \n");
            for (int i = 1; i <= 4; i++)
            {
                Write($"{offsets[i].ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n");
            }

            Write("trailer\n<< /Size 5 /Root 1 0 R >>\nstartxref\n");
            Write(xref.ToString(CultureInfo.InvariantCulture));
            Write("\n%%EOF\n");

            return ms.ToArray();
        }
    }
}
