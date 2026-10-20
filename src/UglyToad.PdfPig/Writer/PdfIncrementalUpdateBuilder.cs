namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Core;
    using CrossReference;
    using Graphics.Operations;
    using Tokens;

    /// <summary>
    /// Writes changes to an existing document as an incremental update. The bytes of the original document stay unchanged
    /// and are followed by the new and replaced objects, a new cross-reference section and a new trailer.
    /// </summary>
    public sealed class PdfIncrementalUpdateBuilder
    {
        private readonly PdfDocument document;
        private readonly Dictionary<IndirectReference, IToken> objects = new Dictionary<IndirectReference, IToken>();
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
        /// Builds the updated document.
        /// </summary>
        /// <returns>The bytes of the original document followed by the update.</returns>
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
        public void Build(Stream output)
        {
            if (output is null)
            {
                throw new ArgumentNullException(nameof(output));
            }

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
            foreach (var entry in objects.OrderBy(x => x.Key.ObjectNumber))
            {
                var offset = original.Length + update.Position;
                offsets[entry.Key] = offset;
                TokenWriter.Instance.WriteToken(new ObjectToken(XrefLocation.File(offset), entry.Key, entry.Value), update);
            }

            // As in PDFBox's COSWriter, the update uses a cross-reference stream if the previous section is one.
            var previous = document.Structure.CrossReferenceTable.Parts[0];
            if (previous.Type == CrossReferenceType.Stream)
            {
                WriteCrossReferenceStream(update, original.Length, offsets, previous);
            }
            else
            {
                var trailer = CreateTrailer(previous, nextObjectNumber);
                TokenWriter.Instance.WriteCrossReferenceTable(offsets, new DictionaryToken(trailer), update, original.Length);
            }

            update.WriteTo(output);
        }

        private void WriteCrossReferenceStream(
            Stream update,
            long streamOffset,
            IReadOnlyDictionary<IndirectReference, long> offsets,
            CrossReferenceTablePart previous)
        {
            var reference = new IndirectReference(nextObjectNumber, 0);
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
