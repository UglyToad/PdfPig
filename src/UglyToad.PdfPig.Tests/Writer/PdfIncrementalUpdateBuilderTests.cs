namespace UglyToad.PdfPig.Tests.Writer
{
    using System.Text;
    using Content;
    using Integration;
    using PdfPig.Core;
    using PdfPig.CrossReference;
    using PdfPig.Tokens;
    using PdfPig.Writer;

    public class PdfIncrementalUpdateBuilderTests
    {
        private const string Pages = "<< /Type /Pages /Kids [3 0 R] /Count 1 >>";
        private const string Page = "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] >>";
        private const string FileSpecification = "<< /Type /Filespec /F (a.txt) /UF (a.txt) /EF << /F 5 0 R >> >>";
        private const string EmbeddedFile = "<< /Type /EmbeddedFile /Length 5 >>\nstream\nhello\nendstream";
        private const string PdfA3Identification = "<pdfaid:part>3</pdfaid:part><pdfaid:conformance>B</pdfaid:conformance>";

        [Fact]
        public void AddedObjectIsAppendedAfterTheOriginalBytes()
        {
            var original = File.ReadAllBytes(IntegrationHelpers.GetDocumentPath("data.pdf"));

            byte[] updated;
            IndirectReferenceToken reference;
            using (var document = PdfDocument.Open(original))
            {
                var builder = new PdfIncrementalUpdateBuilder(document);
                reference = builder.AddObject(new StringToken("Added"));
                updated = builder.Build();
            }

            Assert.Equal(original, updated.Take(original.Length).ToArray());

            using var updatedDocument = PdfDocument.Open(updated);
            var added = Assert.IsType<StringToken>(updatedDocument.Structure.GetObject(reference.Data).Data);
            Assert.Equal("Added", added.Data);
        }

        [Fact]
        public void ReplacedObjectIsReadFromTheUpdate()
        {
            var original = File.ReadAllBytes(IntegrationHelpers.GetDocumentPath("data.pdf"));

            byte[] updated;
            using (var document = PdfDocument.Open(original))
            {
                var information = Assert.IsType<IndirectReferenceToken>(document.Structure.Trailer.Info);

                var builder = new PdfIncrementalUpdateBuilder(document);
                builder.ReplaceObject(information.Data, new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.Title, new StringToken("Updated title") }
                }));
                updated = builder.Build();
            }

            using var updatedDocument = PdfDocument.Open(updated);
            Assert.Equal("Updated title", updatedDocument.Information.Title);
        }

        [Fact]
        public void TrailerContinuesTheTrailerOfTheOriginal()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("data.pdf"));
            var originalIdentifier = document.Structure.Trailer.Identifier;

            var builder = new PdfIncrementalUpdateBuilder(document);
            var added = builder.AddObject(new StringToken("Added"));

            using var updatedDocument = PdfDocument.Open(builder.Build());
            var trailer = updatedDocument.Structure.Trailer;

            // data.pdf: startxref 27833, trailer << /Size 17 /Root 15 0 R /Info 16 0 R /ID [...] >>
            Assert.Equal(17, added.Data.ObjectNumber);
            Assert.Equal(18, trailer.Size);
            Assert.Equal(27833, trailer.PreviousCrossReferenceOffset);
            Assert.Equal(new IndirectReference(15, 0), trailer.Root);
            Assert.Equal(new IndirectReferenceToken(new IndirectReference(16, 0)), trailer.Info);

            Assert.Equal(2, trailer.Identifier.Count);
            Assert.Equal(originalIdentifier[0].Data, trailer.Identifier[0].Data);
            Assert.NotEqual(originalIdentifier[1].Data, trailer.Identifier[1].Data);
        }

        [Fact]
        public void DocumentWithoutIdentifierGetsTheSameNewIdentifierTwice()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("2006_Swedish_Touring_Car_Championship.pdf"));
            Assert.Empty(document.Structure.Trailer.Identifier);

            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddObject(new StringToken("Added"));

            using var updatedDocument = PdfDocument.Open(builder.Build());
            var identifier = updatedDocument.Structure.Trailer.Identifier;

            Assert.Equal(2, identifier.Count);
            Assert.Equal(identifier[0].Data, identifier[1].Data);
        }

        [Fact]
        public void UpdateOfDocumentWithCrossReferenceStreamIsWrittenAsCrossReferenceStream()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("bookmarks-with-accented-characters.pdf"));

            var builder = new PdfIncrementalUpdateBuilder(document);
            var added = builder.AddObject(new StringToken("Added"));

            using var updatedDocument = PdfDocument.Open(builder.Build());
            var update = updatedDocument.Structure.CrossReferenceTable.Parts[0];

            Assert.Equal(CrossReferenceType.Stream, update.Type);
            Assert.Equal(1421, update.Previous);

            var addedToken = Assert.IsType<StringToken>(updatedDocument.Structure.GetObject(added.Data).Data);
            Assert.Equal("Added", addedToken.Data);
            Assert.Equal(document.NumberOfPages, updatedDocument.NumberOfPages);
        }

        [Theory]
        [InlineData("data.pdf", 27833, CrossReferenceType.Table)] // 1 cross-reference table
        [InlineData("bookmarks-with-accented-characters.pdf", 1421, CrossReferenceType.Stream)] // 1 cross-reference stream
        [InlineData("cat-genetics.pdf", 516248, CrossReferenceType.Table)] // 10 cross-reference tables
        [InlineData("AcroFormsBasicFields.pdf", 170199, CrossReferenceType.Stream)] // 5 cross-reference streams
        [InlineData("bold-italic.pdf", 100737, CrossReferenceType.Table)] // hybrid-reference file
        [InlineData("Single Page Non Latin - from acrobat distiller.pdf", 116, CrossReferenceType.Table)] // linearized hybrid-reference file
        [InlineData("annotation-comments.pdf", 116, CrossReferenceType.Stream)] // linearized, /Size 9 in the main section but objects up to 29
        public void UpdatedDocumentKeepsTheOriginalAndReadsTheChanges(string documentName, long startxref, CrossReferenceType type)
        {
            var original = File.ReadAllBytes(IntegrationHelpers.GetDocumentPath(documentName));
            using var document = PdfDocument.Open(original);

            var builder = new PdfIncrementalUpdateBuilder(document);
            var added = builder.AddObject(new StringToken("Added"));
            var catalog = document.Structure.Catalog.CatalogDictionary;
            builder.ReplaceObject(document.Structure.Trailer.Root, catalog.With(NameToken.Create("Updated"), BooleanToken.True));

            var updated = builder.Build();
            Assert.Equal(original, updated.Take(original.Length).ToArray());

            using var updatedDocument = PdfDocument.Open(updated);
            var update = updatedDocument.Structure.CrossReferenceTable.Parts[0];
            Assert.Equal(type, update.Type);
            Assert.Equal(startxref, update.Previous);

            Assert.DoesNotContain(document.Structure.CrossReferenceTable.ObjectOffsets.Keys, x => x.ObjectNumber == added.Data.ObjectNumber);
            var addedToken = Assert.IsType<StringToken>(updatedDocument.Structure.GetObject(added.Data).Data);
            Assert.Equal("Added", addedToken.Data);
            Assert.True(updatedDocument.Structure.Catalog.CatalogDictionary.ContainsKey(NameToken.Create("Updated")));

            Assert.Equal(document.NumberOfPages, updatedDocument.NumberOfPages);
            Assert.Equal(document.GetPage(1).Text, updatedDocument.GetPage(1).Text);
        }

        [Fact]
        public void CanAddXmpMetadataToExistingDocument()
        {
            // The same steps as examples/AddXmpMetadataWithIncrementalUpdate.cs.
            const string xmp =
                "<?xpacket begin=\"﻿\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>" +
                "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">" +
                "<rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">" +
                "<rdf:Description rdf:about=\"\" xmlns:dc=\"http://purl.org/dc/elements/1.1/\">" +
                "<dc:title><rdf:Alt><rdf:li xml:lang=\"x-default\">Added with an incremental update</rdf:li></rdf:Alt></dc:title>" +
                "</rdf:Description>" +
                "</rdf:RDF>" +
                "</x:xmpmeta>" +
                "<?xpacket end=\"w\"?>";

            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("data.pdf"));
            Assert.False(document.TryGetXmpMetadata(out _));

            var builder = new PdfIncrementalUpdateBuilder(document);

            var metadata = builder.AddObject(new StreamToken(new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Type, NameToken.Metadata },
                { NameToken.Subtype, NameToken.Xml }
            }), Encoding.UTF8.GetBytes(xmp)));

            var catalog = document.Structure.Catalog.CatalogDictionary;
            builder.ReplaceObject(document.Structure.Trailer.Root, catalog.With(NameToken.Metadata, metadata));

            using var updatedDocument = PdfDocument.Open(builder.Build());

            Assert.True(updatedDocument.TryGetXmpMetadata(out var updatedMetadata));
            Assert.Contains("Added with an incremental update", updatedMetadata.GetXDocument().ToString());
            Assert.Equal(document.GetPage(1).Text, updatedDocument.GetPage(1).Text);
        }

        [Fact]
        public void AddedObjectCanBeReplaced()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("data.pdf"));

            var builder = new PdfIncrementalUpdateBuilder(document);
            var added = builder.AddObject(NullToken.Instance);
            builder.ReplaceObject(added.Data, new ArrayToken(new IToken[] { added }));

            using var updatedDocument = PdfDocument.Open(builder.Build());
            var array = Assert.IsType<ArrayToken>(updatedDocument.Structure.GetObject(added.Data).Data);
            Assert.Equal(added, Assert.Single(array.Data));
        }

        [Theory]
        [InlineData(100, 0)]
        [InlineData(16, 1)]
        public void ReplacingObjectThatIsNotInTheDocumentThrows(long objectNumber, int generation)
        {
            // data.pdf has the objects 1 to 16 with generation 0.
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("data.pdf"));
            var builder = new PdfIncrementalUpdateBuilder(document);

            Assert.Throws<ArgumentException>(() => builder.ReplaceObject(new IndirectReference(objectNumber, generation), NullToken.Instance));
        }

        [Fact]
        public void EncryptedDocumentIsNotSupported()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("iron-ore-q2-q3-2013.pdf"));
            Assert.True(document.IsEncrypted);

            Assert.Throws<NotSupportedException>(() => new PdfIncrementalUpdateBuilder(document));
        }

        [Fact]
        public void DocumentWithCrossReferenceFoundByBruteForceIsNotSupported()
        {
            var bytes = File.ReadAllBytes(IntegrationHelpers.GetDocumentPath("data.pdf"));

            // Point startxref past the end of the file so that the cross-reference is found by a brute-force search.
            var startxref = bytes.AsSpan().LastIndexOf("27833"u8);
            "99999"u8.CopyTo(bytes.AsSpan(startxref));

            using var document = PdfDocument.Open(bytes);

            Assert.Throws<NotSupportedException>(() => new PdfIncrementalUpdateBuilder(document));
        }

        [Fact]
        public void CanAddEmbeddedFileToDocumentWithoutEmbeddedFiles()
        {
            var original = File.ReadAllBytes(IntegrationHelpers.GetDocumentPath("data.pdf"));

            byte[] updated;
            using (var document = PdfDocument.Open(original))
            {
                Assert.False(document.Advanced.TryGetEmbeddedFiles(out _));

                var builder = new PdfIncrementalUpdateBuilder(document);
                builder.AddEmbeddedFile("hello.txt", "Hello"u8.ToArray());
                updated = builder.Build();
            }

            Assert.Equal(original, updated.Take(original.Length).ToArray());

            using var updatedDocument = PdfDocument.Open(updated, ParsingOptions.LenientParsingOff);
            Assert.True(updatedDocument.Advanced.TryGetEmbeddedFiles(out var files));

            var file = Assert.Single(files);
            Assert.Equal("hello.txt", file.Name);
            Assert.Equal("hello.txt", file.FileSpecification);
            Assert.Equal("Hello"u8.ToArray(), file.Bytes.ToArray());
        }

        [Theory]
        [InlineData("data.pdf")]
        [InlineData("bookmarks-with-accented-characters.pdf")]
        [InlineData("cat-genetics.pdf")]
        [InlineData("AcroFormsBasicFields.pdf")]
        [InlineData("bold-italic.pdf")]
        [InlineData("Single Page Non Latin - from acrobat distiller.pdf")]
        [InlineData("annotation-comments.pdf")]
        public void CanAddEmbeddedFileToDocumentWithAnyKindOfCrossReference(string documentName)
        {
            // The documents of UpdatedDocumentKeepsTheOriginalAndReadsTheChanges.
            var original = File.ReadAllBytes(IntegrationHelpers.GetDocumentPath(documentName));
            using var document = PdfDocument.Open(original);
            var count = document.Advanced.TryGetEmbeddedFiles(out var originalFiles) ? originalFiles.Count : 0;

            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddEmbeddedFile("hello.txt", "Hello"u8.ToArray());

            var updated = builder.Build();
            Assert.Equal(original, updated.Take(original.Length).ToArray());

            using var updatedDocument = PdfDocument.Open(updated);
            Assert.True(updatedDocument.Advanced.TryGetEmbeddedFiles(out var files));
            Assert.Equal(count + 1, files.Count);
            Assert.Equal("Hello"u8.ToArray(), files.Single(x => x.Name == "hello.txt").Bytes.ToArray());
            Assert.Equal(document.GetPage(1).Text, updatedDocument.GetPage(1).Text);
        }

        [Fact]
        public void AddedEmbeddedFileKeepsTheEmbeddedFilesAndOtherNameTreesOfTheDocument()
        {
            // The catalog refers to a names dictionary with the embedded file KSBASE.WQ2 and a JavaScript name tree.
            using var document = PdfDocument.Open(IntegrationHelpers.GetSpecificTestDocumentPath("embedded-file-attachment.pdf"));
            Assert.True(document.Advanced.TryGetEmbeddedFiles(out var originalFiles));
            var originalFile = Assert.Single(originalFiles);

            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddEmbeddedFile("hello.txt", "Hello"u8.ToArray());

            using var updatedDocument = PdfDocument.Open(builder.Build());
            Assert.True(updatedDocument.Advanced.TryGetEmbeddedFiles(out var files));

            Assert.Equal(new[] { "KSBASE.WQ2", "hello.txt" }, files.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
            Assert.Equal(originalFile.Bytes.ToArray(), files.Single(x => x.Name == "KSBASE.WQ2").Bytes.ToArray());
            Assert.Equal("Hello"u8.ToArray(), files.Single(x => x.Name == "hello.txt").Bytes.ToArray());

            var names = updatedDocument.Structure.Catalog.CatalogDictionary.Get<DictionaryToken>(NameToken.Names, updatedDocument.Structure.TokenScanner);
            Assert.True(names.ContainsKey(NameToken.JavaScript));
        }

        [Fact]
        public void AddedEmbeddedFileKeepsTheEmbeddedFilesOfNameTreeWithKids()
        {
            // The root of the name tree has Kids with one leaf, which holds the embedded file LNCS All.joboptions.
            using var document = PdfDocument.Open(IntegrationHelpers.GetSpecificTestDocumentPath("PDFBOX-659-0.pdf"));
            Assert.True(document.Advanced.TryGetEmbeddedFiles(out var originalFiles));
            var originalFile = Assert.Single(originalFiles);

            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddEmbeddedFile("hello.txt", "Hello"u8.ToArray());

            using var updatedDocument = PdfDocument.Open(builder.Build());
            Assert.True(updatedDocument.Advanced.TryGetEmbeddedFiles(out var files));

            Assert.Equal(new[] { "LNCS All.joboptions", "hello.txt" }, files.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
            Assert.Equal(originalFile.Bytes.ToArray(), files.Single(x => x.Name == "LNCS All.joboptions").Bytes.ToArray());

            // A root has either Kids or Names (ISO 32000-1, 7.9.6), and the update writes all keys to the root.
            var scanner = updatedDocument.Structure.TokenScanner;
            var names = updatedDocument.Structure.Catalog.CatalogDictionary.Get<DictionaryToken>(NameToken.Names, scanner);
            var tree = names.Get<DictionaryToken>(NameToken.EmbeddedFiles, scanner);
            Assert.False(tree.ContainsKey(NameToken.Kids));
            Assert.Equal(4, tree.Get<ArrayToken>(NameToken.Names, scanner).Length);
            Assert.True(names.ContainsKey(NameToken.Dests));
        }

        [Fact]
        public void KeyIsWrittenAsUtf16IfAllKeysOfTheDocumentAre()
        {
            // The name tree (object 34) has the key KSBASE.WQ2 written as UTF-16 with the byte order mark FE FF, for
            // 37 0 R. It is referred to by the names dictionary (33), which the catalog (23) refers to.
            using var document = PdfDocument.Open(IntegrationHelpers.GetSpecificTestDocumentPath("embedded-file-attachment.pdf"));
            var originalNames = GetEmbeddedFilesNameTree(document);

            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddEmbeddedFile("a.txt", "Hello"u8.ToArray());

            using var updatedDocument = PdfDocument.Open(builder.Build());
            var names = GetEmbeddedFilesNameTree(updatedDocument);

            // The existing key keeps its bytes and its value, and a.txt follows its encoding.
            Assert.Equal(Assert.IsType<StringToken>(originalNames[0]).Bytes.ToArray(), Assert.IsType<StringToken>(names[0]).Bytes.ToArray());
            Assert.Equal(new IndirectReferenceToken(new IndirectReference(37, 0)), names[1]);
            Assert.Equal(
                new byte[] { 0xFE, 0xFF, 0x00, 0x61, 0x00, 0x2E, 0x00, 0x74, 0x00, 0x78, 0x00, 0x74 },
                Assert.IsType<StringToken>(names[2]).Bytes.ToArray());

            // Only the name tree is replaced, where it is.
            var update = updatedDocument.Structure.CrossReferenceTable.Parts[0].ObjectOffsets.Keys;
            Assert.Contains(new IndirectReference(34, 0), update);
            Assert.DoesNotContain(new IndirectReference(33, 0), update);
            Assert.DoesNotContain(new IndirectReference(23, 0), update);
        }

        [Fact]
        public void KeyIsWrittenAsUtf16IfTheKeyThatAReplacedFileKeepsIs()
        {
            // The only key, KSBASE.WQ2, is written as UTF-16 with the byte order mark FE FF.
            using var document = PdfDocument.Open(IntegrationHelpers.GetSpecificTestDocumentPath("embedded-file-attachment.pdf"));
            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.RemoveEmbeddedFile("KSBASE.WQ2");
            builder.AddEmbeddedFile("KSBASE.WQ2", "Hello"u8.ToArray());
            builder.AddEmbeddedFile("a.txt", "Hello"u8.ToArray());

            using var updatedDocument = PdfDocument.Open(builder.Build());
            var names = GetEmbeddedFilesNameTree(updatedDocument);
            Assert.Equal(
                new byte[] { 0xFE, 0xFF, 0x00, 0x61, 0x00, 0x2E, 0x00, 0x74, 0x00, 0x78, 0x00, 0x74 },
                Assert.IsType<StringToken>(names[2]).Bytes.ToArray());
        }

        [Fact]
        public void EmbeddedFilesAreSortedByteByByte()
        {
            // The key a.txt is written in PDFDocEncoding and A.txt as UTF-16 with the byte order mark FE FF.
            var original = CreateDocument(
                @"<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles << /Names [(a.txt) 4 0 R (\376\377\000A\000.\000t\000x\000t) 4 0 R] >> >> >>",
                Pages,
                Page,
                FileSpecification,
                EmbeddedFile);

            using var document = PdfDocument.Open(original);
            var builder = new PdfIncrementalUpdateBuilder(document);
            foreach (var name in new[] { "ü.txt", "€.txt", "b.txt" })
            {
                builder.AddEmbeddedFile(name, "Hello"u8.ToArray());
            }

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);

            // Not all keys are UTF-16, so the added keys are written in PDFDocEncoding, in which € is A0 and ü is FC.
            Assert.Equal(
                new[] { "a.txt", "b.txt", "€.txt", "ü.txt", "A.txt" },
                GetEmbeddedFilesNameTree(updatedDocument).Data.Where((_, i) => i % 2 == 0).Select(x => ((IDataToken<string>)x).Data));
        }

        [Fact]
        public void CatalogWithDirectNamesDictionaryIsReplacedWithItsOtherEntries()
        {
            var original = CreateDocument(
                "<< /Type /Catalog /Pages 2 0 R /PageMode /UseOutlines /Names << /Dests << /Names [(chapter) [3 0 R /Fit]] >> /EmbeddedFiles << /Names [(a.txt) 4 0 R] >> >> >>",
                Pages,
                Page,
                FileSpecification,
                EmbeddedFile);

            using var document = PdfDocument.Open(original);
            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddEmbeddedFile("b.txt", "Hello"u8.ToArray());

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            Assert.True(updatedDocument.Advanced.TryGetEmbeddedFiles(out var files));
            Assert.Equal(new[] { "a.txt", "b.txt" }, files.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
            Assert.Equal("hello"u8.ToArray(), files.Single(x => x.Name == "a.txt").Bytes.ToArray());

            var scanner = updatedDocument.Structure.TokenScanner;
            var catalog = updatedDocument.Structure.Catalog.CatalogDictionary;
            Assert.Contains(updatedDocument.Structure.Trailer.Root, updatedDocument.Structure.CrossReferenceTable.Parts[0].ObjectOffsets.Keys);
            Assert.Equal(NameToken.Create("UseOutlines"), catalog.Get<NameToken>(NameToken.PageMode, scanner));
            Assert.True(catalog.Get<DictionaryToken>(NameToken.Names, scanner).ContainsKey(NameToken.Dests));
        }

        [Fact]
        public void EmbeddedFileHasTheMimeTypeDescriptionAndDateOfTheOptions()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("data.pdf"));
            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddEmbeddedFile("result.json", "{}"u8.ToArray(), new EmbeddedFileOptions
            {
                MimeType = "application/json",
                Description = "The result",
                ModifiedDate = new DateTimeOffset(2026, 11, 2, 9, 30, 0, TimeSpan.FromHours(1))
            });

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            var scanner = updatedDocument.Structure.TokenScanner;
            Assert.True(updatedDocument.Advanced.TryGetEmbeddedFiles(out var files));

            var stream = Assert.Single(files).Stream.StreamDictionary;
            Assert.Equal("application/json", stream.Get<NameToken>(NameToken.Subtype, scanner).Data);
            Assert.Equal("D:20261102093000+01'00", stream.Get<DictionaryToken>(NameToken.Params, scanner).Get<StringToken>(NameToken.ModDate, scanner).Data);

            var fileSpecification = Assert.IsType<DictionaryToken>(updatedDocument.Structure.GetObject(((IndirectReferenceToken)GetEmbeddedFilesNameTree(updatedDocument)[1]).Data).Data);
            Assert.Equal("The result", fileSpecification.Get<StringToken>(NameToken.Desc, scanner).Data);
        }

        [Theory]
        [InlineData(PdfAStandard.A3B)]
        [InlineData(PdfAStandard.A3A)]
        public void PdfA3DocumentAssociatesTheAddedEmbeddedFileWithTheDocument(PdfAStandard standard)
        {
            using var document = PdfDocument.Open(CreatePdfADocumentWithInvoice(standard));
            var originalAssociatedFiles = document.Structure.Catalog.CatalogDictionary.Get<ArrayToken>(NameToken.Af, document.Structure.TokenScanner);

            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddEmbeddedFile("result.json", "{}"u8.ToArray(), new EmbeddedFileOptions
            {
                MimeType = "application/json",
                Relationship = AssociatedFileRelationship.Supplement
            });

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            var scanner = updatedDocument.Structure.TokenScanner;

            // invoice.xml comes before result.json.
            var added = Assert.IsType<IndirectReferenceToken>(GetEmbeddedFilesNameTree(updatedDocument)[3]);
            var addedFileSpecification = Assert.IsType<DictionaryToken>(updatedDocument.Structure.GetObject(added.Data).Data);
            Assert.Equal(NameToken.Create("Supplement"), addedFileSpecification.Get<NameToken>(NameToken.AfRelationship, scanner));

            var associatedFiles = updatedDocument.Structure.Catalog.CatalogDictionary.Get<ArrayToken>(NameToken.Af, scanner);
            Assert.Equal(originalAssociatedFiles.Data.Append(added), associatedFiles.Data);
        }

        [Fact]
        public void IndirectAssociatedFilesOfPdfA3DocumentAreReplacedWhereTheyAre()
        {
            var original = CreateDocument(
                "<< /Type /Catalog /Pages 2 0 R /Metadata 6 0 R /AF 7 0 R /Names 8 0 R >>",
                Pages,
                Page,
                "<< /Type /Filespec /F (a.txt) /UF (a.txt) /AFRelationship /Data /EF << /F 5 0 R >> >>",
                EmbeddedFile,
                Metadata("<pdfaid:part>3</pdfaid:part><pdfaid:conformance>B</pdfaid:conformance>"),
                "[4 0 R]",
                "<< /EmbeddedFiles << /Names [(a.txt) 4 0 R] >> >>");

            using var document = PdfDocument.Open(original);
            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddEmbeddedFile("b.txt", "Hello"u8.ToArray());

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            var added = Assert.IsType<IndirectReferenceToken>(GetEmbeddedFilesNameTree(updatedDocument)[3]);
            var associatedFiles = Assert.IsType<ArrayToken>(updatedDocument.Structure.GetObject(new IndirectReference(7, 0)).Data);
            Assert.Equal(new IToken[] { new IndirectReferenceToken(new IndirectReference(4, 0)), added }, associatedFiles.Data);

            // The associated files (7) and the names dictionary (8) are replaced where they are, the catalog (1) stays.
            var update = updatedDocument.Structure.CrossReferenceTable.Parts[0].ObjectOffsets.Keys;
            Assert.Contains(new IndirectReference(7, 0), update);
            Assert.Contains(new IndirectReference(8, 0), update);
            Assert.DoesNotContain(new IndirectReference(1, 0), update);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        public void EmbeddedFilesAreOnlySupportedForPdfA3(int part)
        {
            var original = CreateDocument(
                "<< /Type /Catalog /Pages 2 0 R /Metadata 4 0 R >>",
                Pages,
                Page,
                Metadata($"<pdfaid:part>{part}</pdfaid:part><pdfaid:conformance>B</pdfaid:conformance>"));

            using var document = PdfDocument.Open(original);
            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddEmbeddedFile("hello.txt", "Hello"u8.ToArray());

            using var output = new MemoryStream();
            Assert.Throws<NotSupportedException>(() => builder.Build(output));
            Assert.Equal(0, output.Length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(4)]
        public void RemovingEmbeddedFilesIsOnlySupportedForPdfA3(int part)
        {
            var original = CreateDocument(
                "<< /Type /Catalog /Pages 2 0 R /Metadata 6 0 R /Names << /EmbeddedFiles << /Names [(a.txt) 4 0 R] >> >> >>",
                Pages,
                Page,
                FileSpecification,
                EmbeddedFile,
                Metadata($"<pdfaid:part>{part}</pdfaid:part><pdfaid:conformance>B</pdfaid:conformance>"));

            using var document = PdfDocument.Open(original);
            var builder = new PdfIncrementalUpdateBuilder(document);
            Assert.True(builder.RemoveEmbeddedFile("a.txt"));

            Assert.Throws<NotSupportedException>(() => builder.Build());
        }

        [Theory]
        [InlineData("<< /Type /Metadata /Subtype /XML /Filter /FlatDecode /Length 5 >>\nstream\nhello\nendstream")]
        [InlineData("<< /Type /Metadata /Subtype /XML /Length 5 >>\nstream\nhello\nendstream")]
        [InlineData("<< /Type /Metadata /Subtype /XML /Filter /ASCIIHexDecode /Length 3 >>\nstream\nzz>\nendstream")]
        [InlineData("<< /Type /Metadata /Subtype /XML /Filter /ASCII85Decode /Length 4 >>\nstream\naz~>\nendstream")]
        public void MetadataThatCannotBeReadStatesNoPdfA(string metadata)
        {
            // An unknown filter (a misspelled FlateDecode), metadata that is not XML, and data that the filters cannot decode.
            var original = CreateDocument("<< /Type /Catalog /Pages 2 0 R /Metadata 4 0 R >>", Pages, Page, metadata);

            using var document = PdfDocument.Open(original);
            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddEmbeddedFile("hello.txt", "Hello"u8.ToArray());

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            Assert.True(updatedDocument.Advanced.TryGetEmbeddedFiles(out _));
            Assert.False(updatedDocument.Structure.Catalog.CatalogDictionary.ContainsKey(NameToken.Af));
        }

        [Fact]
        public void MetadataWithDocumentTypeDeclarationIsReadWithoutIt()
        {
            var original = CreateDocument(
                "<< /Type /Catalog /Pages 2 0 R /Metadata 4 0 R >>",
                Pages,
                Page,
                Metadata(PdfA3Identification, documentType: "<!DOCTYPE x:xmpmeta [<!ENTITY outside SYSTEM \"file:///etc/hosts\">]>"));

            using var document = PdfDocument.Open(original);
            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddEmbeddedFile("hello.txt", "Hello"u8.ToArray());

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            Assert.True(updatedDocument.Structure.Catalog.CatalogDictionary.ContainsKey(NameToken.Af));
        }

        [Fact]
        public void DocumentThatIsNotPdfA3GetsNoAssociatedFileEntries()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("data.pdf"));
            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddEmbeddedFile("result.json", "{}"u8.ToArray(), new EmbeddedFileOptions
            {
                Relationship = AssociatedFileRelationship.Supplement
            });

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            var fileSpecification = Assert.IsType<DictionaryToken>(updatedDocument.Structure.GetObject(((IndirectReferenceToken)GetEmbeddedFilesNameTree(updatedDocument)[1]).Data).Data);

            Assert.False(fileSpecification.ContainsKey(NameToken.AfRelationship));
            Assert.False(updatedDocument.Structure.Catalog.CatalogDictionary.ContainsKey(NameToken.Af));
        }

        [Fact]
        public void PdfA3IdentificationIsAlsoReadFromAttributes()
        {
            var original = CreateDocument(
                "<< /Type /Catalog /Pages 2 0 R /Metadata 4 0 R >>",
                Pages,
                Page,
                Metadata(string.Empty, " pdfaid:part=\"3\" pdfaid:conformance=\"B\""));

            using var document = PdfDocument.Open(original);
            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddEmbeddedFile("hello.txt", "Hello"u8.ToArray());

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            var added = GetEmbeddedFilesNameTree(updatedDocument)[1];
            var associatedFiles = updatedDocument.Structure.Catalog.CatalogDictionary.Get<ArrayToken>(NameToken.Af, updatedDocument.Structure.TokenScanner);
            Assert.Equal(added, Assert.Single(associatedFiles.Data));
        }

        [Fact]
        public void PdfA2DocumentWhoseIdentificationTheUpdateChangesToPdfA3CanHaveEmbeddedFiles()
        {
            var original = CreateDocument(
                "<< /Type /Catalog /Pages 2 0 R /Metadata 4 0 R >>",
                Pages,
                Page,
                Metadata("<pdfaid:part>2</pdfaid:part><pdfaid:conformance>B</pdfaid:conformance>"));

            using var document = PdfDocument.Open(original);
            var builder = new PdfIncrementalUpdateBuilder(document);

            // The update states PDF/A-3 instead of PDF/A-2, which only allows PDF/A documents as embedded files.
            var metadata = Metadata("<pdfaid:part>3</pdfaid:part><pdfaid:conformance>B</pdfaid:conformance>");
            var xmp = metadata.Substring(metadata.IndexOf("stream\n", StringComparison.Ordinal) + 7).Replace("\nendstream", string.Empty);
            builder.ReplaceObject(new IndirectReference(4, 0), new StreamToken(
                new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.Type, NameToken.Metadata },
                    { NameToken.Subtype, NameToken.Xml }
                }),
                Encoding.UTF8.GetBytes(xmp)));
            builder.AddEmbeddedFile("hello.txt", "Hello"u8.ToArray());

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            var added = GetEmbeddedFilesNameTree(updatedDocument)[1];
            var associatedFiles = updatedDocument.Structure.Catalog.CatalogDictionary.Get<ArrayToken>(NameToken.Af, updatedDocument.Structure.TokenScanner);
            Assert.Equal(added, Assert.Single(associatedFiles.Data));
        }

        [Fact]
        public void AddEmbeddedFileThrowsForNameOfTheDocument()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetSpecificTestDocumentPath("embedded-file-attachment.pdf"));
            var builder = new PdfIncrementalUpdateBuilder(document);

            // The document has the key written as UTF-16, the name compares as text.
            Assert.Throws<ArgumentException>(() => builder.AddEmbeddedFile("KSBASE.WQ2", "Hello"u8.ToArray()));
        }

        [Fact]
        public void AddEmbeddedFileThrowsForNameThatIsAlreadyAdded()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("data.pdf"));
            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddEmbeddedFile("hello.txt", "Hello"u8.ToArray());

            Assert.Throws<ArgumentException>(() => builder.AddEmbeddedFile("hello.txt", "Hello again"u8.ToArray()));
        }

        [Fact]
        public void HexStringKeyWithTheBytesOfTheNameIsTaken()
        {
            // 85 is the en dash in PDFDocEncoding, the encoding a name of these characters is written in.
            var original = CreateDocument(
                $"<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles << /Names [<{HexOf("Rechnung ")}85{HexOf(" Okt.xml")}> 4 0 R] >> >> >>",
                Pages,
                Page,
                FileSpecification,
                EmbeddedFile);

            using var document = PdfDocument.Open(original);
            Assert.Throws<ArgumentException>(() => new PdfIncrementalUpdateBuilder(document).AddEmbeddedFile("Rechnung – Okt.xml", "Hello"u8.ToArray()));

            var builder = new PdfIncrementalUpdateBuilder(document);
            Assert.True(builder.RemoveEmbeddedFile("Rechnung – Okt.xml"));

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            Assert.False(updatedDocument.Advanced.TryGetEmbeddedFiles(out _));
        }

        [Fact]
        public void EntryWithNameAsKeyIsKept()
        {
            // Keys shall be strings, but PdfPig reads a name too.
            var original = CreateDocument(
                "<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles << /Names [/a.txt 4 0 R] >> >> >>",
                Pages,
                Page,
                FileSpecification,
                EmbeddedFile);

            using var document = PdfDocument.Open(original);
            var builder = new PdfIncrementalUpdateBuilder(document);
            Assert.Throws<ArgumentException>(() => builder.AddEmbeddedFile("a.txt", "Hello"u8.ToArray()));
            builder.AddEmbeddedFile("b.txt", "Hello"u8.ToArray());

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            Assert.True(updatedDocument.Advanced.TryGetEmbeddedFiles(out var files));
            Assert.Equal(new[] { "a.txt", "b.txt" }, files.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
        }

        [Fact]
        public void EmptyNameIsNotTheNameOfEntryWithNameAsKey()
        {
            var original = CreateDocument(
                "<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles << /Names [/a.txt 4 0 R] >> >> >>",
                Pages,
                Page,
                FileSpecification,
                EmbeddedFile);

            using var document = PdfDocument.Open(original);

            Assert.False(new PdfIncrementalUpdateBuilder(document).RemoveEmbeddedFile(string.Empty));
        }

        [Fact]
        public void EntryWithNameAsKeyGetsStringAsKeyWhenItsFileIsReplaced()
        {
            var original = CreateDocument(
                "<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles << /Names [/a.txt 4 0 R] >> >> >>",
                Pages,
                Page,
                FileSpecification,
                EmbeddedFile);

            using var document = PdfDocument.Open(original);
            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.RemoveEmbeddedFile("a.txt");
            builder.AddEmbeddedFile("a.txt", "Hello"u8.ToArray());

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            Assert.Equal("a.txt", Assert.IsType<StringToken>(GetEmbeddedFilesNameTree(updatedDocument)[0]).Data);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void AddEmbeddedFileThrowsForMissingName(string? name)
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("data.pdf"));
            var builder = new PdfIncrementalUpdateBuilder(document);

            Assert.ThrowsAny<ArgumentException>(() => builder.AddEmbeddedFile(name, "Hello"u8.ToArray()));
        }

        [Fact]
        public void AddEmbeddedFileThrowsForMissingOptions()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("data.pdf"));
            var builder = new PdfIncrementalUpdateBuilder(document);

            Assert.Throws<ArgumentNullException>(() => builder.AddEmbeddedFile("hello.txt", "Hello"u8.ToArray(), null));
        }

        [Fact]
        public void AddEmbeddedFileThrowsForInvalidMimeType()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("data.pdf"));
            var builder = new PdfIncrementalUpdateBuilder(document);

            Assert.Throws<ArgumentException>(() => builder.AddEmbeddedFile("hello.txt", "Hello"u8.ToArray(), new EmbeddedFileOptions
            {
                MimeType = "text/xml; charset=utf-8"
            }));
        }

        [Fact]
        public void RemovedEmbeddedFileIsNotInTheUpdatedDocument()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetSpecificTestDocumentPath("embedded-file-attachment.pdf"));
            var builder = new PdfIncrementalUpdateBuilder(document);

            Assert.True(builder.RemoveEmbeddedFile("KSBASE.WQ2"));

            using var updatedDocument = PdfDocument.Open(builder.Build());
            Assert.False(updatedDocument.Advanced.TryGetEmbeddedFiles(out _));

            // The names dictionary keeps its other name tree but loses the empty one.
            var names = updatedDocument.Structure.Catalog.CatalogDictionary.Get<DictionaryToken>(NameToken.Names, updatedDocument.Structure.TokenScanner);
            Assert.True(names.ContainsKey(NameToken.JavaScript));
            Assert.False(names.ContainsKey(NameToken.EmbeddedFiles));
        }

        [Fact]
        public void RemovedEmbeddedFileIsNoLongerAssociatedWithPdfA3Document()
        {
            using var document = PdfDocument.Open(CreatePdfADocumentWithInvoice(PdfAStandard.A3B));
            var builder = new PdfIncrementalUpdateBuilder(document);

            Assert.True(builder.RemoveEmbeddedFile("invoice.xml"));

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            Assert.False(updatedDocument.Advanced.TryGetEmbeddedFiles(out _));

            // The associated files are an array of one or more file specifications (ISO 32000-2, Table 29), so it is removed.
            Assert.False(updatedDocument.Structure.Catalog.CatalogDictionary.ContainsKey(NameToken.Af));
        }

        [Fact]
        public void FileSpecificationOfOtherNameStaysAssociatedWhenOneNameIsRemoved()
        {
            var original = CreateDocument(
                "<< /Type /Catalog /Pages 2 0 R /Metadata 6 0 R /AF [4 0 R] /Names << /EmbeddedFiles << /Names [(a.xml) 4 0 R (b.xml) 4 0 R] >> >> >>",
                Pages,
                Page,
                "<< /Type /Filespec /F (a.xml) /UF (a.xml) /AFRelationship /Data /EF << /F 5 0 R >> >>",
                EmbeddedFile,
                Metadata(PdfA3Identification));

            using var document = PdfDocument.Open(original);
            var builder = new PdfIncrementalUpdateBuilder(document);
            Assert.True(builder.RemoveEmbeddedFile("a.xml"));

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            Assert.True(updatedDocument.Advanced.TryGetEmbeddedFiles(out var files));
            Assert.Equal("b.xml", Assert.Single(files).Name);

            var associatedFiles = updatedDocument.Structure.Catalog.CatalogDictionary.Get<ArrayToken>(NameToken.Af, updatedDocument.Structure.TokenScanner);
            Assert.Equal(new IndirectReferenceToken(new IndirectReference(4, 0)), Assert.Single(associatedFiles.Data));
        }

        [Fact]
        public void FileSpecificationThatAnnotationRefersToStaysAssociatedWithPdfA3Document()
        {
            // The file attachment annotation (7) has no associated files of its own, so the catalog keeps the file specification (4).
            var original = CreateDocument(
                "<< /Type /Catalog /Pages 2 0 R /Metadata 6 0 R /AF [4 0 R] /Names << /EmbeddedFiles << /Names [(a.txt) 4 0 R] >> >> >>",
                Pages,
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Annots [7 0 R] >>",
                "<< /Type /Filespec /F (a.txt) /UF (a.txt) /AFRelationship /Data /EF << /F 5 0 R >> >>",
                EmbeddedFile,
                Metadata(PdfA3Identification),
                "<< /Type /Annot /Subtype /FileAttachment /Rect [10 10 30 30] /FS 4 0 R >>");

            using var document = PdfDocument.Open(original);
            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.RemoveEmbeddedFile("a.txt");

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            Assert.False(updatedDocument.Advanced.TryGetEmbeddedFiles(out _));
            Assert.IsType<DictionaryToken>(updatedDocument.Structure.GetObject(new IndirectReference(4, 0)).Data);

            var associatedFiles = updatedDocument.Structure.Catalog.CatalogDictionary.Get<ArrayToken>(NameToken.Af, updatedDocument.Structure.TokenScanner);
            Assert.Equal(new IndirectReferenceToken(new IndirectReference(4, 0)), Assert.Single(associatedFiles.Data));
        }

        [Fact]
        public void ObjectThatIsNotFileSpecificationIsKeptWhenItsNameIsRemoved()
        {
            // A damaged name tree that lists the catalog as an embedded file.
            var original = CreateDocument(
                "<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles << /Names [(a.txt) 1 0 R] >> >> >>",
                Pages,
                Page);

            using var document = PdfDocument.Open(original);
            var builder = new PdfIncrementalUpdateBuilder(document);
            Assert.True(builder.RemoveEmbeddedFile("a.txt"));

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            Assert.Equal(1, updatedDocument.NumberOfPages);
        }

        [Fact]
        public void FileSpecificationInIndirectNamesOfLeafIsReplacedWithNull()
        {
            // The leaf (5) of the name tree has its names in an indirect array (6).
            var original = CreateDocument(
                "<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles 4 0 R >> >>",
                Pages,
                Page,
                "<< /Kids [5 0 R] >>",
                "<< /Limits [(a.txt) (a.txt)] /Names 6 0 R >>",
                "[(a.txt) 7 0 R]",
                "<< /Type /Filespec /F (a.txt) /UF (a.txt) /EF << /F 8 0 R >> >>",
                EmbeddedFile);

            using var document = PdfDocument.Open(original);
            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.RemoveEmbeddedFile("a.txt");

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            Assert.IsType<NullToken>(updatedDocument.Structure.GetObject(new IndirectReference(7, 0)).Data);
        }

        [Fact]
        public void FileSpecificationOfRemovedEmbeddedFileIsReplacedWithNull()
        {
            // PDF/A-3 requires each embedded file to be associated with the document, also one that nothing refers to any more.
            using var document = PdfDocument.Open(CreatePdfADocumentWithInvoice(PdfAStandard.A3B));
            var fileSpecification = Assert.IsType<IndirectReferenceToken>(GetEmbeddedFilesNameTree(document)[1]);

            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.RemoveEmbeddedFile("invoice.xml");

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            Assert.IsType<NullToken>(updatedDocument.Structure.GetObject(fileSpecification.Data).Data);
        }

        [Fact]
        public void FileSpecificationOfRemovedEmbeddedFileInLeafOfNameTreeIsReplacedWithNull()
        {
            // The leaf (245) of the name tree refers to the file specification (246), but the update writes a new root.
            using var document = PdfDocument.Open(IntegrationHelpers.GetSpecificTestDocumentPath("PDFBOX-659-0.pdf"));
            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.RemoveEmbeddedFile("LNCS All.joboptions");

            using var updatedDocument = PdfDocument.Open(builder.Build());
            Assert.IsType<NullToken>(updatedDocument.Structure.GetObject(new IndirectReference(246, 0)).Data);
        }

        [Theory]
        [InlineData(
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Annots [6 0 R] >>",
            "<< /Type /Annot /Subtype /FileAttachment /Rect [10 10 30 30] /FS 4 0 R >>")]
        [InlineData(
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 200 200] /Resources << /XObject << /X 6 0 R >> >> >>",
            "<< /Type /XObject /Subtype /Form /BBox [0 0 10 10] /AF [4 0 R] /Length 0 >>\nstream\n\nendstream")]
        public void FileSpecificationThatAnotherObjectRefersToIsKeptWhenItsEmbeddedFileIsRemoved(string page, string otherObject)
        {
            // A file attachment annotation (6) of the page, or a form XObject (6) in the associated files of its stream
            // dictionary, refers to the file specification (4) too.
            var original = CreateDocument(
                "<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles << /Names [(a.txt) 4 0 R] >> >> >>",
                Pages,
                page,
                FileSpecification,
                EmbeddedFile,
                otherObject);

            using var document = PdfDocument.Open(original);
            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.RemoveEmbeddedFile("a.txt");

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            Assert.False(updatedDocument.Advanced.TryGetEmbeddedFiles(out _));
            Assert.IsType<DictionaryToken>(updatedDocument.Structure.GetObject(new IndirectReference(4, 0)).Data);
        }

        [Fact]
        public void RemoveEmbeddedFileReturnsFalseForNameTheDocumentDoesNotHave()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetSpecificTestDocumentPath("embedded-file-attachment.pdf"));
            var builder = new PdfIncrementalUpdateBuilder(document);

            Assert.False(builder.RemoveEmbeddedFile("hello.txt"));
        }

        [Fact]
        public void EmbeddedFileThatIsAddedAndRemovedAgainIsNotWritten()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("data.pdf"));
            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddEmbeddedFile("hello.txt", "Hello"u8.ToArray());

            Assert.True(builder.RemoveEmbeddedFile("hello.txt"));

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            Assert.False(updatedDocument.Advanced.TryGetEmbeddedFiles(out _));
            Assert.Empty(updatedDocument.Structure.CrossReferenceTable.Parts[0].ObjectOffsets);
            Assert.Equal(document.Structure.Trailer.Size, updatedDocument.Structure.Trailer.Size);
        }

        [Fact]
        public void EmbeddedFileThatIsRemovedAgainLeavesNoGapInTheObjectNumbers()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("data.pdf"));
            var size = document.Structure.Trailer.Size;

            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddEmbeddedFile("a.txt", "Hello"u8.ToArray());
            builder.AddEmbeddedFile("b.txt", "Hello"u8.ToArray());
            builder.RemoveEmbeddedFile("a.txt");

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);

            // The new objects are the embedded file stream and the file specification of b.txt.
            var added = updatedDocument.Structure.CrossReferenceTable.Parts[0].ObjectOffsets.Keys.Where(x => x.ObjectNumber >= size);
            Assert.Equal(new long[] { size, size + 1 }, added.Select(x => x.ObjectNumber).OrderBy(x => x));
            Assert.Equal(size + 2, updatedDocument.Structure.Trailer.Size);
        }

        [Fact]
        public void EmbeddedFileIsReplacedByRemovingAndAddingIt()
        {
            using var document = PdfDocument.Open(CreatePdfADocumentWithInvoice(PdfAStandard.A3B));
            var originalFileSpecification = Assert.Single(document.Structure.Catalog.CatalogDictionary.Get<ArrayToken>(NameToken.Af, document.Structure.TokenScanner).Data);

            var builder = new PdfIncrementalUpdateBuilder(document);
            Assert.True(builder.RemoveEmbeddedFile("invoice.xml"));
            builder.AddEmbeddedFile("invoice.xml", "<Invoice>2</Invoice>"u8.ToArray(), new EmbeddedFileOptions
            {
                MimeType = "text/xml",
                Relationship = AssociatedFileRelationship.Alternative
            });

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            Assert.True(updatedDocument.Advanced.TryGetEmbeddedFiles(out var files));
            Assert.Equal("<Invoice>2</Invoice>"u8.ToArray(), Assert.Single(files).Bytes.ToArray());

            // The associated files only list the new file specification.
            var added = GetEmbeddedFilesNameTree(updatedDocument)[1];
            var associatedFiles = updatedDocument.Structure.Catalog.CatalogDictionary.Get<ArrayToken>(NameToken.Af, updatedDocument.Structure.TokenScanner);
            Assert.Equal(added, Assert.Single(associatedFiles.Data));
            Assert.NotEqual(originalFileSpecification, added);
        }

        [Fact]
        public void ReplacedEmbeddedFileKeepsItsKey()
        {
            // The key a.txt is written in PDFDocEncoding and b.txt as UTF-16 with the byte order mark FE FF.
            var original = CreateDocument(
                @"<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles << /Names [(a.txt) 4 0 R (\376\377\000b\000.\000t\000x\000t) 6 0 R] >> >> >>",
                Pages,
                Page,
                FileSpecification,
                EmbeddedFile,
                "<< /Type /Filespec /F (b.txt) /UF (b.txt) /EF << /F 7 0 R >> >>",
                EmbeddedFile);

            using var document = PdfDocument.Open(original);
            var originalNames = GetEmbeddedFilesNameTree(document);

            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.RemoveEmbeddedFile("b.txt");
            builder.AddEmbeddedFile("b.txt", "Hello"u8.ToArray());

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            var names = GetEmbeddedFilesNameTree(updatedDocument);
            Assert.Equal(Assert.IsType<StringToken>(originalNames[2]).Bytes.ToArray(), Assert.IsType<StringToken>(names[2]).Bytes.ToArray());
            Assert.NotEqual(originalNames[3], names[3]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void EmbeddedFileKeepsTheCatalogThatTheUpdateReplaces(bool replaceCatalogFirst)
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("data.pdf"));
            var builder = new PdfIncrementalUpdateBuilder(document);

            void ReplaceCatalog() => builder.ReplaceObject(document.Structure.Trailer.Root,
                document.Structure.Catalog.CatalogDictionary.With(NameToken.PageMode, NameToken.Create("UseOutlines")));

            if (replaceCatalogFirst)
            {
                ReplaceCatalog();
                builder.AddEmbeddedFile("hello.txt", "Hello"u8.ToArray());
            }
            else
            {
                builder.AddEmbeddedFile("hello.txt", "Hello"u8.ToArray());
                ReplaceCatalog();
            }

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            Assert.True(updatedDocument.Advanced.TryGetEmbeddedFiles(out var files));
            Assert.Equal("hello.txt", Assert.Single(files).Name);
            Assert.Equal(NameToken.Create("UseOutlines"), updatedDocument.Structure.Catalog.CatalogDictionary.Get<NameToken>(NameToken.PageMode, updatedDocument.Structure.TokenScanner));
        }

        [Fact]
        public void ObjectAddedAfterEmbeddedFileIsWrittenToo()
        {
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("data.pdf"));
            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddEmbeddedFile("hello.txt", "Hello"u8.ToArray());
            var reference = builder.AddObject(new StringToken("Added after the file"));

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            Assert.True(updatedDocument.Advanced.TryGetEmbeddedFiles(out var files));
            Assert.Equal("Hello"u8.ToArray(), Assert.Single(files).Bytes.ToArray());
            Assert.Equal("Added after the file", Assert.IsType<StringToken>(updatedDocument.Structure.GetObject(reference.Data).Data).Data);
        }

        [Fact]
        public void BuilderWithEmbeddedFilesCanBuildTwice()
        {
            // The name tree (34) has the file KSBASE.WQ2 with the file specification 37 0 R.
            using var document = PdfDocument.Open(IntegrationHelpers.GetSpecificTestDocumentPath("embedded-file-attachment.pdf"));
            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.RemoveEmbeddedFile("KSBASE.WQ2");
            builder.AddEmbeddedFile("hello.txt", "Hello"u8.ToArray());

            using var firstDocument = PdfDocument.Open(builder.Build());
            using var updatedDocument = PdfDocument.Open(builder.Build());

            Assert.Equal(2, GetEmbeddedFilesNameTree(updatedDocument).Length);
            Assert.Equal("hello.txt", ((IDataToken<string>)GetEmbeddedFilesNameTree(updatedDocument)[0]).Data);
            Assert.IsType<NullToken>(updatedDocument.Structure.GetObject(new IndirectReference(37, 0)).Data);

            // Both updates write the same objects.
            Assert.Equal(
                firstDocument.Structure.CrossReferenceTable.Parts[0].ObjectOffsets.Keys.OrderBy(x => x.ObjectNumber),
                updatedDocument.Structure.CrossReferenceTable.Parts[0].ObjectOffsets.Keys.OrderBy(x => x.ObjectNumber));
            Assert.Equal(firstDocument.Structure.Trailer.Size, updatedDocument.Structure.Trailer.Size);
        }

        [Theory]
        [InlineData("Hang.pdf")]
        [InlineData("FailedToParseContentForPage32.pdf")]
        public void EmbeddedFileOfEarlierUpdateIsKeptInDamagedDocument(string documentName)
        {
            // Reading these damaged documents searches the file for objects, after which the scanner returns the original
            // catalog instead of the one of the first update.
            byte[] first;
            using (var document = PdfDocument.Open(IntegrationHelpers.GetSpecificTestDocumentPath(documentName)))
            {
                var builder = new PdfIncrementalUpdateBuilder(document);
                builder.AddEmbeddedFile("a.txt", "Hello"u8.ToArray());
                first = builder.Build();
            }

            using var updatedDocument = PdfDocument.Open(first);
            var secondBuilder = new PdfIncrementalUpdateBuilder(updatedDocument);
            secondBuilder.AddEmbeddedFile("b.txt", "Hello"u8.ToArray());

            using var secondUpdatedDocument = PdfDocument.Open(secondBuilder.Build());
            Assert.True(secondUpdatedDocument.Advanced.TryGetEmbeddedFiles(out var files));
            Assert.Equal(new[] { "a.txt", "b.txt" }, files.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
        }

        [Fact]
        public void EmbeddedFilesNameTreeWithCycleOfKidsIsReadOnce()
        {
            // The root (4) lists a leaf (5) and itself.
            var original = CreateDocument(
                "<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles 4 0 R >> >>",
                Pages,
                Page,
                "<< /Kids [5 0 R 4 0 R] >>",
                "<< /Limits [(a.txt) (a.txt)] /Names [(a.txt) 6 0 R] >>",
                "<< /Type /Filespec /F (a.txt) /UF (a.txt) /EF << /F 7 0 R >> >>",
                EmbeddedFile);

            using var document = PdfDocument.Open(original);
            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddEmbeddedFile("b.txt", "Hello"u8.ToArray());

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            Assert.Equal(
                new[] { "a.txt", "b.txt" },
                GetEmbeddedFilesNameTree(updatedDocument).Data.Where((_, i) => i % 2 == 0).Select(x => ((IDataToken<string>)x).Data));
        }

        [Fact]
        public void CanAddEmbeddedFileToExistingDocument()
        {
            // The same steps as examples/AddEmbeddedFileWithIncrementalUpdate.cs.
            var result = "{\"total\": 119.00}"u8.ToArray();
            using var document = PdfDocument.Open(IntegrationHelpers.GetDocumentPath("data.pdf"));

            var builder = new PdfIncrementalUpdateBuilder(document);
            builder.AddEmbeddedFile("result.json", result, new EmbeddedFileOptions
            {
                MimeType = "application/json",
                Description = "The result of the analysis"
            });

            using var updatedDocument = PdfDocument.Open(builder.Build(), ParsingOptions.LenientParsingOff);
            Assert.True(updatedDocument.Advanced.TryGetEmbeddedFiles(out var files));

            var file = Assert.Single(files);
            Assert.Equal("result.json", file.Name);
            Assert.Equal(result, file.Bytes.ToArray());
            Assert.Equal(document.GetPage(1).Text, updatedDocument.GetPage(1).Text);
        }

        private static byte[] CreatePdfADocumentWithInvoice(PdfAStandard standard)
        {
            var builder = new PdfDocumentBuilder
            {
                ArchiveStandard = standard
            };

            builder.AddPage(PageSize.A4);
            builder.AddEmbeddedFile("invoice.xml", "<Invoice/>"u8.ToArray(), new EmbeddedFileOptions
            {
                MimeType = "text/xml",
                Relationship = AssociatedFileRelationship.Alternative
            });

            return builder.Build();
        }

        /// <summary>
        /// Writes a metadata stream with the PDF/A identification, as elements or attributes of its rdf:Description.
        /// </summary>
        private static string Metadata(string identification, string attributes = "", string documentType = "")
        {
            var xmp = Xmp(identification, attributes, documentType);
            return $"<< /Type /Metadata /Subtype /XML /Length {xmp.Length} >>\nstream\n{xmp}\nendstream";
        }

        /// <summary>
        /// An XMP packet with the PDF/A identification, as elements or attributes of its rdf:Description.
        /// </summary>
        private static string Xmp(string identification, string attributes = "", string documentType = "")
        {
            return "<?xpacket begin=\"\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>" + documentType
                + "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">"
                + $"<rdf:Description rdf:about=\"\" xmlns:pdfaid=\"http://www.aiim.org/pdfa/ns/id/\"{attributes}>{identification}</rdf:Description>"
                + "</rdf:RDF></x:xmpmeta><?xpacket end=\"w\"?>";
        }

        private static string HexOf(string ascii)
        {
            return string.Concat(Encoding.ASCII.GetBytes(ascii).Select(x => x.ToString("X2")));
        }

        /// <summary>
        /// Writes the objects, numbered from 1, as a document with a cross-reference table.
        /// </summary>
        private static byte[] CreateDocument(params string[] objects)
        {
            var builder = new StringBuilder("%PDF-1.7\n");
            var offsets = new List<int>();
            for (var i = 0; i < objects.Length; i++)
            {
                offsets.Add(builder.Length);
                builder.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n");
            }

            var xref = builder.Length;
            builder.Append($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
            foreach (var offset in offsets)
            {
                builder.Append($"{offset:D10} 00000 n \n");
            }

            builder.Append($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
            return Encoding.ASCII.GetBytes(builder.ToString());
        }

        private static ArrayToken GetEmbeddedFilesNameTree(PdfDocument document)
        {
            var scanner = document.Structure.TokenScanner;
            return document.Structure.Catalog.CatalogDictionary
                .Get<DictionaryToken>(NameToken.Names, scanner)
                .Get<DictionaryToken>(NameToken.EmbeddedFiles, scanner)
                .Get<ArrayToken>(NameToken.Names, scanner);
        }
    }
}
