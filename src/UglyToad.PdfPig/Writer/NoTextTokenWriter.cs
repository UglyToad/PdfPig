using System.Diagnostics.CodeAnalysis;
using System.IO;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Filters;
using UglyToad.PdfPig.Graphics;
using UglyToad.PdfPig.Graphics.Operations;
using UglyToad.PdfPig.Graphics.Operations.TextShowing;
using UglyToad.PdfPig.Logging;
using UglyToad.PdfPig.Parser;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.Writer
{
    /// <summary>
    /// Derived class of <see cref="TokenWriter"/> that does not write <see cref="ShowText"/> or <see cref="ShowTextsWithPositioning"/> operations in streams
    /// </summary>
    internal sealed class NoTextTokenWriter : TokenWriter
    {
        /// <summary>
        /// Set this value prior to processing page to get the right page number in log messages
        /// </summary>
        internal int Page { get; set; }

        /// <summary>
        /// Write stream without <see cref="ShowText"/> or <see cref="ShowTextsWithPositioning"/> operations
        /// </summary>
        /// <param name="streamToken"></param>
        /// <param name="outputStream"></param>
        protected override void WriteStream(StreamToken streamToken, Stream outputStream)
        {
            StreamToken? outputStreamToken;
            if (!WritingPageContents && !IsFormStream(streamToken))
            {
                outputStreamToken = streamToken;
            }
            else if (!TryGetStreamWithoutText(streamToken, out outputStreamToken))
            {
                outputStreamToken = streamToken;
            }

            WriteDictionary(outputStreamToken.StreamDictionary, outputStream);
            WriteLineBreak(outputStream);
            outputStream.Write(StreamStart);
            WriteLineBreak(outputStream);
            outputStream.Write(outputStreamToken.Data.Span);
            WriteLineBreak(outputStream);
            outputStream.Write(StreamEnd);
        }

        private static bool IsFormStream(StreamToken streamToken)
        {
            return streamToken.StreamDictionary.TryGet(NameToken.Subtype, out NameToken subTypeValue)
                   && subTypeValue.Equals(NameToken.Form);
        }

        /// <summary>
        /// Try get a stream without <see cref="ShowText"/> or <see cref="ShowTextsWithPositioning"/> operations.
        /// </summary>
        /// <param name="streamToken"></param>
        /// <param name="outputStreamToken"></param>
        /// <returns>true if any text operation found (and we have a valid <paramref name="outputStreamToken"/> without the text operations),
        /// false if no text operation found (in which case <paramref name="outputStreamToken"/> is null)</returns>
        private bool TryGetStreamWithoutText(StreamToken streamToken, [NotNullWhen(true)] out StreamToken? outputStreamToken)
        {
            var filterProvider = new FilterProviderWithLookup(DefaultFilterProvider.Instance);
            ReadOnlyMemory<byte> bytes;
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
                operations = pageContentParser.Parse(Page, new MemoryInputBytes(bytes), new NoOpLog());
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
                    if (op.Operator == ShowText.Symbol 
                        || op.Operator == ShowTextsWithPositioning.Symbol
                        || op.Operator == MoveToNextLineShowText.Symbol)
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

                var compressedBytes = DataCompresser.CompressBytes(outputStreamT.ToArray());
                var outputStreamDictionary = new Dictionary<NameToken, IToken>()
                {
                    { NameToken.Length, new NumericToken(compressedBytes.Length) },
                    { NameToken.Filter, NameToken.FlateDecode }
                };
                foreach (var kv in streamToken.StreamDictionary.Data)
                {
                    var key = NameToken.Create(kv.Key);
                    if (!outputStreamDictionary.ContainsKey(key))
                    {
                        outputStreamDictionary[key] = kv.Value;
                    }
                };
                outputStreamToken = new StreamToken(new DictionaryToken(outputStreamDictionary), compressedBytes);
                return true;
            }
        }
    }
}