namespace UglyToad.PdfPig.Tokenization
{
    using Core;
    using System;
    using System.Text;
    using Tokens;

    internal sealed class PlainTokenizer : ITokenizer
    {
        /// <summary>
        /// <c>true</c> required to read malformed CMap streams which omit the whitespace mandated between tokens (see #1331).
        /// It must NOT be enabled for general content stream parsing, where operators such as the Type 3 glyph operators
        /// <c>d0</c> and <c>d1</c> legitimately contain digits.
        /// </summary>
        private readonly bool splitOnDigit;

        public bool ReadsNextByte => true;

        public PlainTokenizer(bool splitOnDigit = false)
        {
            this.splitOnDigit = splitOnDigit;
        }

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            if (ReadHelper.IsWhitespace(currentByte))
            {
                token = null;
                return false;
            }

            if (inputBytes is MemoryInputBytes memoryBytes)
            {
                token = TokenizeInMemory(memoryBytes);
                return true;
            }

            using var builder = new ValueStringBuilder(stackalloc char[16]);

            builder.Append((char)currentByte);

            while (inputBytes.MoveNext())
            {
                if (IsTerminator(inputBytes.CurrentByte))
                {
                    break;
                }

                builder.Append((char)inputBytes.CurrentByte);
            }

            var text = builder.AsSpan();

            token = text switch {
                "true"  => BooleanToken.True,
                "false" => BooleanToken.False,
                "null"  => NullToken.Instance,
                _       => OperatorToken.Create(text),
            };

            return true;
        }

        /// <summary>
        /// The same tokenizing as the byte-by-byte loop, done on the input's span: the end of
        /// the token is found in place and the token is created from those bytes, which saves the
        /// interface call per byte and the copy into a character buffer. Content streams are always
        /// read from memory so this is the path they take.
        /// </summary>
        private IToken TokenizeInMemory(MemoryInputBytes inputBytes)
        {
            var span = inputBytes.Span;
            var start = inputBytes.Position;

            var end = start + 1;
            while (end < span.Length && !IsTerminator(span[end]))
            {
                end++;
            }

            // Leave the input as the loop would: on the terminator, or on the last byte when
            // the token ran to the end of the input.
            inputBytes.MoveTo(end < span.Length ? end : span.Length - 1);

            var text = span.Slice(start, end - start);

            if (text.Length == 4)
            {
                if (text.SequenceEqual("true"u8))
                {
                    return BooleanToken.True;
                }

                if (text.SequenceEqual("null"u8))
                {
                    return NullToken.Instance;
                }
            }
            else if (text.Length == 5 && text.SequenceEqual("false"u8))
            {
                return BooleanToken.False;
            }

            return OperatorToken.Create(text);
        }

        private bool IsTerminator(byte b)
        {
            if (ReadHelper.IsWhitespace(b))
            {
                return true;
            }

            if (b is (byte)'<' or (byte)'[' or (byte)'/' or (byte)']' or (byte)'>' or (byte)'(' or (byte)')' or (byte)'%')
            {
                return true;
            }

            // Malformed CMap, see #1331
            return splitOnDigit && b is >= (byte)'0' and <= (byte)'9';
        }
    }
}
