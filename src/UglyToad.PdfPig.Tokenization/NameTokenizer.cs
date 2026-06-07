namespace UglyToad.PdfPig.Tokenization
{
    using System;
    using System.Text;
    using Core;
    using Tokens;

#if NET
    using System.Text.Unicode;
#endif

    internal sealed class NameTokenizer : ITokenizer
    {
        static NameTokenizer()
        {
#if NET
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

            string str;
            if (isValidUtf8)
            {
                // A name object treated as text should be interpreted as UTF-8 (PDF 2.0, 7.3.5).
                str = Encoding.UTF8.GetString(byteArray);
            }
            else if (LooksLikeGbk(byteArray))
            {
                // Some producers (commonly Microsoft Office / WPS on Chinese systems) write the raw
                // GBK/GB18030 bytes of CJK font names into name objects instead of UTF-8 or #-escapes.
                // Only re-decode when every high byte forms a valid GBK double-byte sequence, otherwise
                // an isolated Latin-1 byte (e.g. 'é' in a Western name) would be mangled. See issue #1266.
                str = Encoding.GetEncoding(936).GetString(byteArray);
            }
            else
            {
                str = Encoding.GetEncoding("windows-1252").GetString(byteArray);
            }

            token = NameToken.Create(str);

            return true;
        }

        /// <summary>
        /// Determines whether the bytes (which are not valid UTF-8) look like GBK/GB18030 encoded text,
        /// i.e. every byte is either ASCII or part of a valid GBK double-byte sequence and at least one
        /// such double-byte sequence is present.
        /// </summary>
        private static bool LooksLikeGbk(ReadOnlySpan<byte> bytes)
        {
            bool sawDoubleByte = false;

            for (int i = 0; i < bytes.Length;)
            {
                byte b = bytes[i];

                if (b < 0x80)
                {
                    // ASCII (e.g. the subset prefix "ABCDEE+" or a ",Bold" suffix).
                    i++;
                    continue;
                }

                // High byte: it must be the lead byte of a GBK double-byte sequence (0x81-0xFE).
                if (b < 0x81 || b == 0xFF || i + 1 >= bytes.Length)
                {
                    return false;
                }

                // The trailing byte must be in 0x40-0xFE excluding 0x7F.
                byte trail = bytes[i + 1];
                if (trail < 0x40 || trail > 0xFE || trail == 0x7F)
                {
                    return false;
                }

                sawDoubleByte = true;
                i += 2;
            }

            return sawDoubleByte;
        }
    }
}