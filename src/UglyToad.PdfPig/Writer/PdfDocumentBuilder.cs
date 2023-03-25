
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
    using PdfPig.Fonts.Standard14Fonts;
    using PdfPig.Fonts.TrueType.Parser;
    using PdfPig.Outline;
    using PdfPig.Outline.Destinations;
    using System.Runtime.CompilerServices;
    using Tokenization.Scanner;
    using Tokens;

    using Util.JetBrains.Annotations;

    /// <summary>
    /// Provides methods to construct new PDF documents.
    /// </summary>
    public class PdfDocumentBuilder : IDisposable
    {
        private readonly IPdfStreamWriter context;
        private readonly Dictionary<int, PdfPageBuilder> pages = new Dictionary<int, PdfPageBuilder>();
        private readonly Dictionary<Guid, FontStored> fonts = new Dictionary<Guid, FontStored>();
        private bool completed = false;
        private int fontId = 0;

        private readonly static ArrayToken DefaultProcSet = new ArrayToken(new List<NameToken>
        {
            NameToken.Create("PDF"),
            NameToken.Text,
            NameToken.ImageB,
            NameToken.ImageC,
            NameToken.ImageI
        });

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
        public DocumentInformationBuilder DocumentInformation { get; set; } = new DocumentInformationBuilder();

        /// <summary>
        /// The bookmark nodes to include in the document outline dictionary.
        /// </summary>
        public Bookmarks Bookmarks { get; set; }

        /// <summary>
        /// The current page builders in the document and the corresponding 1 indexed page numbers. Use <see cref="AddPage(double,double)"/>
        /// or <see cref="AddPage(PageSize,bool)"/> to add a new page.
        /// </summary>
        public IReadOnlyDictionary<int, PdfPageBuilder> Pages => pages;

        /// <summary>
        /// The fonts currently available in the document builder added via <see cref="AddTrueTypeFont"/> or <see cref="AddStandard14Font"/>. Keyed by id for internal purposes.
        /// </summary>
        internal IReadOnlyDictionary<Guid, FontStored> Fonts => fonts;

        /// <summary>
        /// Creates a document builder keeping resources in memory.
        /// </summary>
        public PdfDocumentBuilder()
        {
            context = new PdfStreamWriter(new MemoryStream(), true);
            context.InitializePdf(1.7m);
        }

        /// <summary>
        /// Creates a document builder keeping resources in memory.
        /// </summary>
        /// <param name="version">Pdf version to use in header.</param>
        public PdfDocumentBuilder(decimal version)
        {
            context = new PdfStreamWriter(new MemoryStream(), true);
            context.InitializePdf(version);
        }

        /// <summary>
        /// Creates a document builder using the supplied stream.
        /// </summary>
        /// <param name="stream">Steam to write pdf to.</param>
        /// <param name="disposeStream">If stream should be disposed when builder is.</param>
        /// <param name="type">Type of pdf stream writer to use</param>
        /// <param name="version">Pdf version to use in header.</param>
        /// <param name="tokenWriter">Token writer to use</param>
        public PdfDocumentBuilder(Stream stream, bool disposeStream = false, PdfWriterType type = PdfWriterType.Default, decimal version = 1.7m, ITokenWriter tokenWriter = null)
        {
            switch (type)
            {
                case PdfWriterType.ObjectInMemoryDedup:
                    context = new PdfDedupStreamWriter(stream, disposeStream, tokenWriter);
                    break;
                default:
                    context = new PdfStreamWriter(stream, disposeStream, tokenWriter);
                    break;
            }
            context.InitializePdf(version);
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
                var added = new AddedFont(id, context.ReserveObjectNumber());
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
            var name = NameToken.Create($"F{fontId++}");
            var added = new AddedFont(id, context.ReserveObjectNumber());
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


        internal IToken CopyToken(IPdfTokenScanner source, IToken token)
        {
            if (!existingCopies.TryGetValue(source, out var refs))
            {
                refs = new Dictionary<IndirectReference, IndirectReferenceToken>();
                existingCopies.Add(source, refs);
            }

            return WriterUtil.CopyToken(context, token, source, refs);
        }

        internal class PageInfo
        {
            public DictionaryToken Page { get; set; }
            public IReadOnlyList<DictionaryToken> Parents { get; set; }
        }
        private readonly ConditionalWeakTable<IPdfTokenScanner, Dictionary<IndirectReference, IndirectReferenceToken>> existingCopies =
            new ConditionalWeakTable<IPdfTokenScanner, Dictionary<IndirectReference, IndirectReferenceToken>>();
        private readonly ConditionalWeakTable<PdfDocument, Dictionary<int, PageInfo>> existingTrees =
            new ConditionalWeakTable<PdfDocument, Dictionary<int, PageInfo>>();
        /// <summary>
        /// Add a new page with the specified size, this page will be included in the output when <see cref="Build"/> is called.
        /// </summary>
        /// <param name="document">Source document.</param>
        /// <param name="pageNumber">Page to copy.</param>
        /// <returns>A builder for editing the page.</returns>
        public PdfPageBuilder AddPage(PdfDocument document, int pageNumber)
        {
            if (!existingCopies.TryGetValue(document.Structure.TokenScanner, out var refs))
            {
                refs = new Dictionary<IndirectReference, IndirectReferenceToken>();
                existingCopies.Add(document.Structure.TokenScanner, refs);
            }

            if (!existingTrees.TryGetValue(document, out var pagesInfos))
            {
                pagesInfos = new Dictionary<int, PageInfo>();
                int i = 1;
                foreach (var (pageDict, parents) in WriterUtil.WalkTree(document.Structure.Catalog.PageTree))
                {
                    pagesInfos[i] = new PageInfo
                    {
                        Page = pageDict,
                        Parents = parents
                    };
                    i++;
                }

                existingTrees.Add(document, pagesInfos);
            }

            if (!pagesInfos.TryGetValue(pageNumber, out PageInfo pageInfo))
            {
                throw new KeyNotFoundException($"Page {pageNumber} was not found in the source document.");
            }

            // copy content streams
            var streams = new List<PdfPageBuilder.CopiedContentStream>();
            if (pageInfo.Page.TryGet(NameToken.Contents, out IToken contentsToken))
            {
                // Adobe Acrobat errors if content streams ref'd by multiple pages, turn off
                // dedup if on to avoid issues
                var prev = context.AttemptDeduplication;
                context.AttemptDeduplication = false;
                if (contentsToken is ArrayToken array)
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
                else if (contentsToken is IndirectReferenceToken ir)
                {
                    streams.Add(new PdfPageBuilder.CopiedContentStream(
                        WriterUtil.CopyToken(context, ir, document.Structure.TokenScanner, refs) as IndirectReferenceToken));
                }
                context.AttemptDeduplication = prev;
            }

            // manually copy page dict / resources as we need to modify some
            var copiedPageDict = new Dictionary<NameToken, IToken>();
            Dictionary<NameToken, IToken> resources = new Dictionary<NameToken, IToken>();

            // just put all parent resources into new page
            foreach (var dict in pageInfo.Parents)
            {
                if (dict.TryGet(NameToken.Resources, out var resourceToken))
                {
                    CopyResourceDict(resourceToken, resources);
                }
                if (dict.TryGet(NameToken.MediaBox, out var mb))
                {
                    copiedPageDict[NameToken.MediaBox] = WriterUtil.CopyToken(context, mb, document.Structure.TokenScanner, refs);
                }
                if (dict.TryGet(NameToken.CropBox, out var cb))
                {
                    copiedPageDict[NameToken.CropBox] = WriterUtil.CopyToken(context, cb, document.Structure.TokenScanner, refs);
                }
                if (dict.TryGet(NameToken.Rotate, out var rt))
                {
                    copiedPageDict[NameToken.Rotate] = WriterUtil.CopyToken(context, rt, document.Structure.TokenScanner, refs);
                }
            }


            foreach (var kvp in pageInfo.Page.Data)
            {
                if (kvp.Key == NameToken.Contents || kvp.Key == NameToken.Parent || kvp.Key == NameToken.Type)
                {
                    // don't copy these as they'll be handled during page tree writing
                    continue;
                }

                if (kvp.Key == NameToken.Resources)
                {
                    // merge parent resources into child
                    CopyResourceDict(kvp.Value, resources);
                    continue;
                }

                if (kvp.Key == NameToken.Annots)
                {
                    var val = kvp.Value;
                    if (kvp.Value is IndirectReferenceToken ir)
                    {
                        ObjectToken tk = document.Structure.TokenScanner.Get(ir.Data);
                        if (tk == null)
                        {
                            // malformed
                            continue;
                        }
                        val = tk.Data;
                    }

                    if (!(val is ArrayToken arr))
                    {
                        // should be array... ignore and remove bad dict
                        continue;
                    }

                    // -> ignore links to resolve issues with refencing non-existing pages
                    // at some point should add support for copying the links if the
                    // pages are copied as well but for now just fix corruption
                    var toAdd = new List<IToken>();
                    foreach (var annot in arr.Data)
                    {
                        DictionaryToken tk = GetRemoteDict(annot);
                        if (tk == null)
                        {
                            // malformed
                            continue;
                        }
                        if (tk.TryGet(NameToken.Subtype, out var st) && st is NameToken nm && nm == NameToken.Link)
                        {
                            // link -> ignore
                            continue;
                        }
                        toAdd.Add(WriterUtil.CopyToken(context, tk, document.Structure.TokenScanner, refs));
                    }
                    // copy rest
                    copiedPageDict[NameToken.Annots] = new ArrayToken(toAdd);
                    continue;
                }

                copiedPageDict[NameToken.Create(kvp.Key)] =
                    WriterUtil.CopyToken(context, kvp.Value, document.Structure.TokenScanner, refs);
            }

            copiedPageDict[NameToken.Resources] = new DictionaryToken(resources);

            var builder = new PdfPageBuilder(pages.Count + 1, this, streams, copiedPageDict);
            pages[builder.PageNumber] = builder;
            return builder;

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
                            // convert indirect to direct as PdfPageBuilder needs to modify resource entries
                            var obj = document.Structure.TokenScanner.Get(ir.Data);
                            if (obj.Data is StreamToken)
                            {
                                // rare case, have seen /SubType as stream token, can't make direct
                                destinationDict[NameToken.Create(item.Key)] = WriterUtil.CopyToken(context, item.Value, document.Structure.TokenScanner, refs);
                            }
                            else
                            {
                                destinationDict[NameToken.Create(item.Key)] = WriterUtil.CopyToken(context, obj.Data, document.Structure.TokenScanner, refs);
                            }
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
                        if (item.Value is IndirectReferenceToken ir)
                        {
                            // convert indirect to direct as PdfPageBuilder needs to modify resource entries
                            destinationDict[NameToken.Create(item.Key)] = WriterUtil.CopyToken(context, document.Structure.TokenScanner.Get(ir.Data).Data, document.Structure.TokenScanner, refs);
                        }
                        else
                        {
                            destinationDict[NameToken.Create(item.Key)] = WriterUtil.CopyToken(context, item.Value, document.Structure.TokenScanner, refs);
                        }
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
            // write fonts to reserved object numbers
            foreach (var font in fonts)
            {
                font.Value.FontProgram.WriteFont(context, font.Value.FontKey.Reference);
            }

            const int desiredLeafSize = 25; // allow customization at some point?
            var numLeafs = (int)Math.Ceiling(Decimal.Divide(Pages.Count, desiredLeafSize));

            var leafRefs = new List<IndirectReferenceToken>();
            var leafChildren = new List<List<IndirectReferenceToken>>();
            var leafs = new List<Dictionary<NameToken, IToken>>();
            for (var i = 0; i < numLeafs; i++)
            {
                leafs.Add(new Dictionary<NameToken, IToken>()
                {
                    {NameToken.Type, NameToken.Pages},
                });
                leafChildren.Add(new List<IndirectReferenceToken>());
                leafRefs.Add(context.ReserveObjectNumber());
            }

            int leafNum = 0;
            var pageReferences = new Dictionary<int, IndirectReferenceToken>(pages.Count);

            foreach (var page in pages)
            {
                var pageDictionary = page.Value.pageDictionary;
                pageDictionary[NameToken.Type] = NameToken.Page;
                pageDictionary[NameToken.Parent] = leafRefs[leafNum];
                pageDictionary[NameToken.ProcSet] = DefaultProcSet;
                if (!pageDictionary.ContainsKey(NameToken.MediaBox))
                {
                    pageDictionary[NameToken.MediaBox] = RectangleToArray(page.Value.PageSize);
                }

                if (page.Value.rotation.HasValue)
                {
                    pageDictionary[NameToken.Rotate] = new NumericToken(page.Value.rotation.Value);
                }

                // Adobe Acrobat errors if content streams ref'd by multiple pages, turn off
                // dedup if on to avoid issues
                var prev = context.AttemptDeduplication;
                context.AttemptDeduplication = false;

                var toWrite = page.Value.contentStreams.Where(x => x.HasContent).ToList();
                if (toWrite.Count == 0)
                {
                    pageDictionary[NameToken.Contents] = new PdfPageBuilder.DefaultContentStream().Write(context);
                }
                else if (toWrite.Count == 1)
                {
                    // write single
                    pageDictionary[NameToken.Contents] = toWrite[0].Write(context);
                }
                else
                {
                    // write array
                    var streams = new List<IToken>();
                    foreach (var stream in toWrite)
                    {
                        streams.Add(stream.Write(context));
                    }
                    pageDictionary[NameToken.Contents] = new ArrayToken(streams);
                }
                context.AttemptDeduplication = prev;

                pageReferences[page.Key] = context.WriteToken(new DictionaryToken(pageDictionary));
                leafChildren[leafNum].Add(pageReferences[page.Key]);

                if (leafChildren[leafNum].Count >= desiredLeafSize)
                {
                    leafNum += 1;
                }
            }

            var dummyName = NameToken.Create("ObjIdToUse");
            for (var i = 0; i < leafs.Count; i++)
            {
                leafs[i][NameToken.Kids] = new ArrayToken(leafChildren[i]);
                leafs[i][NameToken.Count] = new NumericToken(leafChildren[i].Count);
                leafs[i][dummyName] = leafRefs[i];
            }

            var catalogDictionary = new Dictionary<NameToken, IToken>
            {
                {NameToken.Type, NameToken.Catalog},
            };
            if (leafs.Count == 1)
            {
                var leaf = leafs[0];
                var id = leaf[dummyName] as IndirectReferenceToken;
                leaf.Remove(dummyName);
                catalogDictionary[NameToken.Pages] = context.WriteToken(new DictionaryToken(leaf), id);
            }
            else
            {
                var rootPageInfo = CreatePageTree(leafs, null);
                catalogDictionary[NameToken.Pages] = rootPageInfo.Ref;
            }

            if (Bookmarks != null && Bookmarks.Roots.Count > 0)
            {
                var bookmarks = CreateBookmarkTree(Bookmarks.Roots, pageReferences, null);
                var outline = new Dictionary<NameToken, IToken>
                {
                    {NameToken.Type, NameToken.Outlines},
                    {NameToken.Count, new NumericToken(Bookmarks.Roots.Count)},
                    {NameToken.First, bookmarks[0]},
                    {NameToken.Last, bookmarks[bookmarks.Length - 1]},
                };

                catalogDictionary[NameToken.Outlines] = context.WriteToken(new DictionaryToken(outline));
            }

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
                    case PdfAStandard.A3B:
                        break;
                    case PdfAStandard.A3A:
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

            completed = true;

            (int Count, IndirectReferenceToken Ref) CreatePageTree(List<Dictionary<NameToken, IToken>> pagesNodes, IndirectReferenceToken parent)
            {
                // TODO shorten page tree when there is a single or small number of pages left in a branch
                var count = 0;
                var thisObj = context.ReserveObjectNumber();

                var children = new List<IndirectReferenceToken>();
                if (pagesNodes.Count > desiredLeafSize)
                {
                    var currentTreeDepth = (int)Math.Ceiling(Math.Log(pagesNodes.Count, desiredLeafSize));
                    var perBranch = (int)Math.Ceiling(Math.Pow(desiredLeafSize, currentTreeDepth - 1));
                    var branches = (int)Math.Ceiling(decimal.Divide(pagesNodes.Count, (decimal)perBranch));
                    for (var i = 0; i < branches; i++)
                    {
                        var part = pagesNodes.Skip(i * perBranch).Take(perBranch).ToList();
                        var result = CreatePageTree(part, thisObj);
                        count += result.Count;
                        children.Add(result.Ref);
                    }
                }
                else
                {
                    foreach (var page in pagesNodes)
                    {
                        page[NameToken.Parent] = thisObj;
                        var id = page[dummyName] as IndirectReferenceToken;
                        page.Remove(dummyName);
                        count += (page[NameToken.Count] as NumericToken).Int;
                        children.Add(context.WriteToken(new DictionaryToken(page), id));
                    }
                }

                var node = new Dictionary<NameToken, IToken>
                {
                    {NameToken.Type, NameToken.Pages},
                    {NameToken.Kids, new ArrayToken(children)},
                    {NameToken.Count, new NumericToken(count)}
                };
                if (parent != null)
                {
                    node[NameToken.Parent] = parent;
                }
                return (count, context.WriteToken(new DictionaryToken(node), thisObj));
            }
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

        private IndirectReferenceToken[] CreateBookmarkTree(IReadOnlyList<BookmarkNode> nodes, Dictionary<int, IndirectReferenceToken> pageReferences, IndirectReferenceToken parent)
        {
            var childObjectNumbers = new IndirectReferenceToken[nodes.Count];
            for (var i = 0; i < nodes.Count; i++)
            {
                childObjectNumbers[i] = context.ReserveObjectNumber();
            }

            for (var i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                var objectNumber = childObjectNumbers[i];
                var data = new Dictionary<NameToken, IToken>
                {
                    {NameToken.Title, new StringToken(node.Title)},
                    {NameToken.Count, new NumericToken(node.Children.Count)}
                };

                if (parent != null)
                {
                    data[NameToken.Parent] = parent;
                }
                if (i > 0)
                {
                    data[NameToken.Prev] = childObjectNumbers[i - 1];
                }
                if (i < childObjectNumbers.Length - 1)
                {
                    data[NameToken.Next] = childObjectNumbers[i + 1];
                }

                if (node.Children.Count > 0)
                {
                    var children = CreateBookmarkTree(node.Children, pageReferences, objectNumber);
                    data[NameToken.First] = children[0];
                    data[NameToken.Last] = children[children.Length - 1];
                }

                switch (node)
                {
                    case DocumentBookmarkNode documentBookmarkNode:
                        if (!pageReferences.TryGetValue(documentBookmarkNode.PageNumber, out var pageReference))
                        {
                            throw new KeyNotFoundException($"Page {documentBookmarkNode.PageNumber} was not found in the source document.");
                        }

                        data[NameToken.Dest] = CreateExplicitDestinationToken(documentBookmarkNode.Destination, pageReference);
                        break;

                    case UriBookmarkNode uriBookmarkNode:
                        data[NameToken.A] = new DictionaryToken(new Dictionary<NameToken, IToken>()
                        {
                            [NameToken.S] = NameToken.Uri,
                            [NameToken.Uri] = new StringToken(uriBookmarkNode.Uri),
                        });
                        break;

                    default:
                        throw new NotSupportedException($"{node.GetType().Name} is not a supported bookmark node type.");
                }

                context.WriteToken(new DictionaryToken(data), objectNumber);
            }

            return childObjectNumbers;
        }

        private static ArrayToken CreateExplicitDestinationToken(ExplicitDestination destination, IndirectReferenceToken page)
        {
            switch (destination.Type)
            {
                case ExplicitDestinationType.XyzCoordinates:
                    return new ArrayToken(new IToken[]
                    {
                        page,
                        NameToken.XYZ,
                        new NumericToken(destination.Coordinates.Left ?? 0),
                        new NumericToken(destination.Coordinates.Top ?? 0)
                    });

                case ExplicitDestinationType.FitPage:
                    return new ArrayToken(new IToken[]
                    {
                        page,
                        NameToken.Fit
                    });

                case ExplicitDestinationType.FitHorizontally:
                    return new ArrayToken(new IToken[]
                    {
                        page,
                        NameToken.FitH,
                        new NumericToken(destination.Coordinates.Top ?? 0)
                    });

                case ExplicitDestinationType.FitVertically:
                    return new ArrayToken(new IToken[]
                    {
                        page,
                        NameToken.FitV,
                        new NumericToken(destination.Coordinates.Left ?? 0)
                    });

                case ExplicitDestinationType.FitRectangle:
                    return new ArrayToken(new IToken[]
                    {
                        page,
                        NameToken.FitR,
                        new NumericToken(destination.Coordinates.Left ?? 0),
                        new NumericToken(destination.Coordinates.Top ?? 0),
                        new NumericToken(destination.Coordinates.Right ?? 0),
                        new NumericToken(destination.Coordinates.Bottom ?? 0)
                    });

                case ExplicitDestinationType.FitBoundingBox:
                    return new ArrayToken(new IToken[]
                    {
                        page,
                        NameToken.FitB,
                    });

                case ExplicitDestinationType.FitBoundingBoxHorizontally:
                    return new ArrayToken(new IToken[]
                    {
                        page,
                        NameToken.FitBH,
                        new NumericToken(destination.Coordinates.Left ?? 0)
                    });

                case ExplicitDestinationType.FitBoundingBoxVertically:
                    return new ArrayToken(new IToken[]
                    {
                        page,
                        NameToken.FitBV,
                        new NumericToken(destination.Coordinates.Left ?? 0)
                    });

                default:
                    throw new NotSupportedException($"{destination.Type} is not a supported bookmark destination type.");
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
            /// Reference to the added font.
            /// </summary>
            internal IndirectReferenceToken Reference { get; }

            /// <summary>
            /// Create a new <see cref="AddedFont"/>.
            /// </summary>
            internal AddedFont(Guid id, IndirectReferenceToken reference)
            {
                Id = id;
                Reference = reference;
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
        /// Disposes underlying stream if set to do so.
        /// </summary>
        public void Dispose()
        {
            if (!completed)
            {
                CompleteDocument();
            }

            context.Dispose();
        }
    }
}
