namespace UglyToad.PdfPig.Tokenization.Scanner
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using Encryption;
    using Exceptions;
    using Filters;
    using IO;
    using Parser.Parts;
    using Tokens;

    /// <summary>
    /// Tokenizes objects from bytes in a PDF file.
    /// </summary>
    internal interface IPdfTokenScanner : ISeekableTokenScanner
    {
        /// <summary>
        /// Tokenize the object with a given object number.
        /// </summary>
        /// <param name="reference">The object number for the object to tokenize.</param>
        /// <returns>The tokenized object.</returns>
        ObjectToken Get(IndirectReference reference);
    }

    internal class PdfTokenScanner : IPdfTokenScanner
    {
        private readonly IInputBytes inputBytes;
        private readonly IObjectLocationProvider objectLocationProvider;
        private readonly IFilterProvider filterProvider;
        private readonly CoreTokenScanner coreTokenScanner;

        private IEncryptionHandler encryptionHandler;

        /// <summary>
        /// Stores tokens encountered between obj - endobj markers for each <see cref="MoveNext"/> call.
        /// Cleared after each operation.
        /// </summary>
        private readonly List<IToken> readTokens = new List<IToken>();

        // Store the previous 2 tokens and their positions so we can backtrack to find object numbers and stream dictionaries.
        private readonly long[] previousTokenPositions = new long[2];
        private readonly IToken[] previousTokens = new IToken[2];

        public IToken CurrentToken { get; private set; }

        public long CurrentPosition => coreTokenScanner.CurrentPosition;

        public PdfTokenScanner(IInputBytes inputBytes, IObjectLocationProvider objectLocationProvider, IFilterProvider filterProvider, 
            IEncryptionHandler encryptionHandler)
        {
            this.inputBytes = inputBytes;
            this.objectLocationProvider = objectLocationProvider;
            this.filterProvider = filterProvider;
            this.encryptionHandler = encryptionHandler;
            coreTokenScanner = new CoreTokenScanner(inputBytes);
        }

        public void UpdateEncryptionHandler(IEncryptionHandler newHandler)
        {
            encryptionHandler = newHandler ?? throw new ArgumentNullException(nameof(newHandler));
        }

        public bool MoveNext()
        {
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

                previousTokens[1] = coreTokenScanner.CurrentToken;
                previousTokenPositions[1] = coreTokenScanner.CurrentTokenStart;
            }

            // We only read partial tokens.
            if (tokensRead < 2)
            {
                return false;
            }

            var startPosition = previousTokenPositions[0];
            var objectNumber = previousTokens[0] as NumericToken;
            var generation = previousTokens[1] as NumericToken;

            if (objectNumber == null || generation == null)
            {
                throw new PdfDocumentFormatException("The obj operator (start object) was not preceded by a 2 numbers." +
                                                     $"Instead got: {previousTokens[0]} {previousTokens[1]} obj");
            }

            // Read all tokens between obj and endobj.
            while (coreTokenScanner.MoveNext() && !Equals(coreTokenScanner.CurrentToken, OperatorToken.EndObject))
            {
                if (coreTokenScanner.CurrentToken is CommentToken)
                {
                    continue;
                }

                if (ReferenceEquals(coreTokenScanner.CurrentToken, OperatorToken.StartObject))
                {
                    // This should never happen.
                    Debug.Assert(false, "Encountered a start object 'obj' operator before the end of the previous object.");
                    return false;
                }

                if (ReferenceEquals(coreTokenScanner.CurrentToken, OperatorToken.StartStream))
                {
                    // Read stream: special case.
                    if (TryReadStream(coreTokenScanner.CurrentTokenStart, out var stream))
                    {
                        readTokens.Clear();
                        readTokens.Add(stream);
                    }
                }
                else
                {
                    readTokens.Add(coreTokenScanner.CurrentToken);
                }

                previousTokens[0] = previousTokens[1];
                previousTokenPositions[0] = previousTokenPositions[1];

                previousTokens[1] = coreTokenScanner.CurrentToken;
                previousTokenPositions[1] = coreTokenScanner.CurrentPosition;
            }

            if (!ReferenceEquals(coreTokenScanner.CurrentToken, OperatorToken.EndObject))
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
                Debug.Assert(readTokens.Count == 1, "Found more than 1 token in an object.");

                token = readTokens[readTokens.Count - 1];
            }

            token = encryptionHandler.Decrypt(reference, token);

            CurrentToken = new ObjectToken(startPosition, reference, token);

            objectLocationProvider.UpdateOffset(reference, startPosition);

            readTokens.Clear();
            return true;
        }

        private bool TryReadStream(long startStreamTokenOffset, out StreamToken stream)
        {
            stream = null;

            DictionaryToken streamDictionaryToken = GetStreamDictionary();

            // Get the expected length from the stream dictionary if present.
            long? length = GetStreamLength(streamDictionaryToken);

            // Verify again that we start with "stream"
            var hasStartStreamToken = ReadStreamTokenStart(inputBytes, startStreamTokenOffset);

            if (!hasStartStreamToken)
            {
                return false;
            }

            // From the specification: The stream operator should be followed by \r\n or \n, not just \r.
            if (!inputBytes.MoveNext())
            {
                return false;
            }

            if (inputBytes.CurrentByte == '\r')
            {
                inputBytes.MoveNext();
            }

            if (inputBytes.CurrentByte != '\n')
            {
                return false;
            }

            // Store where we started reading the first byte of data.
            long startDataOffset = inputBytes.CurrentOffset;

            // Store how many bytes we have read for checking against Length.
            long read = 0;

            // We want to check if we ever read 'endobj' or 'endstream'.
            int endObjPosition = 0;
            int endStreamPosition = 0;
            int commonPartPosition = 0;

            const string commonPart = "end";
            const string streamPart = "stream";
            const string objPart = "obj";

            // Track any 'endobj' or 'endstream' operators we see.
            var observedEndLocations = new List<PossibleStreamEndLocation>();
            
            // Begin reading the stream.
            using (var memoryStream = new MemoryStream())
            using (var binaryWrite = new BinaryWriter(memoryStream))
            {
                while (inputBytes.MoveNext())
                {
                    if (length.HasValue && read == length)
                    {
                        // TODO: read ahead and check we're at the end...
                        // break;
                    }

                    // We are reading 'end' (possibly).
                    if (commonPartPosition < commonPart.Length && inputBytes.CurrentByte == commonPart[commonPartPosition])
                    {
                        commonPartPosition++;
                    }
                    else if (commonPartPosition == commonPart.Length)
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

                                observedEndLocations.Add(token);

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
                                if (observedEndLocations.Count > 0)
                                {
                                    var lastEndToken = observedEndLocations[observedEndLocations.Count - 1];

                                    inputBytes.Seek(lastEndToken.Offset + lastEndToken.Type.Data.Length + 1);

                                    break;
                                }

                                var token = new PossibleStreamEndLocation(inputBytes.CurrentOffset - OperatorToken.EndObject.Data.Length, OperatorToken.EndObject);
                                observedEndLocations.Add(token);

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
                        commonPartPosition = (inputBytes.CurrentByte == commonPart[0]) ? 1  : 0;
                    }
                    
                    binaryWrite.Write(inputBytes.CurrentByte);

                    read++;
                }

                binaryWrite.Flush();

                if (observedEndLocations.Count == 0)
                {
                    return false;
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                if (length.HasValue && memoryStream.Length >= length)
                {
                    // Use the declared length to copy just the data we want.
                    byte[] data = new byte[length.Value];

                    memoryStream.Read(data, 0, (int)length.Value);

                    stream = new StreamToken(streamDictionaryToken, data);
                }
                else
                {
                    // Work out where '\r\nendobj' or '\r\nendstream' occurs and read everything up to that.
                    var lastEnd = observedEndLocations[observedEndLocations.Count - 1];

                    var dataLength = lastEnd.Offset - startDataOffset;

                    var current = inputBytes.CurrentOffset;

                    // 3 characters, 'e', '\n' and possibly '\r'
                    inputBytes.Seek(lastEnd.Offset - 3);
                    inputBytes.MoveNext();

                    if (inputBytes.CurrentByte == '\r')
                    {
                        dataLength -= 3;
                    }
                    else
                    {
                        dataLength -= 2;
                    }

                    inputBytes.Seek(current);

                    byte[] data = new byte[dataLength];

                    memoryStream.Read(data, 0, (int)dataLength);

                    stream = new StreamToken(streamDictionaryToken, data);
                }
            }

            return true;
        }

        private DictionaryToken GetStreamDictionary()
        {
            DictionaryToken streamDictionaryToken;
            if (previousTokens[1] is DictionaryToken firstDictionary)
            {
                streamDictionaryToken = firstDictionary;
            }
            else if (previousTokens[0] is DictionaryToken secondDictionary)
            {
                streamDictionaryToken = secondDictionary;
            }
            else
            {
                throw new PdfDocumentFormatException("No dictionary token was found prior to the 'stream' operator. Previous tokens were:" +
                                                     $" {previousTokens[1]} and {previousTokens[0]}.");
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
                    // Move to the length object and read it.
                    Seek(offset);

                    // Keep a copy of the read tokens here since this list must be empty prior to move next.
                    var oldData = new List<IToken>(readTokens);
                    readTokens.Clear();
                    if (MoveNext() && ((ObjectToken)CurrentToken).Data is NumericToken lengthToken)
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
            return coreTokenScanner.TryReadToken(out token);
        }

        public void Seek(long position)
        {
            coreTokenScanner.Seek(position);
        }

        public void RegisterCustomTokenizer(byte firstByte, ITokenizer tokenizer)
        {
            coreTokenScanner.RegisterCustomTokenizer(firstByte, tokenizer);
        }

        public void DeregisterCustomTokenizer(ITokenizer tokenizer)
        {
            coreTokenScanner.DeregisterCustomTokenizer(tokenizer);
        }

        public ObjectToken Get(IndirectReference reference)
        {
            if (objectLocationProvider.TryGetCached(reference, out var objectToken))
            {
                return objectToken;
            }

            if (!objectLocationProvider.TryGetOffset(reference, out var offset))
            {
                throw new InvalidOperationException($"Could not find the object with reference: {reference}.");
            }

            // Negative offsets refer to a stream with that number.
            if (offset < 0)
            {
                var result = GetObjectFromStream(reference, offset);

                return result;
            }

            Seek(offset);

            if (!MoveNext())
            {
                throw new InvalidOperationException($"Could not parse the object with reference: {reference}.");
            }

            return (ObjectToken)CurrentToken;
        }

        private ObjectToken GetObjectFromStream(IndirectReference reference, long offset)
        {
            var streamObjectNumber = offset * -1;

            var streamObject = Get(new IndirectReference(streamObjectNumber, 0));

            if (!(streamObject.Data is StreamToken stream))
            {
                throw new PdfDocumentFormatException("Requested a stream object by reference but the requested stream object " +
                                                     $"was not a stream: {reference}, {streamObject.Data}.");
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
            || !(firstToken is NumericToken))
            {
                throw new PdfDocumentFormatException($"Object stream dictionary did not provide first object offset {stream.StreamDictionary}.");
            }

            // Read the N integers
            var bytes = new ByteArrayInputBytes(stream.Decode(filterProvider));

            var scanner = new CoreTokenScanner(bytes);

            var objects = new List<Tuple<long, long>>();

            for (var i = 0; i < numberOfObjects.Int; i++)
            {
                scanner.MoveNext();
                var objectNumber = (NumericToken) scanner.CurrentToken;
                scanner.MoveNext();
                var byteOffset = (NumericToken) scanner.CurrentToken;

                objects.Add(Tuple.Create(objectNumber.Long, byteOffset.Long));
            }

            var results = new List<ObjectToken>();

            for (var i = 0; i < objects.Count; i++)
            {
                var obj = objects[i];

                scanner.MoveNext();

                var token = scanner.CurrentToken;

                results.Add(new ObjectToken(offset, new IndirectReference(obj.Item1, 0), token));
            }

            return results;
        }
    }
}
