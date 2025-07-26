using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Console = System.Console;

namespace UglyToad.PdfPig.ConsoleRunner
{
    public static class Program
    {
        private class OptionalArg
        {
            public required string ShortSymbol { get; init; }

            public required string Symbol { get; init; }

            public required bool SupportsValue { get; init; }

            public string? Value { get; set; }
        }

        private class ParsedArgs
        {
            public required IReadOnlyList<OptionalArg> SuppliedArgs { get; init; }

            public required string SuppliedDirectoryPath { get; init; }
        }

        private const string FileSymbol = "f";
        private const string RepeatSymbol = "r";

        private static IReadOnlyList<OptionalArg> GetSupportedArgs() =>
        [
            new OptionalArg
            {
                SupportsValue = false,
                ShortSymbol = "nr",
                Symbol = "no-recursion"
            },
            new OptionalArg
            {
                SupportsValue = true,
                ShortSymbol = "o",
                Symbol = "output"
            },
            new OptionalArg
            {
                SupportsValue = true,
                ShortSymbol = "l",
                Symbol = "limit"
            },
            new OptionalArg
            {
                SupportsValue = true,
                ShortSymbol = "f",
                Symbol = "file"
            },
            new OptionalArg
            {
                SupportsValue = true,
                ShortSymbol = "r",
                Symbol = "repeats"
            }
        ];

        private static bool TryParseArgs(
            string[] args,
            [NotNullWhen(true)] out ParsedArgs? parsed)
        {
            parsed = null;
            string? path = null;
            var suppliedOpts = new List<OptionalArg>();

            var opts = GetSupportedArgs();

            for (var i = 0; i < args.Length; i++)
            {
                var str = args[i];

                var isOptFlag = str.StartsWith('-');

                if (!isOptFlag)
                {
                    if (path == null)
                    {
                        path = str;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    var item = opts.SingleOrDefault(x =>
                        string.Equals("-" + x.ShortSymbol, str, StringComparison.OrdinalIgnoreCase)
                        || string.Equals("--" + x.Symbol, str, StringComparison.OrdinalIgnoreCase));

                    if (item == null)
                    {
                        return false;
                    }

                    if (item.SupportsValue)
                    {
                        if (i == args.Length - 1)
                        {
                            return false;
                        }

                        i++;
                        item.Value = args[i];
                    }

                    suppliedOpts.Add(item);
                }
            }

            if (path == null)
            {
                return false;
            }

            parsed = new ParsedArgs
            {
                SuppliedArgs = suppliedOpts,
                SuppliedDirectoryPath = path
            };

            return true;
        }

        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("At least 1 argument, path to test file directory, must be provided.");
                return 7;
            }

            if (!TryParseArgs(args, out var parsed))
            {
                var strJoined = string.Join(" ", args);
                Console.WriteLine($"Unrecognized arguments passed: {strJoined}");
                return 7;
            }

            if (!Directory.Exists(parsed.SuppliedDirectoryPath))
            {
                Console.WriteLine($"The provided path is not a valid directory: {parsed.SuppliedDirectoryPath}.");
                return 7;
            }

            int? maxCount = null;
            var limit = parsed.SuppliedArgs.SingleOrDefault(x => x.ShortSymbol == "l");
            if (limit?.Value != null && int.TryParse(limit.Value, CultureInfo.InvariantCulture, out var maxCountArg))
            {
                Console.WriteLine($"Limiting input files to first: {maxCountArg}");
                maxCount = maxCountArg;
            }
            
            var noRecursionMode = parsed.SuppliedArgs.Any(x => x.ShortSymbol == "nr");
            var outputOpt = parsed.SuppliedArgs.SingleOrDefault(x => x.ShortSymbol == "o" && x.Value != null);

            var fileOpt = parsed.SuppliedArgs.SingleOrDefault(x => x.ShortSymbol == FileSymbol && x.Value != null);

            var hasError = false;
            var errorBuilder = new StringBuilder();
            var fileList = Directory.GetFiles(
                    parsed.SuppliedDirectoryPath,
                    "*.pdf",
                    noRecursionMode ? SearchOption.TopDirectoryOnly : SearchOption.AllDirectories)
                .OrderBy(x => x).ToList();
            var runningCount = 0;

