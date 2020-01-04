namespace UglyToad.PdfPig.Tokenization
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Core;
    using Parser.Parts;
    using Tokens;

    internal class NameTokenizer : ITokenizer
    {
        public bool ReadsNextByte { get; } = true;

        public bool TryTokenize(byte currentByte, IInputBytes inputBytes, out IToken token)
        {
            token = null;

            if (currentByte != '/')
            {
                return false;
            }

            var bytes = new List<byte>();

            bool escapeActive = false;
            int postEscapeRead = 0;
            var escapedChars = new char[2];

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
                            var hex = new string(escapedChars);

                            var characterToWrite = (byte)Convert.ToInt32(hex, 16);
                            bytes.Add(characterToWrite);

                            escapeActive = false;
                            postEscapeRead = 0;
                        }
                    }
                    else
                    {
                        bytes.Add((byte)'#');

                        if (postEscapeRead == 1)
                        {
                            bytes.Add((byte)escapedChars[0]);
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

                        bytes.Add(b);
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
                    bytes.Add(b);
                }
            }

            byte[] byteArray = bytes.ToArray();

            var str = ReadHelper.IsValidUtf8(byteArray)
                ? Encoding.UTF8.GetString(byteArray)
                : Encoding.GetEncoding("windows-1252").GetString(byteArray);

            token = NameToken.Create(str);

            return true;
        }
    }
}