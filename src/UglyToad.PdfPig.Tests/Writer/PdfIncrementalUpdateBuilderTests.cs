namespace UglyToad.PdfPig.Tests.Writer
{
    using System.Text;
    using Integration;
    using PdfPig.Core;
    using PdfPig.CrossReference;
    using PdfPig.Tokens;
    using PdfPig.Writer;

    public class PdfIncrementalUpdateBuilderTests
    {
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
    }
}
