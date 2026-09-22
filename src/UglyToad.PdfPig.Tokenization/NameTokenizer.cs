namespace UglyToad.PdfPig.Tokenization
{
    using System;
    using Core;
    using Tokens;

    internal sealed class NameTokenizer : ITokenizer
    {
        public bool ReadsNextByte { get; } = true;

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            if (currentByte != '/')
            {
                return false;
            }

            using var bytes = new ArrayPoolBufferWriter<byte>();

            bool escapeActive = false;
            int postEscapeRead = 0;
            Span<char> escapedChars = stackalloc char[2];

            while (inputBytes.MoveNext())
            {
                var b = inputBytes.CurrentByte;

                if (b == '#')
                {
                    escapeActive = true;
                }
                else if (escapeActive)
                {
                    if (ReadHelper.IsHex((char)b))
                    {
                        escapedChars[postEscapeRead] = (char)b;
                        postEscapeRead++;

                        if (postEscapeRead == 2)
                        {
                            int high = escapedChars[0] <= '9' ? escapedChars[0] - '0' : char.ToUpper(escapedChars[0]) - 'A' + 10;
                            int low = escapedChars[1] <= '9' ? escapedChars[1] - '0' : char.ToUpper(escapedChars[1]) - 'A' + 10;

                            byte characterToWrite = (byte)(high * 16 + low);

                            bytes.Write(characterToWrite);

                            escapeActive = false;
                            postEscapeRead = 0;
                        }
                    }
                    else
                    {
                        bytes.Write((byte)'#');

                        if (postEscapeRead == 1)
                        {
                            bytes.Write((byte)escapedChars[0]);
                        }

                        if (ReadHelper.IsEndOfName(b))
                        {
                            break;
                        }

                        if (b == '#')
                        {
                            // Make it clear what's going on, we read something like #m#AE
                            // ReSharper disable once RedundantAssignment
                            escapeActive = true;
                            postEscapeRead = 0;
                            continue;
                        }

                        bytes.Write(b);
                        escapeActive = false;
                        postEscapeRead = 0;
                    }

                }
                else if (ReadHelper.IsEndOfName(b))
                {
                    break;
                }
                else
                {
                    bytes.Write(b);
                }
            }

            // ISO 32000-1, 7.3.5: name identity is defined by bytes, not text
            // encoding. A reversible mapping also keeps dictionary keys distinct.
            var str = OtherEncodings.BytesAsLatin1String(bytes.WrittenSpan);

            token = NameToken.Create(str);

            return true;
        }
    }
}