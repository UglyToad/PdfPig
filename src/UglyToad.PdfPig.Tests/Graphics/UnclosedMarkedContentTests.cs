using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace UglyToad.PdfPig.Tests.Graphics
{
    /// <summary>
    /// Marked-content sequences are balanced within a content stream (PDF specification, 14.6). A
    /// stream that leaves one open ends it by ending, so replacement text (<c>/ActualText</c>) it
    /// carries reaches no further than the stream that brought it.
    /// </summary>
    public class UnclosedMarkedContentTests
    {
        /// <summary>
        /// The form leaves its sequence open, so without a boundary its replacement text stays in
        /// effect afterwards and the rest of the page is extracted as the empty value the sequence
        /// gives every glyph after its first.
        /// </summary>
        [Fact]
        public void AnUnclosedSequenceInAFormDoesNotCoverTheRestOfThePage()
        {
            var extracted = Extract(
                pageContent: "q\n/Fm1 Do\nQ\nBT\n/F1 12 Tf\n10 20 Td\n(B) Tj\nET\n",
                formContent: "BT\n/F1 12 Tf\n10 50 Td\n/Span <</ActualText (XY)>> BDC\n(A) Tj\nET\n");

            Assert.Equal("XYB", extracted);
        }

        /// <summary>
        /// The other direction: a form invoked inside a sequence is part of that sequence's content,
        /// so the enclosing replacement still stands for what the form draws and is not repeated
        /// once the form returns.
        /// </summary>
        [Fact]
        public void AnEnclosingReplacementCoversAFormAndIsEmittedOnce()
        {
            var extracted = Extract(
                pageContent: "/Span <</ActualText (XY)>> BDC\nq\n/Fm1 Do\nQ\nEMC\n"
                             + "BT\n/F1 12 Tf\n10 20 Td\n(B) Tj\nET\n",
                formContent: "BT\n/F1 12 Tf\n10 50 Td\n(A) Tj\nET\n");

            Assert.Equal("XYB", extracted);
        }

        /// <summary>
        /// The form ends a sequence it never began. That cannot be the page's sequence ending, since
        /// the page's own end is still to come, so the form's stray end is ignored and the page's
        /// replacement text goes on covering what follows.
        /// </summary>
        [Fact]
        public void AStrayEndInAFormDoesNotEndTheEnclosingSequence()
        {
            var extracted = Extract(
                pageContent: "/Span <</ActualText (XY)>> BDC\nq\n/Fm1 Do\nQ\n"
                             + "BT\n/F1 12 Tf\n10 20 Td\n(B) Tj\nET\nEMC\n",
                formContent: "BT\n/F1 12 Tf\n10 50 Td\n(A) Tj\nET\nEMC\n");

            Assert.Equal("XY", extracted);
        }

        /// <summary>
        /// Within the stream that opened it the sequence is still open, so it covers everything that
        /// follows in that stream.
        /// </summary>
        [Fact]
        public void AnUnclosedSequenceCoversTheRestOfItsOwnStream()
        {
            var extracted = Extract(
                pageContent: "BT\n/F1 12 Tf\n10 50 Td\n/Span <</ActualText (XY)>> BDC\n(A) Tj\n(B) Tj\nET\n",
                formContent: "BT\n/F1 12 Tf\n10 20 Td\n(C) Tj\nET\n");

            Assert.Equal("XY", extracted);
        }

        private static string Extract(string pageContent, string formContent)
        {
            using var document = PdfDocument.Open(
                BuildSinglePagePdfWithForm(Encoding.ASCII.GetBytes(pageContent), Encoding.ASCII.GetBytes(formContent)),
                new ParsingOptions { UseActualText = true });

            return string.Concat(document.GetPage(1).Letters.Select(l => l.Value));
        }

        private static byte[] BuildSinglePagePdfWithForm(byte[] pageContent, byte[] formContent)
        {
            const string FontResource = "/Font << /F1 << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> >>";

            using var ms = new MemoryStream();
            var offsets = new long[6];

            void Write(string s)
            {
                var b = Encoding.ASCII.GetBytes(s);
                ms.Write(b, 0, b.Length);
            }

            void WriteStream(byte[] content)
            {
                Write($"stream\n");
                ms.Write(content, 0, content.Length);
                Write("\nendstream\nendobj\n");
            }

            Write("%PDF-1.7\n");

            offsets[1] = ms.Position;
            Write("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");

            offsets[2] = ms.Position;
            Write("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");

            offsets[3] = ms.Position;
            Write($"3 0 obj\n<< /Type /Page /Parent 2 0 R "
                  + $"/Resources << {FontResource} /XObject << /Fm1 5 0 R >> >> "
                  + "/MediaBox [0 0 100 100] /Contents 4 0 R >>\nendobj\n");

            offsets[4] = ms.Position;
            Write($"4 0 obj\n<< /Length {pageContent.Length.ToString(CultureInfo.InvariantCulture)} >>\n");
            WriteStream(pageContent);

            offsets[5] = ms.Position;
            Write($"5 0 obj\n<< /Type /XObject /Subtype /Form /BBox [0 0 100 100] /Resources << {FontResource} >> "
                  + $"/Length {formContent.Length.ToString(CultureInfo.InvariantCulture)} >>\n");
            WriteStream(formContent);

            var xref = ms.Position;
            Write("xref\n0 6\n");
            Write("0000000000 65535 f \n");
            for (int i = 1; i <= 5; i++)
            {
                Write($"{offsets[i].ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n");
            }

            Write("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n");
            Write(xref.ToString(CultureInfo.InvariantCulture));
            Write("\n%%EOF\n");

            return ms.ToArray();
        }
    }
}
