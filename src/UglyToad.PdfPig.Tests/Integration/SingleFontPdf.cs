namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// Builds a one-page PDF that shows text in a single font, for font tests.
    /// </summary>
    internal static class SingleFontPdf
    {
        /// <summary>
        /// A one-page PDF that shows <paramref name="text"/> (a PDF string, e.g. "(A)") at 12 pt in
        /// <paramref name="font"/>, object 4, used as /F1. <paramref name="extraObjects"/> follow as objects 6, 7, ...,
        /// e.g. the font descriptor and the font program.
        /// </summary>
        public static byte[] BuildPdf(string font, string text, params string[] extraObjects)
        {
            string content = $"BT /F1 12 Tf 10 10 Td {text} Tj ET";

            var objects = new List<string>
            {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Contents 5 0 R /Resources << /Font << /F1 4 0 R >> >> >>",
                font,
                $"<< /Length {content.Length} >>\nstream\n{content}\nendstream"
            };

            objects.AddRange(extraObjects);

            var builder = new StringBuilder("%PDF-1.7\n");
            var offsets = new List<int>();
            for (int i = 0; i < objects.Count; i++)
            {
                offsets.Add(builder.Length);
                builder.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            int xref = builder.Length;
            builder.Append($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
            foreach (int offset in offsets)
            {
                builder.Append($"{offset:D10} 00000 n \n");
            }

            builder.Append($"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF");

            return Encoding.ASCII.GetBytes(builder.ToString());
        }
    }
}
