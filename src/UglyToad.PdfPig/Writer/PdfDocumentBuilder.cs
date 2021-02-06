
namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Content;
    using Core;
    using Fonts;
    using PdfPig.Fonts.TrueType;
    using Graphics.Operations;
    using PdfPig.Fonts.Standard14Fonts;
    using PdfPig.Fonts.TrueType.Parser;
    using Tokens;
    using System.Runtime.CompilerServices;

    using Util.JetBrains.Annotations;

    /// <summary>
    /// Provides methods to construct new PDF documents.
    /// </summary>
    public class PdfDocumentBuilder : IDisposable
    {
        private readonly IPdfStreamWriter context;
        private readonly Dictionary<int, PdfPageBuilder> pages = new Dictionary<int, PdfPageBuilder>();
        private readonly Dictionary<Guid, FontStored> fonts = new Dictionary<Guid, FontStored>();
        private readonly Dictionary<Guid, ImageStored> images = new Dictionary<Guid, ImageStored>();

        /// <summary>
        /// The standard of PDF/A compliance of the generated document. Defaults to <see cref="PdfAStandard.None"/>.
        /// </summary>
        public PdfAStandard ArchiveStandard { get; set; } = PdfAStandard.None;

        /// <summary>
        /// Whether to include the document information dictionary in the produced document.
        /// </summary>
        public bool IncludeDocumentInformation { get; set; } = true;

        /// <summary>
        /// The values of the fields to include in the document information dictionary.
        /// </summary>
        public DocumentInformationBuilder DocumentInformation { get; } = new DocumentInformationBuilder();

        /// <summary>
        /// The current page builders in the document and the corresponding 1 indexed page numbers. Use <see cref="AddPage(double,double)"/>
        /// or <see cref="AddPage(PageSize,bool)"/> to add a new page.
        /// </summary>
        public IReadOnlyDictionary<int, PdfPageBuilder> Pages => pages;

        /// <summary>
        /// The fonts currently available in the document builder added via <see cref="AddTrueTypeFont"/> or <see cref="AddStandard14Font"/>. Keyed by id for internal purposes.
        /// </summary>
        internal IReadOnlyDictionary<Guid, IWritingFont> Fonts => fonts.ToDictionary(x => x.Key, x => x.Value.FontProgram);

        /// <summary>
        /// 
        /// </summary>
        public PdfDocumentBuilder()
        {
            context = new PdfStreamWriter(new MemoryStream(), true);
            context.InitializePdf(1.7m);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="disposeStream"></param>
        /// <param name="type"></param>
        public PdfDocumentBuilder(Stream stream, bool disposeStream=false, PdfWriter type=PdfWriter.Default)
        {
            switch (type)
            {
                case PdfWriter.ObjectInMemoryDedup:
                    context = new PdfHashStreamWriter(stream, false);
                    break;
                default:
                    context = new PdfStreamWriter(stream, false);
                    break;
            }
            context.InitializePdf(1.7m);
        }

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

                var font = TrueTypeFontParser.Parse(new TrueTypeDataBytes(new ByteArrayInputBytes(fontFileBytes)));

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
                var font = TrueTypeFontParser.Parse(new TrueTypeDataBytes(new ByteArrayInputBytes(fontFileBytes)));
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
            if (ArchiveStandard != PdfAStandard.None)
            {
                throw new NotSupportedException($"PDF/A {ArchiveStandard} requires the font to be embedded in the file, only {nameof(AddTrueTypeFont)} is supported.");
            }

            var id = Guid.NewGuid();
            var name = NameToken.Create($"F{fonts.Count}");
            var added = new AddedFont(id, name);
            fonts[id] = new FontStored(added, new Standard14WritingFont(Standard14.GetAdobeFontMetrics(type)));

            return added;
        }

        internal IndirectReferenceToken AddImage(DictionaryToken dictionary, byte[] bytes)
        {
            var streamToken = new StreamToken(dictionary, bytes);
            return context.WriteToken(streamToken);
        }

        /// <summary>
        /// Add a new page with the specified size, this page will be included in the output when <see cref="Build"/> is called.
        /// </summary>
        /// <param name="width">The width of the page in points.</param>
        /// <param name="height">The height of the page in points.</param>
        /// <returns>A builder for editing the new page.</returns>
        public PdfPageBuilder AddPage(double width, double height)
        {
            if (width < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width), $"Width cannot be negative, got: {width}.");
            }

            if (height < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height), $"Height cannot be negative, got: {height}.");
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

            builder.PageSize = new PdfRectangle(0, 0, width, height);
            pages[builder.PageNumber] = builder;

            return builder;
        }

        /// <summary>
        /// Add a new page with the specified size, this page will be included in the output when <see cref="Build"/> is called.
        /// </summary>
        /// <param name="size">The size of the page to add.</param>
        /// <param name="isPortrait">Whether the page is in portait or landscape orientation.</param>
        /// <returns>A builder for editing the new page.</returns>
        public PdfPageBuilder AddPage(PageSize size, bool isPortrait = true)
        {
            if (size == PageSize.Custom)
            {
                throw new ArgumentException($"Cannot use ${nameof(PageSize.Custom)} for ${nameof(AddPage)} using the ${nameof(PageSize)} enum, call the overload with width and height instead.",
                    nameof(size));
            }

            if (!size.TryGetPdfRectangle(out var rectangle))
            {
                throw new ArgumentException($"No rectangle found for Page Size {size}.");
            }

            if (!isPortrait)
            {
                return AddPage(rectangle.Height, rectangle.Width);
            }

            return AddPage(rectangle.Width, rectangle.Height);
        }
        private readonly ConditionalWeakTable<PdfDocument, Dictionary<IndirectReference, IndirectReferenceToken>> existingCopies = 
            new ConditionalWeakTable<PdfDocument, Dictionary<IndirectReference, IndirectReferenceToken>>();

        /// <summary>
        /// Add a new page with the specified size, this page will be included in the output when <see cref="Build"/> is called.
        /// </summary>
        /// <param name="document">Source document.</param>
        /// <param name="pageNumber">Page to copy.</param>
        /// <returns>A builder for editing the page.</returns>
        public PdfPageBuilder AddPage(PdfDocument document, int pageNumber)
        {
            if (!existingCopies.TryGetValue(document, out var refs))
            {
                refs = new Dictionary<IndirectReference, IndirectReferenceToken>();
                existingCopies.Add(document, refs);
            }

            int i = 1;
            foreach (var (pageDict, parents) in WriterUtil.WalkTree(document.Structure.Catalog.PageTree))
            {
                if (i == pageNumber)
                {
                    // copy content streams
                    var streams = new List<PdfPageBuilder.CopiedContentStream>();
                    if (pageDict.ContainsKey(NameToken.Contents))
                    {
                        var token = pageDict.Data[NameToken.Contents];
                        if (token is ArrayToken array)
                        {
                            foreach (var item in array.Data)
                            {
                                if (item is IndirectReferenceToken ir)
                                {
                                    streams.Add(new PdfPageBuilder.CopiedContentStream(
                                        WriterUtil.CopyToken(context, ir, document.Structure.TokenScanner, refs) as IndirectReferenceToken));
                                }

                            }
                        }
                        else if (token is IndirectReferenceToken ir)
                        {
                            streams.Add(new PdfPageBuilder.CopiedContentStream(
                                WriterUtil.CopyToken(context, ir, document.Structure.TokenScanner, refs) as IndirectReferenceToken));
                        }
                    }

                    // manually copy page dict / resources as we need to modify some
                    var copiedPageDict = new Dictionary<NameToken, IToken>();
                    Dictionary<NameToken, IToken> resources = new Dictionary<NameToken, IToken>();

                    // just put all parent resources into new page
                    foreach (var dict in parents)
                    {
                        if (dict.TryGet(NameToken.Resources, out var token))
                        {
                            CopyResourceDict(token, resources);
                        }
                    }


                    foreach (var kvp in pageDict.Data)
                    {
                        if (kvp.Key == NameToken.Contents || kvp.Key == NameToken.Parent || kvp.Key == NameToken.Type)
                        {
                            continue;
                        }

                        if (kvp.Key == NameToken.Resources)
                        {
                            CopyResourceDict(kvp.Value, resources);
                            continue;
                        }

                        copiedPageDict[NameToken.Create(kvp.Key)] =
                            WriterUtil.CopyToken(context, kvp.Value, document.Structure.TokenScanner, refs);
                    }

                    var builder = new PdfPageBuilder(pages.Count + 1, this, streams, resources, copiedPageDict);
                    pages[builder.PageNumber] = builder;
                    return builder;
                }

                i++;
            }

            throw new KeyNotFoundException($"Page {pageNumber} was not found in the source document.");

            void CopyResourceDict(IToken token, Dictionary<NameToken, IToken> destinationDict)
            {
                DictionaryToken dict = GetRemoteDict(token);
                if (dict == null)
                {
                    return;
                }
                foreach (var item in dict.Data)
                {

                    if (!destinationDict.ContainsKey(NameToken.Create(item.Key)))
                    {
                        if (item.Value is IndirectReferenceToken ir)
                        {
                            destinationDict[NameToken.Create(item.Key)] = WriterUtil.CopyToken(context, document.Structure.TokenScanner.Get(ir.Data).Data, document.Structure.TokenScanner, refs);
                        }
                        else
                        {
                            destinationDict[NameToken.Create(item.Key)] = WriterUtil.CopyToken(context, item.Value, document.Structure.TokenScanner, refs);
                        }

                        continue;
                    }


                    var subDict = GetRemoteDict(item.Value);
                    var destSubDict = destinationDict[NameToken.Create(item.Key)] as DictionaryToken;
                    if (destSubDict == null || subDict == null)
                    {
                        // not a dict.. just overwrite with more important one? should maybe check arrays?
                        destinationDict[NameToken.Create(item.Key)] = WriterUtil.CopyToken(context, item.Value, document.Structure.TokenScanner, refs);
                        continue;
                    }
                    foreach (var subItem in subDict.Data)
                    {
                        // last copied most important important
                        destinationDict[NameToken.Create(subItem.Key)] = WriterUtil.CopyToken(context, subItem.Value,
                            document.Structure.TokenScanner, refs);
                    }
                }
            }

            DictionaryToken GetRemoteDict(IToken token)
            {
                DictionaryToken dict = null;
                if (token is IndirectReferenceToken ir)
                {
                    dict = document.Structure.TokenScanner.Get(ir.Data).Data as DictionaryToken;
                }
                else if (token is DictionaryToken dt)
                {
                    dict = dt;
                }
                return dict;
            }
        }


        private void CompleteDocument()
        {
            var fontsWritten = new Dictionary<Guid, IndirectReferenceToken>();

            foreach (var font in fonts)
            {
                var fontObj = font.Value.FontProgram.WriteFont(context, font.Value.FontKey.Name);
                fontsWritten.Add(font.Key, fontObj);
            }

            var procSet = new List<NameToken>
            {
                NameToken.Create("PDF"),
                NameToken.Text,
                NameToken.ImageB,
                NameToken.ImageC,
                NameToken.ImageI
            };
            
            var resources = new Dictionary<NameToken, IToken>
            {
                { NameToken.ProcSet, new ArrayToken(procSet) }
            };

            if (fontsWritten.Count > 0)
            {
                var fontsDictionary = new DictionaryToken(fontsWritten.Select(x =>
                        (fonts[x.Key].FontKey.Name, (IToken)x.Value))
                    .ToDictionary(x => x.Item1, x => x.Item2));

                var fontsDictionaryRef = context.WriteToken(fontsDictionary);

                resources.Add(NameToken.Font, fontsDictionaryRef);
            }

            var parentIndirect = context.ReserveObjectNumber();

            var pageReferences = new List<IndirectReferenceToken>();
            foreach (var page in pages)
            {
                var pageDictionary = page.Value.pageProperties;
                pageDictionary[NameToken.Type] = NameToken.Page;
                pageDictionary[NameToken.Parent] = parentIndirect;
                if (!pageDictionary.ContainsKey(NameToken.MediaBox))
                {
                    pageDictionary[NameToken.MediaBox] = RectangleToArray(page.Value.PageSize);
                }

                pageDictionary[NameToken.Resources] = new DictionaryToken(page.Value.Resources);

                if (page.Value.ContentStreams.Count == 1)
                {
                    pageDictionary[NameToken.Contents] = page.Value.ContentStreams[0].Write(context);
                }
                else
                {
                    var streams = new List<IToken>();
                    foreach (var stream in page.Value.ContentStreams)
                    {
                        streams.Add(stream.Write(context));
                    }

                    pageDictionary[NameToken.Contents] = new ArrayToken(streams);
                }


                var pageRef = context.WriteToken( new DictionaryToken(pageDictionary));

                pageReferences.Add(pageRef);
            }

            var pagesDictionaryData = new Dictionary<NameToken, IToken>
            {
                {NameToken.Type, NameToken.Pages},
                {NameToken.Kids, new ArrayToken(pageReferences)},
                {NameToken.Resources, new DictionaryToken(resources)},
                {NameToken.Count, new NumericToken(pageReferences.Count)}
            };
            
            var pagesDictionary = new DictionaryToken(pagesDictionaryData);

            var pagesRef = context.WriteToken(pagesDictionary, parentIndirect);

            var catalogDictionary = new Dictionary<NameToken, IToken>
            {
                {NameToken.Type, NameToken.Catalog},
                {NameToken.Pages, pagesRef}
            };

            if (ArchiveStandard != PdfAStandard.None)
            {
                Func<IToken, IndirectReferenceToken> writerFunc = x => context.WriteToken(x);

                PdfABaselineRuleBuilder.Obey(catalogDictionary, writerFunc, DocumentInformation, ArchiveStandard);

                switch (ArchiveStandard)
                {
                    case PdfAStandard.A1A:
                        PdfA1ARuleBuilder.Obey(catalogDictionary);
                        break;
                    case PdfAStandard.A2B:
                        break;
                    case PdfAStandard.A2A:
                        PdfA1ARuleBuilder.Obey(catalogDictionary);
                        break;
                }
            }

            var catalog = new DictionaryToken(catalogDictionary);

            var catalogRef = context.WriteToken(catalog);

            var informationReference = default(IndirectReferenceToken);
            if (IncludeDocumentInformation)
            {
                var informationDictionary = DocumentInformation.ToDictionary();
                if (informationDictionary.Count > 0)
                {
                    var dictionary = new DictionaryToken(informationDictionary);
                    informationReference = context.WriteToken(dictionary);
                }
            }

            context.CompletePdf(catalogRef, informationReference);
        }

        /// <summary>
        /// Builds a PDF document from the current content of this builder and its pages.
        /// </summary>
        /// <returns>The bytes of the resulting PDF document.</returns>
        public byte[] Build() 
        {
            CompleteDocument();

            if (context.Stream is MemoryStream ms)
            {
                return ms.ToArray();
            }

            if (!context.Stream.CanSeek)
            {
                throw new InvalidOperationException("PdfDocument.Build() called with non-seekable stream.");
            }

            using (var temp = new MemoryStream())
            {
                context.Stream.Seek(0, SeekOrigin.Begin);
                context.Stream.CopyTo(temp);
                return temp.ToArray();
            }
        }

        private static ArrayToken RectangleToArray(PdfRectangle rectangle)
        {
            return new ArrayToken(new[]
            {
                new NumericToken((decimal)rectangle.BottomLeft.X),
                new NumericToken((decimal)rectangle.BottomLeft.Y),
                new NumericToken((decimal)rectangle.TopRight.X),
                new NumericToken((decimal)rectangle.TopRight.Y)
            });
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

        internal class ImageStored
        {
            public Guid Id { get; }

            public DictionaryToken StreamDictionary { get; }

            public byte[] StreamData { get; }

            public int ObjectNumber { get; }

            public ImageStored(DictionaryToken streamDictionary, byte[] streamData, int objectNumber)
            {
                Id = Guid.NewGuid();
                StreamDictionary = streamDictionary;
                StreamData = streamData;
                ObjectNumber = objectNumber;
            }
        }

        /// <summary>
        /// A key representing a font available to use on the current document builder. Create by adding a font to a document using either
        /// <see cref="AddStandard14Font"/> or <see cref="AddTrueTypeFont"/>.
        /// </summary>
        public class AddedFont
        {
            /// <summary>
            /// The Id uniquely identifying this font on the builder.
            /// </summary>
            internal Guid Id { get; }

            /// <summary>
            /// The name of this font.
            /// </summary>
            public NameToken Name { get; }

            /// <summary>
            /// Create a new <see cref="AddedFont"/>.
            /// </summary>
            internal AddedFont(Guid id, NameToken name)
            {
                Id = id;
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }
        }

        /// <summary>
        /// Sets the values of the <see cref="DocumentInformation"/> dictionary for the document being created.
        /// Control inclusion of the document information dictionary on the output with <see cref="IncludeDocumentInformation"/>.
        /// </summary>
        public class DocumentInformationBuilder
        {
            /// <summary>
            /// <see cref="DocumentInformation.Title"/>.
            /// </summary>
            public string Title { get; set; }

            /// <summary>
            /// <see cref="DocumentInformation.Author"/>.
            /// </summary>
            public string Author { get; set; }

            /// <summary>
            /// <see cref="DocumentInformation.Subject"/>.
            /// </summary>
            public string Subject { get; set; }

            /// <summary>
            /// <see cref="DocumentInformation.Keywords"/>.
            /// </summary>
            public string Keywords { get; set; }

            /// <summary>
            /// <see cref="DocumentInformation.Creator"/>.
            /// </summary>
            public string Creator { get; set; }

            /// <summary>
            /// <see cref="DocumentInformation.Producer"/>.
            /// </summary>
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

        /// <summary>
        /// Disposes the pdf document.
        /// </summary>
        public void Dispose()
        {
            CompleteDocument();
            context.Dispose();
        }
    }
}
