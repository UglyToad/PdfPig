using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Filters;
using UglyToad.PdfPig.Graphics.Operations.TextShowing;
using UglyToad.PdfPig.Graphics.Operations;
using UglyToad.PdfPig.Graphics;
using UglyToad.PdfPig.Logging;
using UglyToad.PdfPig.Parser;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Writer
{
    /// <summary>
    /// Derived class of <see cref="TokenWriter"/> that does not write <see cref="ShowText"/> or <see cref="ShowTextsWithPositioning"/> operations in streams
    /// </summary>
    internal class NoTextTokenWriter : TokenWriter
    {
        /// <summary>
        /// Write stream without <see cref="ShowText"/> or <see cref="ShowTextsWithPositioning"/> operations
        /// </summary>
        /// <param name="streamToken"></param>
        /// <param name="outputStream"></param>
        protected override void WriteStream(StreamToken streamToken, Stream outputStream)
        {
            if (!TryGetStreamWithoutText(streamToken, out var outputStreamToken))
            {
                outputStreamToken = streamToken;
            }
            WriteDictionary(outputStreamToken.StreamDictionary, outputStream);
            WriteLineBreak(outputStream);
            outputStream.Write(StreamStart, 0, StreamStart.Length);
            WriteLineBreak(outputStream);
            outputStream.Write(outputStreamToken.Data.ToArray(), 0, outputStreamToken.Data.Count);
            WriteLineBreak(outputStream);
            outputStream.Write(StreamEnd, 0, StreamEnd.Length);
        }

        /// <summary>
        /// Try get a stream without <see cref="ShowText"/> or <see cref="ShowTextsWithPositioning"/> operations.
        /// </summary>
        /// <param name="streamToken"></param>
        /// <param name="outputStreamToken"></param>
        /// <returns>true if any text operation found (and we have a valid <paramref name="outputStreamToken"/> without the text operations),
        /// false if no text operation found (in which case <paramref name="outputStreamToken"/> is null)</returns>
        private bool TryGetStreamWithoutText(StreamToken streamToken, out StreamToken outputStreamToken)
        {
            var filterProvider = new FilterProviderWithLookup(DefaultFilterProvider.Instance);
            IReadOnlyList<byte> bytes;
            try
            {
                bytes = streamToken.Decode(filterProvider);
            }
            catch
            {
                outputStreamToken = null;
                return false;
            }

            var pageContentParser = new PageContentParser(new ReflectionGraphicsStateOperationFactory());
            IReadOnlyList<IGraphicsStateOperation> operations;
            try
            {
                operations = pageContentParser.Parse(1, new ByteArrayInputBytes(bytes), new NoOpLog());
            }
            catch (Exception)
            {
                outputStreamToken = null;
                return false;
            }

            using (var outputStreamT = new MemoryStream())
            {
                var haveText = false;
                foreach (var op in operations)
                {
                    if (op.Operator == ShowText.Symbol || op.Operator == ShowTextsWithPositioning.Symbol)
                    {
                        haveText = true;
                        continue;
                    }
                    op.Write(outputStreamT);
                }
                if (!haveText)
                {
                    outputStreamToken = null;
                    return false;
                }
                outputStreamT.Seek(0, SeekOrigin.Begin);
                outputStreamToken = DataCompresser.CompressToStream(outputStreamT.ToArray());
                return true;
            }
        }
    }
}