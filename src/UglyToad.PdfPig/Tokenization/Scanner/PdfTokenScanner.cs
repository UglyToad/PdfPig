namespace UglyToad.PdfPig.Tokenization.Scanner
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Core;
    using Encryption;
    using Filters;
    using Tokens;

    internal class PdfTokenScanner : IPdfTokenScanner
    {
        private static ReadOnlySpan<byte> EndstreamBytes => "endstream"u8;

        private static readonly Regex EndsWithNumberRegex = new Regex(@"(?<=^[^\s\d]+)\d+$");

        private readonly IInputBytes inputBytes;
        private readonly IObjectLocationProvider objectLocationProvider;
        private readonly ILookupFilterProvider filterProvider;
        private readonly CoreTokenScanner coreTokenScanner;
        private readonly ParsingOptions parsingOptions;

        private IEncryptionHandler encryptionHandler;
        private bool isDisposed;
        private bool isBruteForcing;

        private readonly Dictionary<IndirectReference, ObjectToken> overwrittenTokens =
            new Dictionary<IndirectReference, ObjectToken>();

        /// <summary>
        /// Stores tokens encountered between obj - endobj markers for each <see cref="MoveNext"/> call.
        /// Cleared after each operation.
        /// </summary>
        private readonly List<IToken> readTokens = [];

        // Store the previous 3 tokens and their positions so we can backtrack to find object numbers and stream dictionaries.
        private readonly long[] previousTokenPositions = new long[3];
        private readonly IToken[] previousTokens = new IToken[3];

        public IToken? CurrentToken { get; private set; }

        private IndirectReference? callingObject;

        public long CurrentPosition => coreTokenScanner.CurrentPosition;

        public long Length => coreTokenScanner.Length;

        public PdfTokenScanner(
            IInputBytes inputBytes,
            IObjectLocationProvider objectLocationProvider,
            ILookupFilterProvider filterProvider,
            IEncryptionHandler encryptionHandler,
            ParsingOptions parsingOptions)
        {
            this.inputBytes = inputBytes;
            this.objectLocationProvider = objectLocationProvider;
            this.filterProvider = filterProvider;
            this.encryptionHandler = encryptionHandler;
            this.parsingOptions = parsingOptions;
            coreTokenScanner = new CoreTokenScanner(inputBytes, true, useLenientParsing: parsingOptions.UseLenientParsing);
        }

        public void UpdateEncryptionHandler(IEncryptionHandler newHandler)
        {
            encryptionHandler = newHandler ?? throw new ArgumentNullException(nameof(newHandler));
        }

        public bool MoveNext()
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(PdfTokenScanner));
            }

            // Read until we find object-number generation obj, e.g. "69 420 obj".
            int tokensRead = 0;
            while (coreTokenScanner.MoveNext() && !Equals(coreTokenScanner.CurrentToken, OperatorToken.StartObject))
            {
                if (coreTokenScanner.CurrentToken is CommentToken)
                {
                    continue;
                }

                tokensRead++;

                previousTokens[0] = previousTokens[1];
                previousTokenPositions[0] = previousTokenPositions[1];

                previousTokens[1] = previousTokens[2];
                previousTokenPositions[1] = previousTokenPositions[2];

                previousTokens[2] = coreTokenScanner.CurrentToken;
                previousTokenPositions[2] = coreTokenScanner.CurrentTokenStart;
            }

            // We only read partial tokens.
            if (tokensRead < 2)
            {
                return false;
            }

            var startPosition = previousTokenPositions[1];
            var objectNumber = previousTokens[1] as NumericToken;
            var generation = previousTokens[2] as NumericToken;

            if (objectNumber == null || generation == null)
            {
                // Handle case where the scanner correctly reads most of an object token but includes too much of the first token
                // specifically %%EOF1 0 obj where scanning starts from 'F'.
                if (generation != null && previousTokens[1] is OperatorToken op)
                {
                    var match = EndsWithNumberRegex.Match(op.Data);

                    if (match.Success && int.TryParse(match.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number))
                    {
                        startPosition = previousTokenPositions[1] + match.Index;
                        objectNumber = new NumericToken(number);
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            var readStream = false;
            // Read all tokens between obj and endobj.
            while (coreTokenScanner.MoveNext() && !IsToken(coreTokenScanner, OperatorToken.EndObject, out _))
            {
                if (coreTokenScanner.CurrentToken is CommentToken)
                {
                    continue;
                }

                if (ReferenceEquals(coreTokenScanner.CurrentToken, OperatorToken.StartObject))
                {
                    if (readStream && readTokens[0] is StreamToken streamRead)
                    {
                        readTokens.Clear();
                        readTokens.Add(streamRead);
                        coreTokenScanner.Seek(previousTokenPositions[0]);
                        break;
                    }

                    if (readTokens.Count == 3 && readTokens[1] is NumericToken extraObjNum && readTokens[2] is NumericToken extraGenNum)
                    {
                        // An obj was encountered after reading the actual token and the object and generation number of the following token.
                        var actualReference = new IndirectReference(objectNumber.Int, generation.Int);
                        var actualToken = encryptionHandler.Decrypt(actualReference, readTokens[0]);

                        CurrentToken = new ObjectToken(startPosition, actualReference, actualToken);

                        readTokens.Clear();
                        coreTokenScanner.Seek(previousTokenPositions[0]);

                        return true;
                    }

                    return false;
                }

                if (IsToken(coreTokenScanner, OperatorToken.Xref, out _) || IsToken(coreTokenScanner, OperatorToken.StartXref, out _))
                {
                    if (readStream && readTokens[0] is StreamToken streamRead)
                    {
                        readTokens.Clear();
                        readTokens.Add(streamRead);
                        coreTokenScanner.Seek(previousTokenPositions[2]);
                        break;
                    }
                
                    if (readTokens.Count == 1)
                    {
                        // An obj was encountered after reading the actual token and the object and generation number of the following token.
                        var actualReference = new IndirectReference(objectNumber.Int, generation.Int);
                        var actualToken = encryptionHandler.Decrypt(actualReference, readTokens[0]);
                
                        CurrentToken = new ObjectToken(startPosition, actualReference, actualToken);
                        readTokens.Clear();
                        coreTokenScanner.Seek(previousTokenPositions[2]);
                
                        return true;
                    }

                    return false;
                }

                if (IsToken(coreTokenScanner, OperatorToken.StartStream, out var actualStartStreamPosition))
                {
                    var streamIdentifier = new IndirectReference(objectNumber.Long, generation.Int);

                    // Prevent an infinite loop where a stream's length references the stream or the stream's offset.
                    var getLengthFromFile = !isBruteForcing && !(callingObject.HasValue && callingObject.Value.Equals(streamIdentifier));

                    var outerCallingObject = callingObject;

                    try
                    {
                        callingObject = streamIdentifier;

                        // Read stream: special case.
                        if (TryReadStream(actualStartStreamPosition.Value, getLengthFromFile, out var stream))
                        {
                            readTokens.Clear();
                            readTokens.Add(stream);
                            readStream = true;
                        }
                    }
                    finally
                    {
                        callingObject = outerCallingObject;
                    }
                }
                else
                {
                    readTokens.Add(coreTokenScanner.CurrentToken);
                }

                previousTokens[0] = previousTokens[1];
                previousTokenPositions[0] = previousTokenPositions[1];

                previousTokens[1] = previousTokens[2];
                previousTokenPositions[1] = previousTokenPositions[2];

                previousTokens[2] = coreTokenScanner.CurrentToken;
                previousTokenPositions[2] = coreTokenScanner.CurrentTokenStart;
            }

            if (!readStream && !IsToken(coreTokenScanner, OperatorToken.EndObject, out _))
            {
                readTokens.Clear();
                return false;
            }

            var reference = new IndirectReference(objectNumber.Long, generation.Int);

            IToken token;
            if (readTokens.Count == 3 && readTokens[0] is NumericToken objNum
                                      && readTokens[1] is NumericToken genNum
                                      && ReferenceEquals(readTokens[2], OperatorToken.R))
            {
                // I have no idea if this can ever happen.
                token = new IndirectReferenceToken(new IndirectReference(objNum.Long, genNum.Int));
            }
            else
            {
                // Just take the last, should only ever be 1
                if (readTokens.Count > 1)
                {
                    Debug.WriteLine("Found more than 1 token in an object.");

                    var trimmedDuplicatedEndTokens = readTokens
                        .Where(x => x is not OperatorToken op || (op.Data != ">" && op.Data != "]")).ToList();

                    if (trimmedDuplicatedEndTokens.Count == 1)
                    {
                        token = trimmedDuplicatedEndTokens[0];
                    }
                    else if (readTokens[0] is StreamToken str
                             && readTokens.Skip(1).All(x => x is OperatorToken op && op.Equals(OperatorToken.EndStream)))
                    {
                        // If a stream token is followed by "endstream" operator tokens just skip the following duplicated tokens.
                        token = str;
                    }
                    else
                    {
                        token = readTokens[readTokens.Count - 1];
                    }
                }
                else
                {
                    token = readTokens[readTokens.Count - 1];
                }
            }

            token = encryptionHandler.Decrypt(reference, token);

            CurrentToken = new ObjectToken(startPosition, reference, token);

            objectLocationProvider.UpdateOffset(reference, startPosition);

            readTokens.Clear();
            return true;
        }

        private bool IsToken(CoreTokenScanner scanner, OperatorToken token, [NotNullWhen(true)] out long? actualTokenStart)
        {
            if (ReferenceEquals(scanner.CurrentToken, token))
            {
                actualTokenStart = scanner.CurrentTokenStart;
                return true;
            }

            if (parsingOptions.UseLenientParsing && scanner.CurrentToken is OperatorToken opToken && opToken.Data.EndsWith(token.Data))
            {
                actualTokenStart = scanner.CurrentTokenStart + opToken.Data.Length - token.Data.Length;
                return true;
            }

            actualTokenStart = null;
            return false;
        }

        private bool TryReadStream(long startStreamTokenOffset, bool getLength, [NotNullWhen(true)] out StreamToken? stream)
        {
            stream = null;

            DictionaryToken streamDictionaryToken = GetStreamDictionary();

            // Get the expected length from the stream dictionary if present.
            long? length = getLength ? GetStreamLength(streamDictionaryToken) : default;

            if (!getLength && streamDictionaryToken.TryGet(NameToken.Length, out NumericToken inlineLengthToken))
            {
                length = inlineLengthToken.Long;
            }

            // Verify again that we start with "stream"
            var hasStartStreamToken = ReadStreamTokenStart(inputBytes, startStreamTokenOffset);
            if (!hasStartStreamToken)
            {
                return false;
            }

            // From the specification: The stream operator should be followed by \r\n or \n, not just \r.
            // While the specification demands a \n we have seen files with \r only in the wild.
            // While the specification demands a \n we have seen files with `garbage` before the actual data
            do
            {
                if (!inputBytes.MoveNext())
                {
                    return false;
                }

                if ((char)inputBytes.CurrentByte == '\r')
                {
                    if (!inputBytes.MoveNext())
                    {
                        return false;
                    }

                    if ((char)inputBytes.CurrentByte != '\n')
                    {
                        inputBytes.Seek(inputBytes.CurrentOffset - 1);
                    }
                    break;
                }

            } while ((char)inputBytes.CurrentByte != '\n');

            // Store where we started reading the first byte of data.
            long startDataOffset = inputBytes.CurrentOffset;

            // Store how many bytes we have read for checking against Length.
            long read = 0;

            // We want to check if we ever read 'endobj' or 'endstream'.
            int endObjPosition = 0;
            int endStreamPosition = 0;
            int commonPartPosition = 0;

            const string endWordPart = "end";
            const string streamPart = "stream";
            const string objPart = "obj";

            if (TryReadUsingLength(inputBytes, length, startDataOffset, out var streamData))
            {
                stream = new StreamToken(streamDictionaryToken, streamData);
                return true;
            }

            long streamDataStart = inputBytes.CurrentOffset;

            PossibleStreamEndLocation? possibleEndLocation = null;


            while (inputBytes.MoveNext())
            {
                if (length.HasValue && read == length)
                {
                    // TODO: read ahead and check we're at the end...
                    // break;
                }

                // We are reading 'end' (possibly).
                if (commonPartPosition < endWordPart.Length && inputBytes.CurrentByte == endWordPart[commonPartPosition])
                {
                    commonPartPosition++;
                }
                else if (commonPartPosition == endWordPart.Length)
                {
                    // We are reading 'stream' after 'end'
                    if (inputBytes.CurrentByte == streamPart[endStreamPosition])
                    {
                        endObjPosition = 0;
                        endStreamPosition++;

                        // We've finished reading 'endstream', add it to the end tokens we've seen.
                        if (endStreamPosition == streamPart.Length && (!inputBytes.MoveNext() || ReadHelper.IsWhitespace(inputBytes.CurrentByte)))
                        {
                            var token = new PossibleStreamEndLocation(inputBytes.CurrentOffset - OperatorToken.EndStream.Data.Length, OperatorToken.EndStream);

                            possibleEndLocation = token;

                            if (length.HasValue && read > length)
                            {
                                break;
                            }

                            endStreamPosition = 0;
                        }
                    }
                    else if (inputBytes.CurrentByte == objPart[endObjPosition])
                    {
                        // We are reading 'obj' after 'end'

                        endStreamPosition = 0;
                        endObjPosition++;

                        // We have finished reading 'endobj'.
                        if (endObjPosition == objPart.Length)
                        {
                            // If we saw an 'endstream' or 'endobj' previously we've definitely hit the end now.
                            if (possibleEndLocation != null)
                            {
                                var lastEndToken = possibleEndLocation.Value;

                                inputBytes.Seek(lastEndToken.Offset + lastEndToken.Type.Data.Length + 1);

                                break;
                            }

                            var token = new PossibleStreamEndLocation(inputBytes.CurrentOffset - OperatorToken.EndObject.Data.Length, OperatorToken.EndObject);

                            possibleEndLocation = token;

                            if (read > length)
                            {
                                break;
                            }
                        }
                    }
                    else
                    {
                        // We were reading 'end' but then we had a character mismatch.
                        // Reset all the counters.

                        endStreamPosition = 0;
                        endObjPosition = 0;
                        commonPartPosition = 0;
                    }
                }
                else
                {
                    // For safety reset every counter in case we had a partial read.

                    endStreamPosition = 0;
                    endObjPosition = 0;
                    commonPartPosition = (inputBytes.CurrentByte == endWordPart[0]) ? 1 : 0;
                }

                read++;
            }

            long streamDataEnd = inputBytes.CurrentOffset + 1;

            if (possibleEndLocation == null)
                return false;

            var lastEnd = possibleEndLocation;

            var dataLength = lastEnd.Value.Offset - startDataOffset;

            // 3 characters, 'e', '\n' and possibly '\r'
            inputBytes.Seek(lastEnd.Value.Offset - 3);
            inputBytes.MoveNext();

            if (inputBytes.CurrentByte == '\r')
            {
                dataLength -= 3;
            }
            else
            {
                dataLength -= 2;
            }

            Memory<byte> data = new byte[dataLength];

            inputBytes.Seek(streamDataStart);
            inputBytes.Read(data.Span);

            inputBytes.Seek(streamDataEnd);

            stream = new StreamToken(streamDictionaryToken, data);

            return true;
        }

        private static bool TryReadUsingLength(IInputBytes inputBytes, long? length, long startDataOffset, [NotNullWhen(true)] out byte[]? data)
        {
            data = null;

            if (!length.HasValue || length.Value + startDataOffset >= inputBytes.Length)
            {
                return false;
            }

            var readBuffer = new byte[EndstreamBytes.Length];

            var newlineCount = 0;

            inputBytes.Seek(length.Value + startDataOffset);

            var next = inputBytes.Peek();

            if (next.HasValue && ReadHelper.IsEndOfLine(next.Value))
            {
                newlineCount++;
                inputBytes.MoveNext();

                next = inputBytes.Peek();

                if (next.HasValue && ReadHelper.IsEndOfLine(next.Value))
                {
                    newlineCount++;
                    inputBytes.MoveNext();
                }
            }

            var readLength = inputBytes.Read(readBuffer);

            if (readLength != readBuffer.Length)
            {
                return false;
            }

            for (var i = 0; i < EndstreamBytes.Length; i++)
            {
                if (readBuffer[i] != EndstreamBytes[i])
                {
                    inputBytes.Seek(startDataOffset);
                    return false;
                }
            }

            inputBytes.Seek(startDataOffset);

            data = new byte[(int)length.Value];

            var countRead = inputBytes.Read(data);

            if (countRead != data.Length)
            {
                throw new InvalidOperationException($"Reading using the stream length failed to read as many bytes as the stream specified. Wanted {length.Value}, got {countRead} at {startDataOffset + 1}.");
            }

            inputBytes.Read(readBuffer);
            // Skip for the line break before 'endstream'.
            for (var i = 0; i < newlineCount; i++)
            {
                var read = inputBytes.MoveNext();
                if (!read)
                {
                    inputBytes.Seek(startDataOffset);
                    return false;
                }
            }

            // 1 skip to move past the 'm' in 'endstream'
            inputBytes.MoveNext();

            return true;
        }

        private DictionaryToken GetStreamDictionary()
        {
            DictionaryToken streamDictionaryToken;
            if (previousTokens[2] is DictionaryToken firstDictionary)
            {
                streamDictionaryToken = firstDictionary;
            }
            else if (previousTokens[1] is DictionaryToken secondDictionary)
            {
                streamDictionaryToken = secondDictionary;
            }
            else
            {
                throw new PdfDocumentFormatException("No dictionary token was found prior to the 'stream' operator. Previous tokens were:" +
                                                     $" {previousTokens[2]} and {previousTokens[1]}.");
            }

            return streamDictionaryToken;
        }

        private long? GetStreamLength(DictionaryToken dictionary)
        {
            if (!dictionary.Data.TryGetValue("Length", out var lengthValue))
            {
                return null;
            }

            long? length = default(long?);

            // Can either be number in the stream dictionary.
            if (lengthValue is NumericToken numeric)
            {
                return numeric.Long;
            }

            long currentOffset = inputBytes.CurrentOffset;

            // Or a reference to another numeric object.
            if (lengthValue is IndirectReferenceToken lengthReference)
            {
                // We can only find it if we know where it is.
                if (objectLocationProvider.TryGetOffset(lengthReference.Data, out var offset))
                {
                    if (offset < 0)
                    {
                        ushort searchDepth = 0;
                        var result = GetObjectFromStream(lengthReference.Data, offset, ref searchDepth);

                        if (!(result.Data is NumericToken streamLengthToken))
                        {
                            throw new PdfDocumentFormatException($"Could not locate the length object with offset {offset} which should have been in a stream." +
                                                                 $" Found: {result.Data}.");
                        }

                        return streamLengthToken.Long;
                    }
                    // Move to the length object and read it.
                    Seek(offset);

                    // Keep a copy of the read tokens here since this list must be empty prior to move next.
                    var oldData = new List<IToken>(readTokens);
                    readTokens.Clear();
                    if (MoveNext() && ((ObjectToken)CurrentToken!).Data is NumericToken lengthToken)
                    {
                        length = lengthToken.Long;
                    }
                    readTokens.AddRange(oldData);

                    // Move back to where we started.
                    Seek(currentOffset);
                }
                else
                {
                    // warn, we had a reference to a length object but didn't find it...
                }
            }

            return length;
        }

        private static bool ReadStreamTokenStart(IInputBytes input, long tokenStart)
        {
            input.Seek(tokenStart);

            for (var i = 0; i < OperatorToken.StartStream.Data.Length; i++)
            {
                if (!input.MoveNext() || input.CurrentByte != OperatorToken.StartStream.Data[i])
                {
                    input.Seek(tokenStart);
                    return false;
                }
            }

            return true;
        }

        public bool TryReadToken<T>(out T token) where T : class, IToken
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(PdfTokenScanner));
            }

            return coreTokenScanner.TryReadToken(out token);
        }

        public void Seek(long position)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(PdfTokenScanner));
            }

            coreTokenScanner.Seek(position);
        }

        public void RegisterCustomTokenizer(byte firstByte, ITokenizer tokenizer)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(PdfTokenScanner));
            }

            coreTokenScanner.RegisterCustomTokenizer(firstByte, tokenizer);
        }

        public void DeregisterCustomTokenizer(ITokenizer tokenizer)
        {
            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(PdfTokenScanner));
            }

            coreTokenScanner.DeregisterCustomTokenizer(tokenizer);
        }

        public ObjectToken? Get(IndirectReference reference)
        {
            ushort searchDepth = 0;
            return Get(reference, ref searchDepth);
        }

        private ObjectToken? Get(IndirectReference reference, ref ushort searchDepth)
        {
            if (searchDepth > 100)
            {
                throw new PdfDocumentFormatException("Reached maximum search depth while getting indirect reference.");
            }

            searchDepth++;


            if (isDisposed)
            {
                throw new ObjectDisposedException(nameof(PdfTokenScanner));
            }

            if (overwrittenTokens.TryGetValue(reference, out var value))
            {
                return value;
            }

            if (objectLocationProvider.TryGetCached(reference, out var objectToken))
            {
                return objectToken;
            }

            if (!objectLocationProvider.TryGetOffset(reference, out var offset))
            {
                return null;
            }

            // Negative offsets refer to a stream with that number.
            if (offset < 0)
            {
                var result = GetObjectFromStream(reference, offset, ref searchDepth);

                return result;
            }

            if (offset == 0 && reference.Generation > ushort.MaxValue)
            {
                // TODO - To remove as should not happen anymore
                return new ObjectToken(offset, reference, NullToken.Instance);
            }

            Seek(offset);

            if (!MoveNext())
            {
                TryBruteForceFileToFindReference(reference, out var bfObjectToken);
                return bfObjectToken;
            }

            var found = (ObjectToken)CurrentToken!;

            if (found.Number.Equals(reference))
            {
                return found;
            }

            TryBruteForceFileToFindReference(reference, out var bfToken);

            return bfToken;
        }

        public void ReplaceToken(IndirectReference reference, IToken token)
        {
            // Using 0 position as it isn't written to stream and this value doesn't
            // seem to be used by any callers. In future may need to revisit this.
            overwrittenTokens[reference] = new ObjectToken(0, reference, token);
        }

        private bool TryBruteForceFileToFindReference(IndirectReference reference, [NotNullWhen(true)] out ObjectToken? result)
        {
            result = null;
            try
            {
                // Brute force read the entire file
                isBruteForcing = true;

                Seek(0);

                while (MoveNext())
                {
                    objectLocationProvider.Cache((ObjectToken)CurrentToken!, true);
                }

                if (!objectLocationProvider.TryGetCached(reference, out var objectToken))
                {
                    return false;
                }

                result = objectToken;

                return true;
            }
            finally
            {
                isBruteForcing = false;
            }
        }

        private ObjectToken GetObjectFromStream(IndirectReference reference, long offset, ref ushort searchDepth)
        {
            var streamObjectNumber = offset * -1;

            var streamObject = Get(new IndirectReference(streamObjectNumber, 0), ref searchDepth);

            if (!(streamObject?.Data is StreamToken stream))
            {
                throw new PdfDocumentFormatException("Requested a stream object by reference but the requested stream object " +
                                                     $"was not a stream: {reference}, {streamObject?.Data}.");
            }

            var objects = ParseObjectStream(stream, offset);

            foreach (var o in objects)
            {
                objectLocationProvider.Cache(o);
            }

            if (!objectLocationProvider.TryGetCached(reference, out var result))
            {
                throw new PdfDocumentFormatException($"Could not find the object {reference} in the stream {streamObjectNumber}.");
            }

            return result;
        }

        private IReadOnlyList<ObjectToken> ParseObjectStream(StreamToken stream, long offset)
        {
            if (!stream.StreamDictionary.TryGet(NameToken.N, out var numberToken)
            || !(numberToken is NumericToken numberOfObjects))
            {
                throw new PdfDocumentFormatException($"Object stream dictionary did not provide number of objects {stream.StreamDictionary}.");
            }

            if (!stream.StreamDictionary.TryGet(NameToken.First, out var firstToken)
            || !(firstToken is NumericToken firstTokenNum))
            {
                throw new PdfDocumentFormatException($"Object stream dictionary did not provide first object offset {stream.StreamDictionary}.");
            }

            long firstTokenOffset = firstTokenNum.Long;

            // Read the N integers
            var bytes = new MemoryInputBytes(stream.Decode(filterProvider, this));

            var scanner = new CoreTokenScanner(
                bytes,
                true,
                useLenientParsing: parsingOptions.UseLenientParsing,
                isStream: true);

            var objects = new List<(long, long)>();

            for (var i = 0; i < numberOfObjects.Int; i++)
            {
                scanner.MoveNext();
                var objectNumber = (NumericToken)scanner.CurrentToken;
                scanner.MoveNext();
                var byteOffset = (NumericToken)scanner.CurrentToken;

                objects.Add((objectNumber.Long, firstTokenOffset + byteOffset.Long));
            }

            var results = new List<ObjectToken>();

            for (var i = 0; i < objects.Count; i++)
            {
                var obj = objects[i];

                // Check item offset is in [currentPosition - 1; currentPosition + 1]
                bool isBetween = ((obj.Item2 - (scanner.CurrentPosition - 1)) | ((scanner.CurrentPosition + 1) - obj.Item2)) >= 0;
                if (!isBetween)
                {
                    // TODO - Not sure if it belongs here but fixes issue 1013.
                    // It is not clear what happens with this specific document 'document_with_failed_fonts.pdf'
                    // I could not find where the same logic is applied in pdfbox.
                    scanner.Seek(obj.Item2);
                }

                scanner.MoveNext();

                var token = scanner.CurrentToken;

                if (token.Equals(OperatorToken.EndObject))
                {
                    scanner.MoveNext();

                    token = scanner.CurrentToken;
                }

                results.Add(new ObjectToken(offset, new IndirectReference(obj.Item1, 0), token));
            }

            return results;
        }

        public void Dispose()
        {
            inputBytes?.Dispose();
            isDisposed = true;
        }
    }
}
