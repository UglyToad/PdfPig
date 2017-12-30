namespace UglyToad.Pdf.Parser.Parts
{
    using System.IO;
    using System.Text;
    using Cos;
    using IO;
    using Util;

    internal interface IBaseParser
    {
        CosBase Parse(IRandomAccessRead reader, CosObjectPool pool);
    }

    internal class CosBaseParser : IBaseParser
    {
        private readonly CosNameParser nameParser;
        private readonly CosStringParser stringParser;
        private readonly CosDictionaryParser dictionaryParser;
        private readonly CosArrayParser arrayParser;

        public CosBaseParser(CosNameParser nameParser, CosStringParser stringParser, 
            CosDictionaryParser dictionaryParser, CosArrayParser arrayParser)
        {
            this.nameParser = nameParser;
            this.stringParser = stringParser;
            this.dictionaryParser = dictionaryParser;
            this.arrayParser = arrayParser;
        }

        public CosBase Parse(IRandomAccessRead reader, CosObjectPool pool)
        {
            CosBase retval = null;

            ReadHelper.SkipSpaces(reader);
            int nextByte = reader.Peek();

            if (nextByte == -1)
            {
                return null;
            }

            char c = (char)nextByte;
            switch (c)
            {
                case '<':
                    {
                        // pull off first left bracket
                        int leftBracket = reader.Read();
                        // check for second left bracket
                        c = (char)reader.Peek();
                        reader.Unread(leftBracket);
                        if (c == '<')
                        {
                            retval = dictionaryParser.Parse(reader, this, pool);
                            ReadHelper.SkipSpaces(reader);
                        }
                        else
                        {
                            retval = stringParser.Parse(reader);
                        }
                        break;
                    }
                case '[':
                    {
                        // array
                        retval = arrayParser.Parse(reader, this, pool);
                        break;
                    }
                case '(':
                    retval = stringParser.Parse(reader);
                    break;
                case '/':
                    // name
                    retval = nameParser.Parse(reader);
                    break;
                case 'n':
                    {
                        // null
                        ReadHelper.ReadExpectedString(reader, "null");
                        retval = CosNull.Null;
                        break;
                    }
                case 't':
                    {
                        string truestring = OtherEncodings.BytesAsLatin1String(reader.ReadFully(4));
                        if (truestring.Equals("true"))
                        {
                            retval = CosBoolean.True;
                        }
                        else
                        {
                            throw new IOException("expected true actual='" + truestring + "' " + reader +
                            "' at offset " + reader.GetPosition());
                        }
                        break;
                    }
                case 'f':
                    {
                        string falsestring = OtherEncodings.BytesAsLatin1String(reader.ReadFully(5));
                        if (falsestring.Equals("false"))
                        {
                            retval = CosBoolean.False;
                        }
                        else
                        {
                            throw new IOException("expected false actual='" + falsestring + "' " + reader +
                            "' at offset " + reader.GetPosition());
                        }
                        break;
                    }
                case 'R':
                    reader.Read();
                    retval = new CosObject(null);
                    break;
                default:

                    if (char.IsDigit(c) || c == '-' || c == '+' || c == '.')
                    {
                        StringBuilder buf = new StringBuilder();
                        int ic = reader.Read();
                        c = (char)ic;
                        while (char.IsDigit(c) ||
                        c == '-' ||
                        c == '+' ||
                        c == '.' ||
                        c == 'E' ||
                        c == 'e')
                        {
                            buf.Append(c);
                            ic = reader.Read();
                            c = (char)ic;
                        }
                        if (ic != -1)
                        {
                            reader.Unread(ic);
                        }
                        retval = CosNumberFactory.get(buf.ToString()) as CosBase;
                    }
                    else
                    {
                        //This is not suppose to happen, but we will allow for it
                        //so we are more compatible with POS writers that don't
                        //follow the spec
                        string badstring = ReadHelper.ReadString(reader);
                        if (badstring == string.Empty)
                        {
                            int peek = reader.Peek();
                            // we can end up in an infinite loop otherwise
                            throw new IOException("Unknown dir object c='" + c +
                            "' cInt=" + (int)c + " peek='" + (char)peek
                            + "' peekInt=" + peek + " at offset " + reader.GetPosition());
                        }

                        // if it's an endstream/endobj, we want to put it back so the caller will see it
                        if (string.Equals("endobj", badstring) || string.Equals("endstream", badstring))
                        {
                            reader.Unread(OtherEncodings.StringAsLatin1Bytes(badstring));
                        }
                    }
                    break;
            }
            return retval;
        }
    }
}
