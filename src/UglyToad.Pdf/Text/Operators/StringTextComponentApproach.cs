namespace UglyToad.Pdf.Text.Operators
{
    using System;
    using System.Collections.Generic;

    public class StringTextComponentApproach : ITextComponentApproach
    {
        public bool CanRead(byte b, int offset)
        {
            if (offset == 0)
            {
                if (b == '<' || b == '(')
                {
                    return true;
                }

                return false;
            }

            return true;
        }

        public ITextObjectComponent Read(IReadOnlyList<byte> readBytes, IEnumerable<byte> furtherBytes, out int offset)
        {
            var bytes = new List<byte>(readBytes);

            bool isHexString = false;
            bool isKnownType = false;
            if (readBytes.Count > 0)
            {
                isHexString = readBytes[0] == '<';

                if (!isHexString && readBytes[0] != '(')
                {
                    throw new InvalidOperationException("String started with an unexpected character: " + bytes[0]);
                }

                isKnownType = true;
            }

            bool isEscapeActive = false;
            int bracketDepth = 0;
            using (var reader = furtherBytes.GetEnumerator())
            {
                while (reader.MoveNext())
                {
                    if (!isKnownType)
                    {
                        isHexString = reader.Current == '<';

                        if (!isHexString && reader.Current != '(')
                        {
                            throw new InvalidOperationException("String started with an unexpected character: " + bytes[0]);
                        }

                        isKnownType = true;
                        bytes.Add(reader.Current);
                        continue;
                    }
                    
                    bytes.Add(reader.Current);

                    if (isHexString)
                    {
                        if (reader.Current == '>')
                        {
                            break;
                        }

                        var isValid = IsValidHexCharacter(reader.Current);

                        if (!isValid)
                        {
                            throw new InvalidOperationException("Found an unexpected character in a hex string: " + reader.Current);
                        }
                    }
                    else
                    {
                        bool exit = false;
                        switch (reader.Current)
                        {
                            case (byte)'\\':
                                isEscapeActive = true;
                                break;
                            case (byte)'(':
                                if (!isEscapeActive)
                                {
                                    bracketDepth++;
                                }

                                break;
                            case (byte)')':
                                if (isEscapeActive)
                                {
                                    continue;
                                }
                                else if (bracketDepth > 0)
                                {
                                    bracketDepth--;
                                }
                                else
                                {
                                    exit = true;
                                }
                                break;
                            default:
                                isEscapeActive = false;
                                break;
                        }

                        if (exit)
                        {
                            break;
                        }
                    }
                }

                if (reader.MoveNext() && !BaseTextComponentApproach.IsEmpty(reader.Current))
                {
                    throw new InvalidOperationException("Unexpected byte following string operator, expected whitespace: " + (char)reader.Current);
                }
            }
            
            offset = bytes.Count;

            return new OperandComponent(new StringOperand(bytes), TextObjectComponentType.String);
        }

        private static bool IsValidHexCharacter(byte b)
        {
            return (b >= '0' && b <= '9')
                   || (b >= 'a' && b <= 'f')
                   || (b >= 'A' && b <= 'F');
        }
    }
}