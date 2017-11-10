namespace UglyToad.Pdf.Tokenization
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using IO;
    using Parser.Parts;
    using Tokens;

    public class NameTokenizer : ITokenizer
    {
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
                    if (ReadHelper.IsHexDigit((char)b))
                    {
                        escapedChars[postEscapeRead] = (char)b;
                        postEscapeRead++;

                        if (postEscapeRead == 2)
                        {
                            string hex = new string(escapedChars);
                            try
                            {
                                var characterToWrite = (byte)Convert.ToInt32(hex, 16);
                                bytes.Add(characterToWrite);
                            }
                            catch (FormatException e)
                            {
                                throw new InvalidOperationException("Error: expected hex digit, actual='" + hex + "'", e);
                            }

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

            token = new NameToken(str);

            return true;
        }
    }
}