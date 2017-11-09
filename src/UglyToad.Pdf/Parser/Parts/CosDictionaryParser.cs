namespace UglyToad.Pdf.Parser.Parts
{
    using System;
    using System.Collections.Generic;
    using ContentStream;
    using Cos;
    using IO;
    using Logging;
    using Util;
    using Util.JetBrains.Annotations;

    internal class CosDictionaryParser
    {
        private readonly ILog log;
        private readonly CosNameParser nameParser;

        protected static readonly int E = 'e';
        protected static readonly int N = 'n';
        protected static readonly int D = 'd';

        protected static readonly int S = 's';
        protected static readonly int T = 't';
        protected static readonly int R = 'r';
        protected static readonly int A = 'a';
        protected static readonly int M = 'm';

        protected static readonly int O = 'o';
        protected static readonly int B = 'b';
        protected static readonly int J = 'j';

        public CosDictionaryParser(CosNameParser nameParser, ILog log)
        {
            this.log = log;
            this.nameParser = nameParser ?? throw new ArgumentNullException();
        }

        public ContentStreamDictionary Parse(IRandomAccessRead reader, CosBaseParser baseParser, CosObjectPool pool)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            if (baseParser == null)
            {
                throw new ArgumentNullException(nameof(baseParser));
            }

            if (pool == null)
            {
                throw new ArgumentNullException(nameof(pool));
            }

            ReadHelper.ReadExpectedChar(reader, '<');
            ReadHelper.ReadExpectedChar(reader, '<');
            ReadHelper.SkipSpaces(reader);
            
            var dictionary = new ContentStreamDictionary();

            var done = false;
            while (!done)
            {
                ReadHelper.SkipSpaces(reader);

                var c = (char)reader.Peek();

                switch (c)
                {
                    case '>':
                        done = true;
                        break;
                    case '/':
                        var nameValue = ParseCosDictionaryNameValuePair(reader, baseParser, pool);

                        if (nameValue.key != null && nameValue.value != null)
                        {
                            dictionary.Set(nameValue.key, nameValue.value);
                        }

                        break;
                    default:
                        if (ReadUntilEnd(reader))
                        {
                            return new ContentStreamDictionary();
                        }
                        break;
                }
            }

            ReadHelper.ReadExpectedString(reader, ">>");
            
            return dictionary;
        }

        [ItemCanBeNull]
        private (CosName key, CosBase value) ParseCosDictionaryNameValuePair(IRandomAccessRead reader, CosBaseParser baseParser, CosObjectPool pool)
        {
            var key = nameParser.Parse(reader);
            var value = ParseValue(reader, baseParser, pool);
            ReadHelper.SkipSpaces(reader);

            if ((char)reader.Peek() == 'd')
            {
                // if the next string is 'def' then we are parsing a cmap stream
                // and want to ignore it, otherwise throw an exception.
                var potentialDef = ReadHelper.ReadString(reader);
                if (!potentialDef.Equals("def"))
                {
                    reader.Unread(OtherEncodings.StringAsLatin1Bytes(potentialDef));
                }
                else
                {
                    ReadHelper.SkipSpaces(reader);
                }
            }

            if (value == null)
            {
                log?.Warn("Bad Dictionary Declaration " + ReadHelper.ReadString(reader));
                return (null, null);
            }
            
            // label this item as direct, to avoid signature problems.
            value.Direct = true;

            return (key, value);
        }
        
        private static CosBase ParseValue(IRandomAccessRead reader, CosBaseParser baseParser, CosObjectPool pool)
        {
            var numOffset = reader.GetPosition();
            var value = baseParser.Parse(reader, pool);

            ReadHelper.SkipSpaces(reader);

            // proceed if the given object is a number and the following is a number as well
            if (!(value is ICosNumber) || !ReadHelper.IsDigit(reader))
            {
                return value;
            }
            // read the remaining information of the object number
            var genOffset = reader.GetPosition();
            var generationNumber = baseParser.Parse(reader, pool);
            ReadHelper.SkipSpaces(reader);
            ReadHelper.ReadExpectedChar(reader, 'R');
            if (!(value is CosInt))
            {
                throw new InvalidOperationException("expected number, actual=" + value + " at offset " + numOffset);
            }
            if (!(generationNumber is CosInt))
            {
                throw new InvalidOperationException("expected number, actual=" + value + " at offset " + genOffset);
            }

            var key = new CosObjectKey(((CosInt)value).AsLong(), ((CosInt)generationNumber).AsInt());

            // dereference the object
            return pool.Get(key);
        }

        private static bool ReadUntilEnd(IRandomAccessRead reader)
        {
            var c = reader.Read();
            while (c != -1 && c != '/' && c != '>')
            {
                // in addition to stopping when we find / or >, we also want
                // to stop when we find endstream or endobj.
                if (c == E)
                {
                    c = reader.Read();
                    if (c == N)
                    {
                        c = reader.Read();
                        if (c == D)
                        {
                            c = reader.Read();
                            var isStream = c == S && reader.Read() == T && reader.Read() == R
                                           && reader.Read() == E && reader.Read() == A && reader.Read() == M;
                            var isObj = !isStream && c == O && reader.Read() == B && reader.Read() == J;
                            if (isStream || isObj)
                            {
                                // we're done reading this object!
                                return true;
                            }
                        }
                    }
                }
                c = reader.Read();
            }
            if (c == -1)
            {
                return true;
            }
            reader.Unread(c);
            return false;
        }
    }
}