            if (fileOpt?.Value != null)
            {
                fileList = fileList.Where(x => x.EndsWith(fileOpt.Value, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var repeatOpt = parsed.SuppliedArgs.SingleOrDefault(x => x.ShortSymbol == RepeatSymbol);

            var repeats = 1;
            if (repeatOpt?.Value != null && int.TryParse(repeatOpt.Value, CultureInfo.InvariantCulture, out repeats))
            {
            }

            Console.WriteLine($"Found {fileList.Count} files.");
            Console.WriteLine();

            PrintTableColumns("File", "Size", "Words", "Pages", "Open cost (μs)", "Total cost (μs)", "Page cost (μs)");

            var dataList = new List<DataRecord>();

            var sw = new Stopwatch();
            for (int i = 0; i < repeats; i++)
            {
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
                        long openMicros;
                        long totalPageMicros;

                        sw.Reset();
                        sw.Start();

                        using (var pdfDocument = PdfDocument.Open(file))
                        {
                            sw.Stop();

                            openMicros = sw.Elapsed.Microseconds;

                            sw.Start();

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

                            sw.Stop();
                            totalPageMicros = sw.Elapsed.Microseconds;
                        }

                        var filename = Path.GetFileName(file);

                        var size = new FileInfo(file);

                        var item = new DataRecord
                        {
                            FileName = filename,
                            OpenCostMicros = openMicros,
                            Pages = numPages,
                            Size = size.Length,
                            Words = numWords,
                            TotalCostMicros = totalPageMicros + openMicros,
                            PerPageMicros = Math.Round(totalPageMicros / (double)Math.Max(numPages, 1), 2)
                        };

                        dataList.Add(item);

                        PrintTableColumns(
                            item.FileName,
                            item.Size,
                            item.Words,
                            item.Pages,
                            item.OpenCostMicros,
                            item.TotalCostMicros,
                            item.PerPageMicros);
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
            }

            if (hasError)
            {
                Console.WriteLine(errorBuilder.ToString());
                return 5;
            }

            if (outputOpt != null && outputOpt.Value != null)
            {
                WriteOutput(outputOpt.Value, dataList);
            }

            Console.WriteLine("Complete! :)");

            return 0;
        }

        private static void WriteOutput(string outPath, IReadOnlyList<DataRecord> records)
        {
            using var fs = File.OpenWrite(outPath);
            using var sw = new StreamWriter(fs);

            sw.WriteLine("File,Size,Words,Pages,Open Cost,Total Cost,Per Page");
            foreach (var record in records)
            {
                var sizeStr = record.Size.ToString("D", CultureInfo.InvariantCulture);
                var wordsStr = record.Words.ToString("D", CultureInfo.InvariantCulture);
                var pagesStr = record.Pages.ToString("D", CultureInfo.InvariantCulture);
                var openCostStr = record.OpenCostMicros.ToString("D", CultureInfo.InvariantCulture);
                var totalCostStr = record.TotalCostMicros.ToString("D", CultureInfo.InvariantCulture);
                var ppcStr = record.PerPageMicros.ToString("F2", CultureInfo.InvariantCulture);

                var numericPartsStr = string.Join(",",
                [
                    sizeStr,
                    wordsStr,
                    pagesStr,
                    openCostStr,
                    totalCostStr,
                    ppcStr
                ]);
                
                sw.WriteLine($"\"{record.FileName}\",{numericPartsStr}");
            }

            sw.Flush();
        }

        private static void PrintTableColumns(params object[] values)
        {
            for (var i = 0; i < values.Length; i++)
            {
                var value = values[i];
                var valueStr = value.ToString();

                var cleaned = GetCleanStr(valueStr ?? string.Empty);

                var padChars = 16 - cleaned.Length;

                var padding = padChars > 0 ? new string(' ', padChars) : string.Empty;

                var padded = cleaned + padding;

                Console.Write("| ");

                Console.Write(padded);
            }

            Console.WriteLine();
        }

        private static string GetCleanStr(string name, int maxLength = 16)
        {
            if (name.Length <= maxLength)
            {
                var fillLength = maxLength - name.Length;

                return name + new string(' ', fillLength);
            }

            return name.Substring(0, maxLength);
        }
    }

    internal class DataRecord
    {
        public required string FileName { get; init; }

        public required long Size { get; init; }

        public required int Words { get; init; }

        public required int Pages { get; init; }

        public required long OpenCostMicros { get; init; }

        public required long TotalCostMicros { get; init; }

        public required double PerPageMicros { get; init; }
    }
}
