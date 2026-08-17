namespace UglyToad.PdfPig.Tests.Writer
{
    using PdfPig.Content;
    using PdfPig.Core;
    using PdfPig.Fonts.Standard14Fonts;
    using PdfPig.Writer;

    /// <summary>
    /// Regression tests for https://github.com/UglyToad/PdfPig/issues/1243 - the writer used to hardcode the
    /// <see cref="PdfPig.Filters.DefaultFilterProvider"/> instead of honouring
    /// <see cref="ParsingOptions.FilterProvider"/>, so documents relying on a custom filter could not be written.
    /// </summary>
    public class WriterCustomFilterProviderTests
    {
        [Fact]
        public void PdfTextRemoverUsesFilterProviderFromParsingOptions()
        {
            var input = CreateSinglePageDocumentWithCustomFilterName();

            AssertCannotBeReadWithTheDefaultFilterProvider(input);

            var provider = CustomNameFilterProvider.Create();

            using (var document = PdfDocument.Open(input, new ParsingOptions { FilterProvider = provider }))
            {
                Assert.Equal("Hello World!", document.GetPage(1).Text);

                using (var output = new MemoryStream())
                {
                    PdfTextRemover.RemoveText(document, output);

                    // The rewritten content stream uses the standard filter so the result opens with any provider.
                    using (var withoutText = PdfDocument.Open(output.ToArray()))
                    {
                        Assert.Equal(1, withoutText.NumberOfPages);
                        Assert.Equal(string.Empty, withoutText.GetPage(1).Text);
                    }
                }
            }
        }

        [Fact]
        public void PdfDocumentBuilderReadsCopiedContentStreamWithFilterProviderFromParsingOptions()
        {
            var input = CreateSinglePageDocumentWithCustomFilterName();

            var provider = CustomNameFilterProvider.Create();

            using (var document = PdfDocument.Open(input, new ParsingOptions { FilterProvider = provider }))
            {
                // AddPage parses the source page, which decodes the content stream, and then decodes it a second
                // time to look for a globally applied transform. Measure the cost of the former so the assertion
                // below is about the latter only.
                provider.ResetCount();
                document.GetPage(1);
                var decodesPerPageParse = provider.CustomFilterDecodeCount;

                Assert.True(decodesPerPageParse > 0);

                using (var output = new MemoryStream())
                {
                    using (var builder = new PdfDocumentBuilder(output, false))
                    {
                        provider.ResetCount();

                        builder.AddPage(document, 1);

                        // The global transform lookup can only decode the copied content stream if it was handed
                        // the provider from the parsing options.
                        Assert.True(
                            provider.CustomFilterDecodeCount > decodesPerPageParse,
                            $"Expected AddPage to decode the content stream with the supplied filter provider, "
                            + $"but it only decoded {provider.CustomFilterDecodeCount} stream(s), the same as parsing the page.");
                    }

                    using (var copied = PdfDocument.Open(output.ToArray(), new ParsingOptions { FilterProvider = provider }))
                    {
                        Assert.Equal(1, copied.NumberOfPages);
                        Assert.Equal("Hello World!", copied.GetPage(1).Text);
                    }
                }
            }
        }

        private static void AssertCannotBeReadWithTheDefaultFilterProvider(byte[] input)
        {
            // Sanity check for the tests above: without the custom provider the content stream is undecodable, so
            // any code path falling back to the default provider is guaranteed to fail rather than silently pass.
            Assert.ThrowsAny<Exception>(() =>
            {
                using (var document = PdfDocument.Open(input))
                {
                    _ = document.GetPage(1).Text;
                }
            });
        }

        private static byte[] CreateSinglePageDocumentWithCustomFilterName()
        {
            var builder = new PdfDocumentBuilder();

            var page = builder.AddPage(PageSize.A4);
            var font = builder.AddStandard14Font(Standard14Font.Helvetica);

            page.AddText("Hello World!", 12, new PdfPoint(25, 520), font);

            return CustomNameFilterProvider.ReplaceFlateDecodeName(builder.Build());
        }
    }
}
