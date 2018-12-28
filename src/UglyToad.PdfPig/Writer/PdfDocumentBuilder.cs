namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Content;
    using Fonts;
    using Fonts.TrueType;
    using Fonts.TrueType.Parser;
    using Geometry;
    using Graphics.Operations;
    using IO;
    using Tokens;
    using Util;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Provides methods to construct new PDF documents.
    /// </summary>
    internal class PdfDocumentBuilder
    {
        private static readonly TrueTypeFontParser Parser = new TrueTypeFontParser();

        private readonly Dictionary<int, PdfPageBuilder> pages = new Dictionary<int, PdfPageBuilder>();
        private readonly Dictionary<Guid, FontStored> fonts = new Dictionary<Guid, FontStored>();

        /// <summary>
        /// Whether to include the document information dictionary in the produced document.
        /// </summary>
        public bool IncludeDocumentInformation { get; set; } = true;
        /// <summary>
        /// The values of the fields to include in the document information dictionary.
        /// </summary>
        public DocumentInformationBuilder DocumentInformation { get; } = new DocumentInformationBuilder();

        /// <summary>
        /// The current page builders in the document and the corresponding 1 indexed page numbers. Use <see cref="AddPage"/> to add a new page.
        /// </summary>
        public IReadOnlyDictionary<int, PdfPageBuilder> Pages => pages;

        /// <summary>
        /// The fonts currently available in the document builder added via <see cref="AddTrueTypeFont"/> or <see cref="AddStandard14Font"/>. Keyed by id for internal purposes.
        /// </summary>
        public IReadOnlyDictionary<Guid, IWritingFont> Fonts => fonts.ToDictionary(x => x.Key, x => x.Value.FontProgram);

        /// <summary>
        /// Determines whether the bytes of the TrueType font file provided can be used in a PDF document.
        /// </summary>
        /// <param name="fontFileBytes">The bytes of a TrueType font file.</param>
        /// <param name="reasons">Any reason messages explaining why the file can't be used, if applicable.</param>
        /// <returns><see langword="true"/> if the file can be used, <see langword="false"/> otherwise.</returns>
        public bool CanUseTrueTypeFont(IReadOnlyList<byte> fontFileBytes, out IReadOnlyList<string> reasons)
        {
            var reasonsMutable = new List<string>();
            reasons = reasonsMutable;
            try
            {
                if (fontFileBytes == null)
                {
                    reasonsMutable.Add("Provided bytes were null.");
                    return false;
                }

                if (fontFileBytes.Count == 0)
                {
                    reasonsMutable.Add("Provided bytes were empty.");
                    return false;
                }

                var font = Parser.Parse(new TrueTypeDataBytes(new ByteArrayInputBytes(fontFileBytes)));

                if (font.TableRegister.CMapTable == null)
                {
                    reasonsMutable.Add("The provided font did not contain a cmap table, used to map character codes to glyph codes.");
                    return false;
                }

                if (font.TableRegister.Os2Table == null)
                {
                    reasonsMutable.Add("The provided font did not contain an OS/2 table, used to fill in the font descriptor dictionary.");
                    return false;
                }

                if (font.TableRegister.PostScriptTable == null)
                {
                    reasonsMutable.Add("The provided font did not contain a post PostScript table, used to map character codes to glyph codes.");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                reasonsMutable.Add(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Adds a TrueType font to the builder so that pages in this document can use it.
        /// </summary>
        /// <param name="fontFileBytes">The bytes of a TrueType font.</param>
        /// <returns>An identifier which can be passed to <see cref="PdfPageBuilder.AddText"/>.</returns>
        public AddedFont AddTrueTypeFont(IReadOnlyList<byte> fontFileBytes)
        {
            try
            {
                var font = Parser.Parse(new TrueTypeDataBytes(new ByteArrayInputBytes(fontFileBytes)));
                var id = Guid.NewGuid();
                var i = fonts.Count;
                var added = new AddedFont(id, NameToken.Create($"F{i}"));
                fonts[id] = new FontStored(added, new TrueTypeWritingFont(font, fontFileBytes));

                return added;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Writing only supports TrueType fonts, please provide a valid TrueType font.", ex);
            }
        }

        /// <summary>
        /// Adds one of the Standard 14 fonts which are included by default in PDF programs so that pages in this document can use it. These Standard 14 fonts are old and possibly obsolete.
        /// </summary>
        /// <param name="type">The type of the Standard 14 font to use.</param>
        /// <returns>An identifier which can be passed to <see cref="PdfPageBuilder.AddText"/>.</returns>
        public AddedFont AddStandard14Font(Standard14Font type)
        {
            var id = Guid.NewGuid();
            var name = NameToken.Create($"F{fonts.Count}");
            var added = new AddedFont(id, name);
            fonts[id] = new FontStored(added, new Standard14WritingFont(Standard14.GetAdobeFontMetrics(type)));

            return added;
        }

        /// <summary>
        /// Add a new page with the specified size, this page will be included in the output when <see cref="Build"/> is called.
        /// </summary>
        /// <param name="size">The size of the page to add.</param>
        /// <param name="isPortrait">Whether the page is in portait or landscape orientation.</param>
        /// <returns>A builder for editing the new page.</returns>
        public PdfPageBuilder AddPage(PageSize size, bool isPortrait = true)
        {
            if (!size.TryGetPdfRectangle(out var rectangle))
            {
                throw new ArgumentException($"No rectangle found for Page Size {size}.");
            }

            if (!isPortrait)
            {
                rectangle = new PdfRectangle(0, 0, rectangle.Height, rectangle.Width);
            }

            PdfPageBuilder builder = null;
            for (var i = 0; i < pages.Count; i++)
            {
                if (!pages.ContainsKey(i + 1))
                {
                    builder = new PdfPageBuilder(i + 1, this);
                    break;
                }
            }

            if (builder == null)
            {
                builder = new PdfPageBuilder(pages.Count + 1, this);
            }

            builder.PageSize = rectangle;
            pages[builder.PageNumber] = builder;

            return builder;
        }

        /// <summary>
        /// Builds a PDF document from the current content of this builder and its pages.
        /// </summary>
        /// <returns>The bytes of the resulting PDF document.</returns>
        public byte[] Build()
        {
            var context = new BuilderContext();
            var fontsWritten = new Dictionary<Guid, ObjectToken>();
            using (var memory = new MemoryStream())
            {
                // Header
                WriteString("%PDF-1.7", memory);

                // Files with binary data should contain a 2nd comment line followed by 4 bytes with values > 127
                memory.WriteText("%");
                memory.WriteByte(169);
                memory.WriteByte(205);
                memory.WriteByte(196);
                memory.WriteByte(210);
                memory.WriteNewLine();

                // Body
                foreach (var font in fonts)
                {
                    var fontObj = font.Value.FontProgram.WriteFont(font.Value.FontKey.Name, memory, context);
                    fontsWritten.Add(font.Key, fontObj);
                }

                var resources = new Dictionary<NameToken, IToken>
                {
                    { NameToken.ProcSet, new ArrayToken(new []{ NameToken.Create("PDF"), NameToken.Create("Text") }) }
                };

                if (fontsWritten.Count > 0)
                {
                    var fontsDictionary = new DictionaryToken(fontsWritten.Select(x => (fonts[x.Key].FontKey.Name, (IToken)new IndirectReferenceToken(x.Value.Number)))
                        .ToDictionary(x => x.Item1, x => x.Item2));

                    var fontsDictionaryRef = context.WriteObject(memory, fontsDictionary);

                    resources.Add(NameToken.Font, new IndirectReferenceToken(fontsDictionaryRef.Number));
                }

                var reserved = context.ReserveNumber();
                var parentIndirect = new IndirectReferenceToken(new IndirectReference(reserved, 0));

                var pageReferences = new List<IndirectReferenceToken>();
                foreach (var page in pages)
                {
                    var pageDictionary = new Dictionary<NameToken, IToken>
                    {
                        {NameToken.Type, NameToken.Page},
                        {
                            NameToken.Resources,
                            new DictionaryToken(resources)
                        },
                        {NameToken.MediaBox, RectangleToArray(page.Value.PageSize)},
                        {NameToken.Parent, parentIndirect}
                    };

                    if (page.Value.Operations.Count > 0)
                    {
                        var contentStream = WriteContentStream(page.Value.Operations);

                        var contentStreamObj = context.WriteObject(memory, contentStream);

                        pageDictionary[NameToken.Contents] = new IndirectReferenceToken(contentStreamObj.Number);
                    }

                    var pageRef = context.WriteObject(memory, new DictionaryToken(pageDictionary));

                    pageReferences.Add(new IndirectReferenceToken(pageRef.Number));
                }

                var pagesDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.Type, NameToken.Pages },
                    { NameToken.Kids, new ArrayToken(pageReferences) },
                    { NameToken.Count, new NumericToken(1) }
                });

                var pagesRef = context.WriteObject(memory, pagesDictionary, reserved);

                var catalog = new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.Type, NameToken.Catalog },
                    { NameToken.Pages, new IndirectReferenceToken(pagesRef.Number) }
                });

                var catalogRef = context.WriteObject(memory, catalog);

                var informationReference = default(IndirectReference?);
                if (IncludeDocumentInformation)
                {
                    var informationDictionary = DocumentInformation.ToDictionary();
                    if (informationDictionary.Count > 0)
                    {
                        var dictionary = new DictionaryToken(informationDictionary);
                        informationReference = context.WriteObject(memory, dictionary).Number;
                    }
                }
                
                TokenWriter.WriteCrossReferenceTable(context.ObjectOffsets, catalogRef, memory, informationReference);

                return memory.ToArray();
            }
        }

        private static StreamToken WriteContentStream(IReadOnlyList<IGraphicsStateOperation> content)
        {
            using (var memoryStream = new MemoryStream())
            {
                foreach (var operation in content)
                {
                    operation.Write(memoryStream);
                }

                var bytes = memoryStream.ToArray();

                var streamDictionary = new Dictionary<NameToken, IToken>
                {
                    { NameToken.Length, new NumericToken(bytes.Length) }
                };

                var stream = new StreamToken(new DictionaryToken(streamDictionary), bytes);

                return stream;
            }
        }

        private static ArrayToken RectangleToArray(PdfRectangle rectangle)
        {
            return new ArrayToken(new[]
            {
                new NumericToken(rectangle.BottomLeft.X),
                new NumericToken(rectangle.BottomLeft.Y),
                new NumericToken(rectangle.TopRight.X),
                new NumericToken(rectangle.TopRight.Y)
            });
        }

        private static void WriteString(string text, MemoryStream stream, bool appendBreak = true)
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes(text);
            stream.Write(bytes, 0, bytes.Length);
            if (appendBreak)
            {
                stream.WriteNewLine();
            }
        }

        internal class FontStored
        {
            [NotNull]
            public AddedFont FontKey { get; }

            [NotNull]
            public IWritingFont FontProgram { get; }

            public FontStored(AddedFont fontKey, IWritingFont fontProgram)
            {
                FontKey = fontKey ?? throw new ArgumentNullException(nameof(fontKey));
                FontProgram = fontProgram ?? throw new ArgumentNullException(nameof(fontProgram));
            }
        }

        public class AddedFont
        {
            public Guid Id { get; }

            public NameToken Name { get; }

            internal AddedFont(Guid id, NameToken name)
            {
                Id = id;
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }
        }

        internal class DocumentInformationBuilder
        {
            public string Title { get; set; }
            public string Author { get; set; }
            public string Subject { get; set; }
            public string Keywords { get; set; }
            public string Creator { get; set; }
            public string Producer { get; set; } = "PdfPig";

            internal Dictionary<NameToken, IToken> ToDictionary()
            {
                var result = new Dictionary<NameToken, IToken>();

                if (Title != null)
                {
                    result[NameToken.Title] = new StringToken(Title);
                }

                if (Author != null)
                {
                    result[NameToken.Author] = new StringToken(Author);
                }

                if (Subject != null)
                {
                    result[NameToken.Subject] = new StringToken(Subject);
                }

                if (Keywords != null)
                {
                    result[NameToken.Keywords] = new StringToken(Keywords);
                }

                if (Creator != null)
                {
                    result[NameToken.Creator] = new StringToken(Creator);
                }

                if (Producer != null)
                {
                    result[NameToken.Producer] = new StringToken(Producer);
                }

                return result;
            }
        }
    }
}
