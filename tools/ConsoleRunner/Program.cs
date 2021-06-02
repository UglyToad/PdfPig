using System;
using System.IO;
using System.Text;
using Console = System.Console;

namespace UglyToad.PdfPig.ConsoleRunner
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("At least 1 argument, path to test file directory, may be provided.");
                return 7;
            }

            var path = args[0];

            if (!Directory.Exists(path))
            {
                Console.WriteLine($"The provided path is not a valid directory: {path}.");
                return 7;
            }

            var maxCount = default(int?);

            if (args.Length > 1 && int.TryParse(args[1], out var countIn))
            {
                maxCount = countIn;
            }

            var hasError = false;
            var errorBuilder = new StringBuilder();
            var fileList = Directory.GetFiles(path, "*.pdf");
            var runningCount = 0;
            
            foreach (var file in fileList)
            {
                if (maxCount.HasValue && runningCount >= maxCount)
                {
                    break;
                }

                try
                {
                    var numWords = 0;
                    var numPages = 0;
                    using (var pdfDocument = PdfDocument.Open(file))
                    {
                        foreach (var page in pdfDocument.GetPages())
                        {
                            numPages++;
                            foreach (var word in page.GetWords())
                            {
                                if (word != null)
                                {
                                    numWords++;
                                }
                            }
                        }
                    }

                    Console.WriteLine($"Read {numWords} words on {numPages} pages in document {file}.");
                }
                catch (Exception ex)
                {
                    hasError = true;
                    errorBuilder.AppendLine($"Parsing document {file} failed due to an error.")
                        .Append(ex)
                        .AppendLine();
                }

                runningCount++;
            }

            if (hasError)
            {
                Console.WriteLine(errorBuilder.ToString());
                return 5;
            }

            Console.WriteLine("Complete! :)");

            return 0;
        }
    }
}
