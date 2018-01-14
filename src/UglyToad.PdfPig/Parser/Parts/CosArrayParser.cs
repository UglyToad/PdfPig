namespace UglyToad.PdfPig.Parser.Parts
{
    using ContentStream;
    using Cos;
    using IO;
    using Util;

    internal class CosArrayParser
    {
        public COSArray Parse(IRandomAccessRead reader, CosBaseParser baseParser, CosObjectPool pool)
        {
            ReadHelper.ReadExpectedChar(reader, '[');
            var po = new COSArray();
            CosBase pbo;
            ReadHelper.SkipSpaces(reader);
            int i;
            while (((i = reader.Peek()) > 0) && ((char)i != ']'))
            {
                pbo = baseParser.Parse(reader, pool);
                if (pbo is CosObject)
                {
                    // We have to check if the expected values are there or not PDFBOX-385
                    if (po.get(po.size() - 1) is CosInt)
                    {
                        var genNumber = (CosInt)po.remove(po.size() - 1);
                        if (po.get(po.size() - 1) is CosInt)
                        {
                            var number = (CosInt)po.remove(po.size() - 1);
                            IndirectReference key = new IndirectReference(number.AsLong(), genNumber.AsInt());
                            pbo = pool.Get(key);
                        }
                        else
                        {
                            // the object reference is somehow wrong
                            pbo = null;
                        }
                    }
                    else
                    {
                        pbo = null;
                    }
                }
                if (pbo != null)
                {
                    po.add(pbo);
                }
                else
                {
                    //it could be a bad object in the array which is just skipped
                    // LOG.warn("Corrupt object reference at offset " + seqSource.getPosition());

                    // This could also be an "endobj" or "endstream" which means we can assume that
                    // the array has ended.
                    string isThisTheEnd = ReadHelper.ReadString(reader);
                    reader.Unread(OtherEncodings.StringAsLatin1Bytes(isThisTheEnd));
                    if (string.Equals(isThisTheEnd, "endobj") || string.Equals(isThisTheEnd, "endstream"))
                    {
                        return po;
                    }
                }

                ReadHelper.SkipSpaces(reader);
            }
            // read ']'
            reader.Read();
            ReadHelper.SkipSpaces(reader);
            return po;
        }
    }
}

