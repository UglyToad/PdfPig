namespace UglyToad.PdfPig.Fonts.Type1.Parser
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Encodings;
    using Fonts;
    using Type1;
    using Tokenization.Scanner;
    using Tokens;

    /// <summary>
    /// Parse Adobe Type 1 font format.
    /// </summary>
    public static class Type1FontParser
    {
        private const string ClearToMark = "cleartomark";

        private const int PfbFileIndicator = 0x80;

        private static readonly Type1EncryptedPortionParser EncryptedPortionParser = new Type1EncryptedPortionParser();
        
        /// <summary>
        /// Parses an embedded Adobe Type 1 font file.
        /// </summary>
        /// <param name="inputBytes">The bytes of the font program.</param>
        /// <param name="length1">The length in bytes of the clear text portion of the font program.</param>
        /// <param name="length2">The length in bytes of the encrypted portion of the font program.</param>
        /// <returns>The parsed type 1 font.</returns>
        public static Type1Font Parse(IInputBytes inputBytes, int length1, int length2)
        {
            // Sometimes the entire PFB file including the header bytes can be included which prevents parsing in the normal way.
            var isEntirePfbFile = inputBytes.Peek() == PfbFileIndicator;

            IReadOnlyList<byte> eexecPortion = new byte[0];

            if (isEntirePfbFile)
            {
                var (ascii, binary) = ReadPfbHeader(inputBytes);

                eexecPortion = binary;
                inputBytes = new ByteArrayInputBytes(ascii);
            }

            var scanner = new CoreTokenScanner(inputBytes);

            if (!scanner.TryReadToken(out CommentToken comment) || !comment.Data.StartsWith("!"))
            {
                throw new InvalidFontFormatException("The Type1 program did not start with '%!'.");
            }

            string name;
            var parts = comment.Data.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 3)
            {
                name = parts[1];
            }
            else
            {
                name = "Unknown";
            }

            var comments = new List<string>();

            while (scanner.MoveNext() && scanner.CurrentToken is CommentToken commentToken)
            {
                comments.Add(commentToken.Data);
            }

            var dictionaries = new List<DictionaryToken>();

            // Override arrays and names since type 1 handles these differently.
            var arrayTokenizer = new Type1ArrayTokenizer();
            var nameTokenizer = new Type1NameTokenizer();
            scanner.RegisterCustomTokenizer((byte)'{', arrayTokenizer);
            scanner.RegisterCustomTokenizer((byte)'/', nameTokenizer);
            
            try
            {
                var tempEexecPortion = new List<byte>();
                var tokenSet = new PreviousTokenSet();
                tokenSet.Add(scanner.CurrentToken);
                while (scanner.MoveNext())
                {
                    if (scanner.CurrentToken is OperatorToken operatorToken)
                    {
                        if (Equals(scanner.CurrentToken, OperatorToken.Eexec))
                        {
                            int offset = 0;

                            while (inputBytes.MoveNext())
                            {
                                if (inputBytes.CurrentByte == (byte)ClearToMark[offset])
                                {
                                    offset++;
                                }
                                else
                                {
                                    if (offset > 0)
                                    {
                                        for (int i = 0; i < offset; i++)
                                        {
                                            tempEexecPortion.Add((byte)ClearToMark[i]);
                                        }
                                    }

                                    offset = 0;
                                }

                                if (offset == ClearToMark.Length)
                                {
                                    break;
                                }

                                if (offset > 0)
                                {
                                    continue;
                                }

                                tempEexecPortion.Add(inputBytes.CurrentByte);
                            }
                        }
                        else
                        {
                            HandleOperator(operatorToken, scanner, tokenSet, dictionaries);
                        }
                    }

                    tokenSet.Add(scanner.CurrentToken);
                }

                if (!isEntirePfbFile)
                {
                    eexecPortion = tempEexecPortion;
                }
            }
            finally
            {
                scanner.DeregisterCustomTokenizer(arrayTokenizer);
                scanner.DeregisterCustomTokenizer(nameTokenizer);
            }

            var encoding = GetEncoding(dictionaries);
            var matrix = GetFontMatrix(dictionaries);
            var boundingBox = GetBoundingBox(dictionaries);

            var (privateDictionary, charStrings) = EncryptedPortionParser.Parse(eexecPortion, false);

            return new Type1Font(name, encoding, matrix, boundingBox ?? new PdfRectangle(), privateDictionary, charStrings);
        }

        /// <summary>
        /// Where an entire PFB file has been embedded in the PDF we read the header first.
        /// </summary>
        private static (byte[] ascii, byte[] binary) ReadPfbHeader(IInputBytes bytes)
        {
            /*
             * The header is a 6 byte sequence. The first byte is 0x80 followed by 0x01 for the ASCII record indicator.
             * The following 4 bytes determine the size/length of the ASCII part of the PFB file.
             * After the ASCII part another 6 byte sequence is present, this time 0x80 0x02 for the Binary part length.
             * A 3rd sequence is present at the end re-stating the ASCII length but this is surplus to requirements.
             */

            // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
            int ReadSize(byte recordType)
            {
                bytes.MoveNext();

                if (bytes.CurrentByte != PfbFileIndicator)
                {
                    throw new InvalidOperationException($"File does not start with 0x80, which indicates a full PFB file. Instead got: {bytes.CurrentByte}");
                }

                bytes.MoveNext();

                if (bytes.CurrentByte != recordType)
                {
                    throw new InvalidOperationException($"Encountered unexpected header type in the PFB file: {bytes.CurrentByte}");
                }

                bytes.MoveNext();
                int size = bytes.CurrentByte;
                bytes.MoveNext();
                size += bytes.CurrentByte << 8;
                bytes.MoveNext();
                size += bytes.CurrentByte << 16;
                bytes.MoveNext();
                size += bytes.CurrentByte << 24;

                return size;
            }

            var asciiSize = ReadSize(0x01);
            var asciiPart = new byte[asciiSize];

            int i = 0;
            while (i < asciiSize)
            {
                bytes.MoveNext();
                asciiPart[i] = bytes.CurrentByte;
                i++;
            }

            var binarySize = ReadSize(0x02);

            var binaryPart = new byte[binarySize];
            i = 0;

            while (i < binarySize)
            {
                bytes.MoveNext();
                binaryPart[i] = bytes.CurrentByte;
                i++;
            }

            return (asciiPart, binaryPart);
        }

        private static void HandleOperator(OperatorToken token, ISeekableTokenScanner scanner, PreviousTokenSet set, List<DictionaryToken> dictionaries)
        {
            switch (token.Data)
            {
                case "dict":
                    var number = ((NumericToken)set[0]).Int;
                    var dictionary = ReadDictionary(number, scanner);

                    dictionaries.Add(dictionary);
                    break;
                default:
                    return;
            }
        }

        private static DictionaryToken ReadDictionary(int keys, ISeekableTokenScanner scanner)
        {
            IToken previousToken = null;

            var dictionary = new Dictionary<NameToken, IToken>();

            // Skip the operators "dup" etc to reach "begin".
            while (scanner.MoveNext() && (!(scanner.CurrentToken is OperatorToken operatorToken) || operatorToken.Data != "begin"))
            {
                // Skipping.
            }

            for (int i = 0; i < keys; i++)
            {
                if (!scanner.TryReadToken(out NameToken key))
                {
                    return new DictionaryToken(dictionary);
                }

                if (key.Data.Equals(NameToken.Encoding))
                {
                    var encoding = ReadEncoding(scanner);
                    dictionary[key] = (IToken)encoding.encoding ?? encoding.name;
                    continue;
                }

                while (scanner.MoveNext())
                {
                    if (scanner.CurrentToken == OperatorToken.Def)
                    {
                        dictionary[key] = previousToken;

                        break;
                    }

                    if (scanner.CurrentToken == OperatorToken.Dict)
                    {
                        if (!(previousToken is NumericToken numeric))
                        {
                            return new DictionaryToken(dictionary);
                        }

                        var inner = ReadDictionary(numeric.Int, scanner);

                        previousToken = inner;
                    }
                    else if (scanner.CurrentToken == OperatorToken.Readonly)
                    {
                        // skip
                    }
                    else if (scanner.CurrentToken is OperatorToken op && op.Data == "end")
                    {
                        // skip
                    }
                    else
                    {
                        previousToken = scanner.CurrentToken;
                    }
                }
            }

            return new DictionaryToken(dictionary);
        }

        private static (ArrayToken encoding, NameToken name) ReadEncoding(ISeekableTokenScanner scanner)
        {
            var result = new List<IToken>();

            // Treat encoding differently, it's what we came here for!
            if (!scanner.TryReadToken(out NumericToken _))
            {
                // The tokens following /Encoding may be StandardEncoding def.
                if (scanner.CurrentToken is OperatorToken encodingName
                    && encodingName.Data.Equals(NameToken.StandardEncoding))
                {
                    return (null, NameToken.StandardEncoding);
                }
                return (new ArrayToken(result), null);
            }

            if (!scanner.TryReadToken(out OperatorToken arrayOperatorToken) || arrayOperatorToken.Data != "array")
            {
                return (new ArrayToken(result), null);
            }

            var stoppedOnDup = false;

            while (scanner.MoveNext() && (!(scanner.CurrentToken is OperatorToken forOperator) || forOperator.Data != "for"))
            {
                if (!(scanner.CurrentToken is OperatorToken operatorToken))
                {
                    continue;
                }

                if (string.Equals(operatorToken.Data, "for", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (string.Equals(operatorToken.Data, OperatorToken.Dup.Data, StringComparison.OrdinalIgnoreCase))
                {
                    stoppedOnDup = true;
                    break;
                }
                // skip these operators for now, they're probably important...
            }

            if (scanner.CurrentToken != OperatorToken.For && !stoppedOnDup)
            {
                return (new ArrayToken(result), null);
            }

            bool IsDefOrReadonly()
            {
                return scanner.CurrentToken == OperatorToken.Def
                       || scanner.CurrentToken == OperatorToken.Readonly;
            }

            while ((stoppedOnDup || scanner.MoveNext()) && !IsDefOrReadonly())
            {
                stoppedOnDup = false;
                if (scanner.CurrentToken != OperatorToken.Dup)
                {
                    throw new InvalidFontFormatException("Expected the array for encoding to begin with 'dup'.");
                }

                scanner.MoveNext();
                var number = (NumericToken)scanner.CurrentToken;
                scanner.MoveNext();
                var name = (NameToken)scanner.CurrentToken;

                if (!scanner.TryReadToken(out OperatorToken put) || put != OperatorToken.Put)
                {
                    throw new InvalidFontFormatException("Expected the array entry to end with 'put'.");
                }

                result.Add(number);
                result.Add(name);
            }

            while (scanner.CurrentToken != OperatorToken.Def && scanner.MoveNext())
            {
                // skip
            }

            return (new ArrayToken(result), null);
        }

        private static IReadOnlyDictionary<int, string> GetEncoding(IReadOnlyList<DictionaryToken> dictionaries)
        {
            var result = new Dictionary<int, string>();

            foreach (var dictionary in dictionaries)
            {
                if (dictionary.TryGet(NameToken.Encoding, out var token))
                {
                    if (token is ArrayToken encodingArray)
                    {
                        for (var i = 0; i < encodingArray.Data.Count; i += 2)
                        {
                            var code = (NumericToken) encodingArray.Data[i];
                            var name = (NameToken) encodingArray.Data[i + 1];

                            result[code.Int] = name.Data;
                        }

                        return result;
                    }

                    if (token is NameToken encodingName && encodingName.Equals(NameToken.StandardEncoding))
                    {
                        return StandardEncoding.Instance.CodeToNameMap;
                    }
                }
            }

            return result;
        }

        private static ArrayToken GetFontMatrix(IReadOnlyList<DictionaryToken> dictionaries)
        {
            foreach (var dictionaryToken in dictionaries)
            {
                if (dictionaryToken.TryGet(NameToken.FontMatrix, out var token) && token is ArrayToken array)
                {
                    return array;
                }
            }

            return null;
        }

        private static PdfRectangle? GetBoundingBox(IReadOnlyList<DictionaryToken> dictionaries)
        {
            foreach (var dictionary in dictionaries)
            {
                if (dictionary.TryGet(NameToken.FontBbox, out var token) && token is ArrayToken array && array.Data.Count == 4)
                {
                    var x1 = (NumericToken)array.Data[0];
                    var y1 = (NumericToken)array.Data[1];
                    var x2 = (NumericToken)array.Data[2];
                    var y2 = (NumericToken)array.Data[3];

                    return new PdfRectangle(x1.Double, y1.Double, x2.Double, y2.Double);
                }
            }

            return null;
        }

        private class PreviousTokenSet
        {
            private readonly IToken[] tokens = new IToken[3];

            public IToken this[int index] => tokens[2 - index];

            public void Add(IToken token)
            {
                tokens[0] = tokens[1];
                tokens[1] = tokens[2];
                tokens[2] = token;
            }
        }
    }
}



