namespace UglyToad.Examples
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public static class Program
    {
        public static void Main()
        {
            Console.WriteLine("Welcome to the PdfPig examples gallery!");

            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var filesDirectory = Path.Combine(baseDirectory, "..", "..", "..", "..", "src", "UglyToad.PdfPig.Tests", "Integration", "Documents");

            var examples = new Dictionary<int, (string name, Action action)>
            {
                {1,
                    ("Extract Words with newline detection",
                    () => OpenDocumentAndExtractWords.Run(Path.Combine(filesDirectory, "Two Page Text Only - from libre office.pdf")))
                },
                {2,
                    ("Extract images",
                    () => ExtractImages.Run(Path.Combine(filesDirectory, "2006_Swedish_Touring_Car_Championship.pdf")))
                }
            };

            var choices = string.Join(Environment.NewLine, examples.Select(x => $"{x.Key}: {x.Value.name}"));

            Console.WriteLine(choices);
            Console.WriteLine();

            do
            {
                Console.Write("Enter a number to pick an example (enter 'q' to exit):");

                var val = Console.ReadLine();

                if (!int.TryParse(val, out var opt) || !examples.TryGetValue(opt, out var act))
                {
                    if (string.Equals(val, "q", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }

                    Console.WriteLine($"No option with value: {val}.");
                    continue;
                }

                act.action.Invoke();
            } while (true);
        }
    }
}
