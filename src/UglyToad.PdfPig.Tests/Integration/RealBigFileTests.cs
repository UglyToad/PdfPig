namespace UglyToad.PdfPig.Tests.Integration
{
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using SkiaSharp;
    using Xunit.Abstractions;

    /// <summary>
    /// A class for testing files which are not checked in to source control.
    /// </summary>
    public class RealBigFileTests(ITestOutputHelper output)
    {
        [Fact(Skip = "Local opt-in repro: creates a PDF larger than 2 GiB and is too expensive for normal test runs.")]
        public void CanCreateAndReadRealPdfLargerThanTwoGigabytes()
        {
            var tempDirectory = Path.Combine(Path.GetTempPath(), "PdfPigTests", Guid.NewGuid().ToString("N"));
            var outputPath = Path.Combine(tempDirectory, "three-gb-noise.pdf");

            try
            {
                WriteProgress($"Starting large PDF generation at {outputPath}");
                var result = LargePdfTestDocumentGenerator.CreateNoisePdf(outputPath, progress: WriteProgress);

                WriteProgress($"Created {result.OutputPath}: {result.PageCount:N0} pages, {result.Bytes / 1024d / 1024d / 1024d:N2} GiB, elapsed {result.Elapsed}.");

                Assert.True(result.Bytes > int.MaxValue, $"Expected local test PDF to be larger than {int.MaxValue} bytes.");

                WriteProgress("Opening generated PDF with PdfDocument.Open(Stream).");
                using var fileStream = File.OpenRead(outputPath);
                using var document = PdfDocument.Open(fileStream, ParsingOptions.LenientParsingOff);
                WriteProgress($"Opened {Path.GetFileName(outputPath)} ({result.Bytes:N0} bytes), pages: {document.NumberOfPages}.");
                Assert.True(document.NumberOfPages >= LargePdfTestDocumentGenerator.SentinelPageNumber);

                WriteProgress($"Reading page {LargePdfTestDocumentGenerator.SentinelPageNumber}.");
                var page = document.GetPage(LargePdfTestDocumentGenerator.SentinelPageNumber);

                WriteProgress("Asserting sentinel text.");
                Assert.Contains(LargePdfTestDocumentGenerator.SentinelPageText, page.Text);
            }
            finally
            {
                if (Directory.Exists(tempDirectory))
                {
                    WriteProgress($"Deleting temporary directory {tempDirectory}.");
                    Directory.Delete(tempDirectory, recursive: true);
                }
            }
        }

        private void WriteProgress(string message)
        {
            var line = $"{DateTimeOffset.Now:HH:mm:ss} {message}";
            output.WriteLine(line);
            Trace.WriteLine(line);
            Console.WriteLine(line);
        }

        //[Fact]
        //public void Tests()
        //{
        //    var files = Directory.GetFiles(@"C:\temp\pdfs", "*.pdf");

        //    foreach (var file in files)
        //    {
        //        try
        //        {
        //            using (var document = PdfDocument.Open(file, new ParsingOptions { UseLenientParsing = false }))
        //            {
        //                for (var i = 1; i <= document.NumberOfPages; i++)
        //                {
        //                    var page = document.GetPage(i);
        //                    var text = page.Text;
        //                    Trace.WriteLine(text);
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            throw new InvalidOperationException($"Error parsing: {Path.GetFileName(file)}.", ex);
        //        }
        //    }
        //}
    }

    internal static class LargePdfTestDocumentGenerator
    {
        private const long DefaultTargetBytes = (long)int.MaxValue + 1024 * 1024;
        private const int DefaultImageWidth = 1536;
        private const int DefaultImageHeight = 1536;
        private const int DefaultPagesPerProgressUpdate = 10;

        public const int SentinelPageNumber = 250;
        public const string SentinelPageText = "PdfPig >2 GiB sentinel text on page 250";

        private static readonly string[] TextFragments =
        {
            "large-file parsing",
            "xref after two gigabytes",
            "named destinations",
            "url annotation",
            "random text overlay",
            "PdfPig stress document",
            "seekable stream path",
        };

        private static readonly SKPaint OverlayPaint = new() { Color = new SKColor(12, 16, 24, 210), IsAntialias = true, };

        private static readonly SKPaint TextPaint = new()
        {
            Color = SKColors.White,
            IsAntialias = true,
            TextSize = 34,
            Typeface = SKTypeface.FromFamilyName("Consolas") ?? SKTypeface.Default,
        };

        private static readonly SKPaint AccentPaint = new()
        {
            Color = new SKColor(0, 184, 148, 235),
            IsAntialias = true,
            TextSize = 26,
            Typeface = SKTypeface.FromFamilyName("Segoe UI") ?? SKTypeface.Default,
        };

        public static Result CreateNoisePdf(
            string outputPath,
            long targetBytes = DefaultTargetBytes,
            int imageWidth = DefaultImageWidth,
            int imageHeight = DefaultImageHeight,
            int pagesPerProgressUpdate = DefaultPagesPerProgressUpdate,
            Action<string>? progress = null)
        {
            progress?.Invoke($"Creating output directory for {outputPath}.");

            var outputDirectory = Path.GetDirectoryName(outputPath);

            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            progress?.Invoke($"Allocating {imageWidth:N0}x{imageHeight:N0} BGRA noise buffer.");
            var imageInfo = new SKImageInfo(imageWidth, imageHeight, SKColorType.Bgra8888, SKAlphaType.Opaque);

            var pageRect = new SKRect(0, 0, imageWidth, imageHeight);
            var buffer = new byte[imageInfo.BytesSize];
            var startedAt = DateTimeOffset.Now;
            var pageNumber = 0;

            var random = new Random(8675309);
            progress?.Invoke($"Opening output file. Target size: {targetBytes / 1024d / 1024d:N1} MiB.");
            using var file = new FileStream(
                outputPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.Read,
                bufferSize: 1024 * 1024,
                options: FileOptions.SequentialScan);

            using var stream = new SKManagedWStream(file);
            var metadata = new SKDocumentPdfMetadata(SKDocumentPdfMetadata.DefaultEncodingQuality)
            {
                Title = "PdfPig >2 GiB Noise Stress Document",
                Author = "PdfPig Tests",
                Subject = "Large PDF parsing stress fixture",
                Keywords = "PdfPig, large file, xref, annotations, named destinations",
                Creator = nameof(LargePdfTestDocumentGenerator),
                Creation = DateTime.Now,
                Modified = DateTime.Now,
            };

            using var document = SKDocument.CreatePdf(
                stream,
                metadata);

            progress?.Invoke("Writing noisy PDF pages.");
            while (file.Length < targetBytes)
            {
                pageNumber++;

                FillWithOpaqueNoise(buffer, random);
                using var bitmap = new SKBitmap(imageInfo);
                Marshal.Copy(buffer, 0, bitmap.GetPixels(), buffer.Length);

                var canvas = document.BeginPage(imageWidth, imageHeight);
                canvas.Clear(SKColors.White);
                canvas.DrawBitmap(bitmap, pageRect);
                DrawPageChrome(canvas, imageWidth, imageHeight, pageNumber);
                document.EndPage();

                if (pageNumber % pagesPerProgressUpdate != 0) { continue; }

                stream.Flush();
                progress?.Invoke($"{pageNumber:N0} pages written, {file.Length / 1024d / 1024d:N1} MiB, {file.Length * 100d / targetBytes:N1}%.");
            }

            progress?.Invoke("Closing SKDocument.");
            document.Close();
            stream.Flush();
            progress?.Invoke("Flushing output file to disk.");
            file.Flush(flushToDisk: true);

            return new Result(outputPath, pageNumber, file.Length, DateTimeOffset.Now - startedAt);
        }

        private static void DrawPageChrome(SKCanvas canvas, int width, int height, int pageNumber)
        {
            // SkiaSharp's PDF backend supports named destinations and link annotations, but not outline bookmarks.
            var destinationName = $"page-{pageNumber}";
            canvas.DrawNamedDestinationAnnotation(new SKPoint(72, 72), destinationName);

            var panel = new SKRect(48, 48, width - 48, 218);
            canvas.DrawRoundRect(panel, 12, 12, OverlayPaint);

            var heading = pageNumber == SentinelPageNumber ? SentinelPageText : $"PdfPig large-file fixture - page {pageNumber:N0}";

            canvas.DrawText(heading, 72, 104, TextPaint);
            canvas.DrawText(CreateOverlayText(pageNumber), 72, 154, AccentPaint);
            canvas.DrawText("Top banner links to page 1. Bottom banner links to github.com/UglyToad/PdfPig.", 72, 194, AccentPaint);

            if (pageNumber != 1)
            {
                canvas.DrawLinkDestinationAnnotation(panel, "page-1");
            }

            var urlRect = new SKRect(48, height - 118, width - 48, height - 48);
            canvas.DrawRoundRect(urlRect, 12, 12, OverlayPaint);
            canvas.DrawText("https://github.com/UglyToad/PdfPig", 72, height - 73, AccentPaint);
            canvas.DrawUrlAnnotation(urlRect, "https://github.com/UglyToad/PdfPig");
        }

        private static string CreateOverlayText(int pageNumber)
        {
            var random = new Random(pageNumber);

            return string.Join(" | ", Enumerable.Range(0, 4).Select(_ => TextFragments[random.Next(TextFragments.Length)]));
        }

        private static void FillWithOpaqueNoise(byte[] bgra, Random random)
        {
            random.NextBytes(bgra);

            for (var i = 3; i < bgra.Length; i += 4)
            {
                bgra[i] = 255;
            }
        }

        public readonly record struct Result(string OutputPath, int PageCount, long Bytes, TimeSpan Elapsed);
    }
}
