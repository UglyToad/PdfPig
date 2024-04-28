namespace UglyToad.PdfPig.Tokenization
{
    using System;
    using System.Text;
    using Core;
    using Tokens;

#if NET8_0_OR_GREATER
    using System.Text.Unicode;
#endif

    internal sealed class NameTokenizer : ITokenizer
    {
        static NameTokenizer()
        {
#if NET6_0_OR_GREATER
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
#endif
        }

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

#if NET8_0_OR_GREATER
            var byteArray = bytes.WrittenSpan;
            bool isValidUtf8 = Utf8.IsValid(byteArray);
#else
            var byteArray = bytes.WrittenSpan.ToArray();
            bool isValidUtf8 = ReadHelper.IsValidUtf8(byteArray);
#endif

            var str = isValidUtf8
                ? Encoding.UTF8.GetString(byteArray)
                : Encoding.GetEncoding("windows-1252").GetString(byteArray);
            
            token = NameToken.Create(str);

            return true;
        }
    }
}