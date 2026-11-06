namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;
    using Core;
    using CrossReference;
    using Graphics.Operations;
    using Tokens;
    using Xmp;

    /// <summary>
    /// Writes changes to an existing document as an incremental update. The bytes of the original document stay unchanged
    /// and are followed by the new and replaced objects, a new cross-reference section and a new trailer.
    /// </summary>
    public sealed class PdfIncrementalUpdateBuilder
    {
        private readonly PdfDocument document;
        private readonly Dictionary<IndirectReference, IToken> objects = new Dictionary<IndirectReference, IToken>();
        private readonly List<(StringToken Name, StreamToken Stream, string? Description, AssociatedFileRelationship Relationship)> embeddedFiles = new();
        private readonly List<StringToken> removedEmbeddedFiles = new();
        private long nextObjectNumber;

        /// <summary>
        /// Create a new <see cref="PdfIncrementalUpdateBuilder"/> for the document.
        /// </summary>
        /// <param name="document">The document to update. It must stay open until the update is built.</param>
        /// <exception cref="NotSupportedException">The document is encrypted or its cross-reference was found by a brute-force search.</exception>
        public PdfIncrementalUpdateBuilder(PdfDocument document)
        {
            this.document = document ?? throw new ArgumentNullException(nameof(document));

            if (document.IsEncrypted)
            {
                throw new NotSupportedException("Incremental updates of encrypted documents are not supported.");
            }

            var structure = document.Structure;
            if (structure.CrossReferenceTable.IsBruteForced)
            {
                throw new NotSupportedException(
                    "Incremental updates are not supported for documents whose cross-reference was found by a brute-force search.");
            }

            var previousSize = structure.CrossReferenceTable.Parts[0].Dictionary.TryGet(NameToken.Size, out NumericToken size)
                ? size.Long
                : 0;
            var highestObjectNumber = structure.CrossReferenceTable.ObjectOffsets.Keys.Max(x => (long?)x.ObjectNumber) ?? 0;

            // As in PDFBox's COSWriter, new objects are numbered after both the size in the trailer and the highest
            // object number, because in some files the size is too small.
            nextObjectNumber = Math.Max(Math.Max(previousSize, structure.Trailer.Size), highestObjectNumber + 1);
        }

        /// <summary>
        /// Adds a new object to the document.
        /// </summary>
        /// <param name="token">The content of the object.</param>
        /// <returns>The reference to the new object.</returns>
        public IndirectReferenceToken AddObject(IToken token)
        {
            if (token is null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            var reference = new IndirectReference(nextObjectNumber++, 0);
            objects[reference] = token;

            return new IndirectReferenceToken(reference);
        }

        /// <summary>
        /// Replaces an object of the document. The new content keeps the object number and generation of the reference.
        /// </summary>
        /// <param name="reference">The reference to the object to replace.</param>
        /// <param name="token">The new content of the object.</param>
        /// <exception cref="ArgumentException">Neither the document nor this update contains the object.</exception>
        public void ReplaceObject(IndirectReference reference, IToken token)
        {
            if (token is null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            if (!objects.ContainsKey(reference) && !document.Structure.CrossReferenceTable.ObjectOffsets.ContainsKey(reference))
            {
                throw new ArgumentException($"The document does not contain the object {reference}.", nameof(reference));
            }

            objects[reference] = token;
        }

        /// <summary>
        /// Adds a file to the document as an embedded file, which readers show as an attachment of the document.
        /// Of the PDF/A standards embedded files can only be added to PDF/A-3 documents, for the others <see cref="Build()"/> throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="name">The name of the file, which no other embedded file of the document may have.</param>
        /// <param name="bytes">The bytes of the file.</param>
        /// <exception cref="ArgumentException">The name is empty or already used.</exception>
        public void AddEmbeddedFile(string name, ReadOnlyMemory<byte> bytes)
        {
            AddEmbeddedFile(name, bytes, new EmbeddedFileOptions());
        }

        /// <summary>
        /// Adds a file to the document as an embedded file, which readers show as an attachment of the document.
        /// Of the PDF/A standards embedded files can only be added to PDF/A-3 documents, for the others <see cref="Build()"/> throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="name">The name of the file, which no other embedded file of the document may have.</param>
        /// <param name="bytes">The bytes of the file.</param>
        /// <param name="options">The MIME type and other information about the file.</param>
        /// <exception cref="ArgumentException">The name is empty or already used, or the MIME type is not a type and a subtype.</exception>
        public void AddEmbeddedFile(string name, ReadOnlyMemory<byte> bytes, EmbeddedFileOptions options)
        {
            EmbeddedFileFactory.Validate(name, options);

            if (embeddedFiles.Any(x => x.Name.Data == name))
            {
                throw new ArgumentException($"An embedded file named '{name}' was already added.", nameof(name));
            }

            if (DocumentHasEmbeddedFile(name))
            {
                throw new ArgumentException($"The document already has an embedded file named '{name}'.", nameof(name));
            }

            // The objects of the file are numbered when the update is built, so that a file removed again leaves no gap.
            embeddedFiles.Add((new StringToken(name), EmbeddedFileFactory.CreateStream(bytes, options), options.Description, options.Relationship));
        }

        /// <summary>
        /// Removes an embedded file from the document. Unless another object refers to its file specification, the update also
        /// replaces the file specification with null and removes it from the associated files of the catalog. The bytes of the
        /// file stay in the original document, which an incremental update leaves unchanged.
        /// A file can be replaced by removing it and adding the new file with the same name.
        /// Of the PDF/A standards embedded files can only be removed from PDF/A-3 documents, for the others <see cref="Build()"/> throws a <see cref="NotSupportedException"/>.
        /// </summary>
        /// <param name="name">The name of the file.</param>
        /// <returns><see langword="true"/> if the document or this update had an embedded file with the name, otherwise <see langword="false"/>.</returns>
        public bool RemoveEmbeddedFile(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            // A file that this update added is not written at all.
            var index = embeddedFiles.FindIndex(x => x.Name.Data == name);
            if (index >= 0)
            {
                embeddedFiles.RemoveAt(index);
                return true;
            }

            if (!DocumentHasEmbeddedFile(name))
            {
                return false;
            }

            removedEmbeddedFiles.Add(new StringToken(name));
            return true;
        }

        /// <summary>
        /// Builds the updated document.
        /// </summary>
        /// <returns>The bytes of the original document followed by the update.</returns>
        /// <exception cref="NotSupportedException">The update adds or removes embedded files of a PDF/A document other than PDF/A-3.</exception>
        public byte[] Build()
        {
            using var output = new MemoryStream();
            Build(output);
            return output.ToArray();
        }

        /// <summary>
        /// Writes the updated document, the bytes of the original document followed by the update, to the stream.
        /// </summary>
        /// <param name="output">The stream to write to.</param>
        /// <exception cref="NotSupportedException">The update adds or removes embedded files of a PDF/A document other than PDF/A-3.</exception>
        public void Build(Stream output)
        {
            if (output is null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            // The embedded files are written to a copy, so that the builder can build again.
            var updatedObjects = embeddedFiles.Count > 0 || removedEmbeddedFiles.Count > 0 ? WithEmbeddedFiles() : objects;

            // The size is one greater than the highest object number (ISO 32000-1, 7.5.5), which may belong to an embedded file.
            var size = Math.Max(nextObjectNumber, updatedObjects.Keys.Max(x => (long?)x.ObjectNumber + 1) ?? 0);

            var original = document.InputBytes;
            original.Seek(0);

            var buffer = new byte[81920];
            int read;
            while ((read = original.Read(buffer)) > 0)
            {
                output.Write(buffer, 0, read);
            }

            using var update = new MemoryStream();

            // The original may not end with a line break after %%EOF.
            update.WriteNewLine();

            var offsets = new Dictionary<IndirectReference, long>();
            foreach (var entry in updatedObjects.OrderBy(x => x.Key.ObjectNumber))
            {
                var offset = original.Length + update.Position;
                offsets[entry.Key] = offset;
                TokenWriter.Instance.WriteToken(new ObjectToken(XrefLocation.File(offset), entry.Key, entry.Value), update);
            }

            // As in PDFBox's COSWriter, the update uses a cross-reference stream if the previous section is one.
            var previous = document.Structure.CrossReferenceTable.Parts[0];
            if (previous.Type == CrossReferenceType.Stream)
            {
                WriteCrossReferenceStream(update, original.Length, offsets, previous, size);
            }
            else
            {
                var trailer = CreateTrailer(previous, size);
                TokenWriter.Instance.WriteCrossReferenceTable(offsets, new DictionaryToken(trailer), update, original.Length);
            }

            update.WriteTo(output);
        }

        private Dictionary<IndirectReference, IToken> WithEmbeddedFiles()
        {
            var result = new Dictionary<IndirectReference, IToken>(objects);

            var root = document.Structure.Trailer.Root;
            if (Resolve(new IndirectReferenceToken(root), result) is not DictionaryToken catalog)
            {
                throw new InvalidOperationException("The catalog of the document is not a dictionary.");
            }

            // Only PDF/A-3 is supported, also for removing files. PDF/A-1 does not allow embedded files and PDF/A-2 only allows
            // PDF/A documents, which cannot be checked here. PdfPig does not support PDF/A-4, and removing the last file of a
            // PDF/A-4f document would remove the EmbeddedFiles key that it requires.
            var part = GetPdfAPart(catalog, result);
            if (part is not null && part != 3)
            {
                throw new NotSupportedException($"Embedded files are only supported for PDF/A-3, not for PDF/A-{part}.");
            }

            var isPdfA3 = part == 3;

            // The objects of the added files are numbered after the other new objects.
            var added = new List<(StringToken Name, IndirectReferenceToken FileSpecification)>();
            var number = nextObjectNumber;
            foreach (var file in embeddedFiles)
            {
                var stream = new IndirectReferenceToken(new IndirectReference(number++, 0));
                var reference = new IndirectReferenceToken(new IndirectReference(number++, 0));
                var fileSpecification = EmbeddedFileFactory.CreateFileSpecification(file.Name, stream, file.Description);

                // PDF/A-3 requires the relationship of each embedded file and its association with the document (ISO 19005-3, 6.8).
                if (isPdfA3)
                {
                    fileSpecification[NameToken.AfRelationship] = EmbeddedFileFactory.GetRelationship(file.Relationship);
                }

                result[stream.Data] = file.Stream;
                result[reference.Data] = new DictionaryToken(fileSpecification);
                added.Add((file.Name, reference));
            }

            var updatedCatalog = WithNameTree(catalog, result, added, out var removedValues);
            if (!ReferenceEquals(updatedCatalog, catalog))
            {
                result[root] = updatedCatalog;
            }

            var deleted = DeleteUnreferencedFileSpecifications(removedValues, result);

            var associated = isPdfA3 ? added.Select(x => (IToken)x.FileSpecification).ToList() : new List<IToken>();
            updatedCatalog = WithAssociatedFiles(updatedCatalog, result, deleted, associated);
            if (!ReferenceEquals(updatedCatalog, catalog))
            {
                result[root] = updatedCatalog;
            }

            return result;
        }

        /// <summary>
        /// Writes the embedded files name tree without the removed files and with the added ones, and returns the catalog, which
        /// only changes if the names dictionary is direct.
        /// </summary>
        private DictionaryToken WithNameTree(
            DictionaryToken catalog,
            Dictionary<IndirectReference, IToken> result,
            List<(StringToken Name, IndirectReferenceToken FileSpecification)> added,
            out List<IToken> removedValues)
        {
            catalog.Data.TryGetValue(NameToken.Names.Data, out var namesToken);
            var names = Resolve(namesToken, result) as DictionaryToken;

            IToken? treeToken = null;
            names?.Data.TryGetValue(NameToken.EmbeddedFiles.Data, out treeToken);

            var entries = new List<(IToken Key, IToken Value)>();
            if (treeToken is not null)
            {
                CollectEntries(treeToken, result, entries);
            }

            var removedEntries = entries.Where(x => IsRemoved(x.Key)).ToList();
            removedValues = removedEntries.Select(x => x.Value).ToList();
            entries.RemoveAll(x => IsRemoved(x.Key));

            // Any encoding of the keys may be used as long as it is self-consistent (ISO 32000-1, 7.9.6), so a replaced file
            // keeps its key, and a new key is written as UTF-16 if all other keys are. Keys are strings, so a name the document
            // has as key is not kept.
            var keptKeys = added
                .Select(x => removedEntries.Select(y => y.Key).FirstOrDefault(y => y is StringToken or HexToken && EmbeddedFileFactory.HasName(y, x.Name)))
                .ToList();
            var otherKeys = entries.Select(x => x.Key).Concat(keptKeys.OfType<IToken>()).ToList();
            var isUtf16 = otherKeys.Count > 0 && otherKeys.All(EmbeddedFileFactory.IsUtf16);
            for (var i = 0; i < added.Count; i++)
            {
                var key = keptKeys[i] ?? (isUtf16 ? EmbeddedFileFactory.ToUtf16(added[i].Name) : added[i].Name);
                entries.Add((key, added[i].FileSpecification));
            }

            DictionaryToken updatedNames;
            if (entries.Count == 0)
            {
                // A tree without keys is removed.
                updatedNames = (names ?? DictionaryToken.Empty).Without(NameToken.EmbeddedFiles);
            }
            else
            {
                // The keys are sorted byte by byte, and OrderBy keeps the order of equal keys of the document.
                var sortedEntries = entries.OrderBy(x => x.Key, Comparer<IToken>.Create(EmbeddedFileFactory.CompareKeys));
                var updatedTree = new DictionaryToken(new Dictionary<NameToken, IToken>
                {
                    { NameToken.Names, new ArrayToken(sortedEntries.SelectMany(x => new[] { x.Key, x.Value }).ToArray()) }
                });

                // An indirect object is replaced where it is, so that other references to it stay valid.
                if (treeToken is IndirectReferenceToken treeReference && Resolve(treeToken, result) is DictionaryToken)
                {
                    result[treeReference.Data] = updatedTree;
                    return catalog;
                }

                updatedNames = (names ?? DictionaryToken.Empty).With(NameToken.EmbeddedFiles, updatedTree);
            }

            if (namesToken is IndirectReferenceToken namesReference && names is not null)
            {
                result[namesReference.Data] = updatedNames;
                return catalog;
            }

            return updatedNames.Data.Count == 0 ? catalog.Without(NameToken.Names) : catalog.With(NameToken.Names, updatedNames);
        }

        /// <summary>
        /// Replaces the file specifications of removed files that the document no longer refers to with null, and returns them.
        /// </summary>
        private HashSet<IndirectReference> DeleteUnreferencedFileSpecifications(List<IToken> removedValues, Dictionary<IndirectReference, IToken> result)
        {
            // The original part of the document keeps the file specification of a removed file. This builder writes no free
            // cross-reference entries (ISO 32000-1, 7.5.6), so the file specification becomes null instead, as PDF/A-3
            // requires each one to be associated (ISO 19005-3, 6.8).
            var candidates = new HashSet<IndirectReference>(removedValues
                .OfType<IndirectReferenceToken>()
                .Where(x => IsFileSpecification(Resolve(x, result)))
                .Select(x => x.Data));

            if (candidates.Count == 0)
            {
                return candidates;
            }

            // A candidate the document still refers to stays. The walk starts at the trailer and skips the associated files of
            // the catalog, from which the update removes the candidates.
            var root = document.Structure.Trailer.Root;
            var visited = new HashSet<IndirectReference>();
            var pending = new Stack<IToken>();
            pending.Push(new IndirectReferenceToken(root));
            if (document.Structure.Trailer.Info is not null)
            {
                pending.Push(document.Structure.Trailer.Info);
            }

            while (pending.Count > 0 && candidates.Count > 0)
            {
                var token = pending.Pop();
                if (token is IndirectReferenceToken reference)
                {
                    if (candidates.Remove(reference.Data) || !visited.Add(reference.Data))
                    {
                        continue;
                    }

                    token = GetObject(reference.Data, result);
                    if (reference.Data.Equals(root) && token is DictionaryToken catalog)
                    {
                        token = catalog.Without(NameToken.Af);
                    }
                }

                switch (token)
                {
                    case ArrayToken array:
                        foreach (var item in array.Data)
                        {
                            pending.Push(item);
                        }

                        break;
                    case DictionaryToken dictionary:
                        foreach (var value in dictionary.Data.Values)
                        {
                            pending.Push(value);
                        }

                        break;
                    case StreamToken stream:
                        pending.Push(stream.StreamDictionary);
                        break;
                }
            }

            foreach (var reference in candidates)
            {
                result[reference] = NullToken.Instance;
            }

            return candidates;
        }

        /// <summary>
        /// Writes the associated files of the catalog without the deleted and with the added file specifications, and returns
        /// the catalog, which only changes if the associated files are direct or are removed.
        /// </summary>
        private DictionaryToken WithAssociatedFiles(
            DictionaryToken catalog,
            Dictionary<IndirectReference, IToken> result,
            HashSet<IndirectReference> deleted,
            List<IToken> added)
        {
            if (deleted.Count == 0 && added.Count == 0)
            {
                return catalog;
            }

            catalog.Data.TryGetValue(NameToken.Af.Data, out var associatedFilesToken);
            var associatedFiles = Resolve(associatedFilesToken, result) as ArrayToken;
            var kept = ((IReadOnlyList<IToken>?)associatedFiles?.Data ?? Array.Empty<IToken>())
                .Where(x => !(x is IndirectReferenceToken reference && deleted.Contains(reference.Data)))
                .ToList();

            if (added.Count == 0 && kept.Count == (associatedFiles?.Length ?? 0))
            {
                return catalog;
            }

            var updatedAssociatedFiles = kept.Concat(added).ToArray();
            if (updatedAssociatedFiles.Length == 0)
            {
                // The associated files are an array of one or more file specifications (ISO 32000-2, Table 29).
                return catalog.Without(NameToken.Af);
            }

            if (associatedFilesToken is IndirectReferenceToken associatedFilesReference && associatedFiles is not null)
            {
                result[associatedFilesReference.Data] = new ArrayToken(updatedAssociatedFiles);
                return catalog;
            }

            return catalog.With(NameToken.Af, new ArrayToken(updatedAssociatedFiles));
        }

        private static bool IsFileSpecification(IToken? token)
        {
            return token is DictionaryToken dictionary
                && ((dictionary.TryGet(NameToken.Type, out NameToken type) && type.Equals(NameToken.Filespec)) || dictionary.ContainsKey(NameToken.Ef));
        }

        /// <summary>
        /// Gets the part of PDF/A, such as 3 for PDF/A-3, that the XMP metadata of the catalog states.
        /// </summary>
        private int? GetPdfAPart(DictionaryToken catalog, IReadOnlyDictionary<IndirectReference, IToken> state)
        {
            if (!catalog.Data.TryGetValue(NameToken.Metadata.Data, out var metadataToken) || Resolve(metadataToken, state) is not StreamToken metadata)
            {
                return null;
            }

            XDocument xml;
            try
            {
                ReadOnlyMemory<byte> xmp = metadata.Decode(document.Structure.FilterProvider, document.Structure.TokenScanner);
                using var stream = xmp.AsReadOnlyMemoryStream();
                using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null });
                xml = XDocument.Load(reader);
            }
            catch (Exception)
            {
                // Metadata that cannot be decoded or is not XML states no part. The filters throw various exceptions for damaged data.
                return null;
            }

            // The part is an element or an attribute of an rdf:Description (ISO 16684-1, 7.5).
            XNamespace pdfaid = XmpWriter.PdfAIdentificationExtensionNamespace;
            var part = xml.Descendants(pdfaid + "part").Select(x => x.Value)
                .Concat(xml.Descendants().Attributes(pdfaid + "part").Select(x => x.Value))
                .FirstOrDefault();

            return int.TryParse(part?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : null;
        }

        private bool IsRemoved(IToken key)
        {
            return removedEmbeddedFiles.Any(x => EmbeddedFileFactory.HasName(key, x));
        }

        /// <summary>
        /// Whether the document, with the changes of this update so far, has an embedded file with the name.
        /// </summary>
        private bool DocumentHasEmbeddedFile(string name)
        {
            var key = new StringToken(name);
            return removedEmbeddedFiles.All(x => x.Data != name)
                && GetEmbeddedFilesOfDocument(objects).Any(x => EmbeddedFileFactory.HasName(x.Key, key));
        }

        /// <summary>
        /// Gets the entries of the embedded files name tree of the document, as it is in the state.
        /// </summary>
        private List<(IToken Key, IToken Value)> GetEmbeddedFilesOfDocument(IReadOnlyDictionary<IndirectReference, IToken> state)
        {
            var entries = new List<(IToken Key, IToken Value)>();
            if (Resolve(new IndirectReferenceToken(document.Structure.Trailer.Root), state) is DictionaryToken catalog
                && catalog.Data.TryGetValue(NameToken.Names.Data, out var namesToken)
                && Resolve(namesToken, state) is DictionaryToken names
                && names.Data.TryGetValue(NameToken.EmbeddedFiles.Data, out var treeToken))
            {
                CollectEntries(treeToken, state, entries);
            }

            return entries;
        }

        private void CollectEntries(IToken tree, IReadOnlyDictionary<IndirectReference, IToken> state, List<(IToken Key, IToken Value)> entries)
        {
            // As in NameTreeParser the nodes are walked in document order, and each indirect node once, so that a cycle of Kids ends.
            var visited = new HashSet<IndirectReference>();
            var pending = new Stack<IToken>();
            pending.Push(tree);

            while (pending.Count > 0)
            {
                var token = pending.Pop();
                if ((token is IndirectReferenceToken reference && !visited.Add(reference.Data)) || Resolve(token, state) is not DictionaryToken node)
                {
                    continue;
                }

                if (node.Data.TryGetValue(NameToken.Names.Data, out var namesToken) && Resolve(namesToken, state) is ArrayToken names)
                {
                    for (var i = 0; i + 1 < names.Length; i += 2)
                    {
                        // As NameTreeParser, which reads a name as a key too.
                        if (names[i] is IDataToken<string>)
                        {
                            entries.Add((names[i], names[i + 1]));
                        }
                    }
                }

                if (node.Data.TryGetValue(NameToken.Kids.Data, out var kidsToken) && Resolve(kidsToken, state) is ArrayToken kids)
                {
                    for (var i = kids.Length - 1; i >= 0; i--)
                    {
                        pending.Push(kids[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the object a token refers to as it is in this update, or as it is in the document if the update does not change it.
        /// </summary>
        private IToken? Resolve(IToken? token, IReadOnlyDictionary<IndirectReference, IToken> state)
        {
            // A reference may refer to another reference, the set ends a cycle of them.
            var visited = new HashSet<IndirectReference>();
            while (token is IndirectReferenceToken reference && visited.Add(reference.Data))
            {
                token = GetObject(reference.Data, state);
            }

            return token is IndirectReferenceToken ? null : token;
        }

        private IToken? GetObject(IndirectReference reference, IReadOnlyDictionary<IndirectReference, IToken> state)
        {
            if (state.TryGetValue(reference, out var token))
            {
                return token;
            }

            // The catalog as the document read it, which Advanced.TryGetEmbeddedFiles reads too. After a search of a damaged
            // file for objects, the scanner may return an older version of it.
            if (reference.Equals(document.Structure.Trailer.Root))
            {
                return document.Structure.Catalog.CatalogDictionary;
            }

            try
            {
                return document.Structure.TokenScanner.Get(reference)?.Data;
            }
            catch (PdfDocumentFormatException)
            {
                // An object that cannot be read is treated as missing, which is the null object (ISO 32000-1, 7.3.10).
                return null;
            }
        }

        private void WriteCrossReferenceStream(
            Stream update,
            long streamOffset,
            IReadOnlyDictionary<IndirectReference, long> offsets,
            CrossReferenceTablePart previous,
            long objectNumber)
        {
            var reference = new IndirectReference(objectNumber, 0);
            var offset = streamOffset + update.Position;

            // As in PDFBox, the stream lists the head of the free list and itself.
            var entries = new List<(long Number, byte Type, long Field2, long Field3)>
            {
                (0, 0, 0, 65535),
                (reference.ObjectNumber, 1, offset, 0)
            };
            entries.AddRange(offsets.Select(x => (x.Key.ObjectNumber, (byte)1, x.Value, (long)x.Key.Generation)));
            entries.Sort((x, y) => x.Number.CompareTo(y.Number));

            var widths = new[] { 1, GetByteCount(entries.Max(x => x.Field2)), GetByteCount(entries.Max(x => x.Field3)) };

            var data = new byte[entries.Count * widths.Sum()];
            var position = 0;
            foreach (var entry in entries)
            {
                data[position++] = entry.Type;
                position = WriteBigEndian(entry.Field2, widths[1], data, position);
                position = WriteBigEndian(entry.Field3, widths[2], data, position);
            }

            var index = new List<IToken>();
            for (var start = 0; start < entries.Count;)
            {
                var end = start + 1;
                while (end < entries.Count && entries[end].Number == entries[end - 1].Number + 1)
                {
                    end++;
                }

                index.Add(new NumericToken(entries[start].Number));
                index.Add(new NumericToken(end - start));
                start = end;
            }

            var compressed = DataCompressor.CompressBytes(data);

            var dictionary = CreateTrailer(previous, reference.ObjectNumber + 1);
            dictionary[NameToken.Type] = NameToken.Xref;
            dictionary[NameToken.Index] = new ArrayToken(index);
            dictionary[NameToken.W] = new ArrayToken(widths.Select(x => new NumericToken(x)).ToArray());
            dictionary[NameToken.Filter] = NameToken.FlateDecode;

            var stream = new StreamToken(new DictionaryToken(dictionary), compressed);
            TokenWriter.Instance.WriteToken(new ObjectToken(XrefLocation.File(offset), reference, stream), update);

            update.WriteText("startxref"u8);
            update.WriteNewLine();
            update.WriteText(offset.ToString(CultureInfo.InvariantCulture));
            update.WriteNewLine();
            update.WriteText("%%EOF"u8);
        }

        private Dictionary<NameToken, IToken> CreateTrailer(CrossReferenceTablePart previous, long size)
        {
            var trailer = new Dictionary<NameToken, IToken>();
            foreach (var entry in previous.Dictionary.Data)
            {
                // A cross-reference stream dictionary also describes the stream, so as in PDFBox's PDFXRefStream
                // only its trailer entries are taken.
                var isTrailerEntry = previous.Type == CrossReferenceType.Table
                    ? entry.Key != NameToken.Prev.Data && entry.Key != NameToken.XrefStm.Data
                    : entry.Key == NameToken.Root.Data || entry.Key == NameToken.Info.Data;

                if (isTrailerEntry)
                {
                    trailer[NameToken.Create(entry.Key)] = entry.Value;
                }
            }

            trailer[NameToken.Size] = new NumericToken(size);
            trailer[NameToken.Prev] = new NumericToken(previous.Offset);

            // As in PDFBox's COSWriter, the first identifier stays and the second changes with each update.
            // A document without identifiers gets the same new one twice.
            var identifier = new HexToken(Guid.NewGuid().ToString("N").AsSpan());
            IToken permanentIdentifier = identifier;
            if (previous.Dictionary.TryGet(NameToken.Id, out ArrayToken previousIdentifier) && previousIdentifier.Length == 2)
            {
                permanentIdentifier = previousIdentifier.Data[0];
            }

            trailer[NameToken.Id] = new ArrayToken(new[] { permanentIdentifier, identifier });

            return trailer;
        }

        private static int GetByteCount(long value)
        {
            var count = 1;
            while ((value >>= 8) > 0)
            {
                count++;
            }

            return count;
        }

        private static int WriteBigEndian(long value, int width, byte[] data, int position)
        {
            for (var i = width - 1; i >= 0; i--)
            {
                data[position++] = (byte)(value >> (8 * i));
            }

            return position;
        }
    }
}
