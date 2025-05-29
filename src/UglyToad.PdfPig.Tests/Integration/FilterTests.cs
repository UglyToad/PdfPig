namespace UglyToad.PdfPig.Tests.Integration
{
    using PdfPig.Filters;
    using PdfPig.Tokens;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class FilterTests
    {
        private static readonly Lazy<string> DocumentFolder = new Lazy<string>(() => Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "Integration", "Documents")));
        private static readonly HashSet<string> _documentsToIgnore =
        [
            "issue_671.pdf",
            "GHOSTSCRIPT-698363-0.pdf",
            "ErcotFacts.pdf"
        ];

        [Theory]
        [MemberData(nameof(GetAllDocuments))]
        public void NoImageDecoding(string documentName)
        {
            // Add the full path back on, we removed it so we could see it in the test explorer.
            documentName = Path.Combine(DocumentFolder.Value, documentName);

            var parsingOptions = new ParsingOptions
            {
                UseLenientParsing = true,
                FilterProvider = MyFilterProvider.Instance
            };

            using (var document = PdfDocument.Open(documentName, parsingOptions))
            {
                for (var i = 0; i < document.NumberOfPages; i++)
                {
                    var page = document.GetPage(i + 1);

                    foreach (var pdfImage in page.GetImages())
                    {
                        if (pdfImage.ImageDictionary.TryGet(NameToken.Filter, out NameToken filter))
                        {
                            if (filter.Data.Equals(NameToken.FlateDecode.Data) ||
                                filter.Data.Equals(NameToken.FlateDecodeAbbreviation.Data) || 
                                filter.Data.Equals(NameToken.LzwDecode.Data) ||
                                filter.Data.Equals(NameToken.LzwDecodeAbbreviation.Data))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            continue;
                        }

                        Assert.False(pdfImage.TryGetPng(out _));
                    }
                }
            }
        }

        public sealed class NoFilter : IFilter
        {
            public bool IsSupported => false;

            public Memory<byte> Decode(Memory<byte> input, DictionaryToken streamDictionary, IFilterProvider filterProvider, int filterIndex)
            {
                throw new NotImplementedException();
            }
        }

        public class MyFilterProvider : BaseFilterProvider
        {
            /// <summary>
            /// The single instance of this provider.
            /// </summary>
            public static readonly IFilterProvider Instance = new MyFilterProvider();

            /// <inheritdoc/>
            protected MyFilterProvider() : base(GetDictionary())
            {
            }

            private static Dictionary<string, IFilter> GetDictionary()
            {
                var ascii85 = new Ascii85Filter();
                var asciiHex = new AsciiHexDecodeFilter();
                var flate = new FlateFilter();
                var runLength = new RunLengthFilter();
                var lzw = new LzwFilter();

                var noFilter = new NoFilter();

                return new Dictionary<string, IFilter>
                {
                    { NameToken.Ascii85Decode.Data, ascii85 },
                    { NameToken.Ascii85DecodeAbbreviation.Data, ascii85 },
                    { NameToken.AsciiHexDecode.Data, asciiHex },
                    { NameToken.AsciiHexDecodeAbbreviation.Data, asciiHex },
                    { NameToken.CcittfaxDecode.Data, noFilter },
                    { NameToken.CcittfaxDecodeAbbreviation.Data, noFilter },
                    { NameToken.DctDecode.Data, noFilter },
                    { NameToken.DctDecodeAbbreviation.Data, noFilter },
                    { NameToken.FlateDecode.Data, flate },
                    { NameToken.FlateDecodeAbbreviation.Data, flate },
                    { NameToken.Jbig2Decode.Data, noFilter },
                    { NameToken.JpxDecode.Data, noFilter },
                    { NameToken.RunLengthDecode.Data, runLength },
                    { NameToken.RunLengthDecodeAbbreviation.Data, runLength },
                    {NameToken.LzwDecode, lzw },
                    {NameToken.LzwDecodeAbbreviation, lzw }
                };
            }
        }

        public static IEnumerable<object[]> GetAllDocuments
        {
            get
            {
                var files = Directory.GetFiles(DocumentFolder.Value, "*.pdf");

                // Return the shortname so we can see it in the test explorer.
                return files.Where(x => !_documentsToIgnore.Any(i => x.EndsWith(i))).Select(x => new object[] { Path.GetFileName(x) });
            }
        }
    }
}
