namespace UglyToad.Examples
{
    using System;
    using System.Text;
    using PdfPig;
    using PdfPig.Content;

    public static class OpenDocumentAndExtractWords
    {
        public static void Run(string filePath)
        {
            var sb = new StringBuilder();

            using (var document = PdfDocument.Open(filePath))
            {
                Word previous = null;
                foreach (var page in document.GetPages())
                {
                    foreach (var word in page.GetWords())
                    {
                        if (previous != null)
                        {
                            var hasInsertedWhitespace = false;
                            var bothNonEmpty = previous.Letters.Count > 0 && word.Letters.Count > 0;
                            if (bothNonEmpty)
                            {
                                var prevLetter1 = previous.Letters[0];
                                var currentLetter1 = word.Letters[0];

                                var baselineGap = Math.Abs(prevLetter1.StartBaseLine.Y - currentLetter1.StartBaseLine.Y);

                                if (baselineGap > 3)
                                {
                                    hasInsertedWhitespace = true;
                                    sb.AppendLine();
                                }
                            }

                            if (!hasInsertedWhitespace)
                            {
                                sb.Append(" ");
                            }
                        }

                        sb.Append(word.Text);

                        previous = word;
                    }
                }
            }

            Console.WriteLine(sb.ToString());
        }
    }
}
