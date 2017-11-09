using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.Pdf.Cos;

namespace UglyToad.Pdf.Parser
{
    using System.Collections;
    using System.Linq;
    using System.Text.RegularExpressions;
    using ContentStream;
    using Cos;
    using IO;
    using Parts;
    using Util;

    /**
     * PDF-Parser which first reads startxref and xref tables in order to know valid objects and parse only these objects.
     * 
     * First {@link PDFParser#parse()} or  {@link FDFParser#parse()} must be called before page objects
     * can be retrieved, e.g. {@link PDFParser#getPDDocument()}.
     * 
     * This class is a much enhanced version of <code>QuickParser</code> presented in <a
     * href="https://issues.apache.org/jira/browse/PDFBOX-1104">PDFBOX-1104</a> by Jeremy Villalobos.
     */
    internal class COSParser : BaseParser
    {
        private static readonly String PDF_HEADER = "%PDF-";
        private static readonly String FDF_HEADER = "%FDF-";

        private static readonly String PDF_DEFAULT_VERSION = "1.4";
        private static readonly String FDF_DEFAULT_VERSION = "1.0";

        private static readonly char[] XREF_TABLE = new char[] { 'x', 'r', 'e', 'f' };
        private static readonly char[] XREF_STREAM = new char[] { '/', 'X', 'R', 'e', 'f' };
        private static readonly char[] STARTXREF = new char[] { 's', 't', 'a', 'r', 't', 'x', 'r', 'e', 'f' };

        private static readonly byte[] ENDSTREAM = new byte[]
        {
            (byte) 'E', (byte) 'N', (byte) 'D',
            (byte) 'S', (byte) 'T', (byte) 'R', (byte) 'E', (byte) 'A', (byte) 'M'
        };

        private static readonly byte[] ENDOBJ = new byte[]
        {
            (byte) 'E', (byte) 'N', (byte) 'D',
            (byte) 'O', (byte) 'B', (byte) 'J'
        };

        private static readonly long MINIMUM_SEARCH_OFFSET = 6;

        private static readonly int X = 'x';

        private static readonly int STRMBUFLEN = 2048;
        private readonly byte[] strmBuf = new byte[STRMBUFLEN];

        protected readonly IRandomAccessRead source;

        /**
         * Only parse the PDF file minimally allowing access to basic information.
         */
        public static readonly String SYSPROP_PARSEMINIMAL =
                "org.apache.pdfbox.pdfparser.nonSequentialPDFParser.parseMinimal";

        /**
         * The range within the %%EOF marker will be searched.
         * Useful if there are additional characters after %%EOF within the PDF. 
         */
        public static readonly String SYSPROP_EOFLOOKUPRANGE =
                "org.apache.pdfbox.pdfparser.nonSequentialPDFParser.eofLookupRange";

        /**
         * How many trailing bytes to read for EOF marker.
         */
        private static readonly int DEFAULT_TRAIL_BYTECOUNT = 2048;
        /**
         * EOF-marker.
         */
        protected static readonly char[] EOF_MARKER = new char[] { '%', '%', 'E', 'O', 'F' };
        /**
         * obj-marker.
         */
        protected static readonly char[] OBJ_MARKER = new char[] { 'o', 'b', 'j' };

        private long trailerOffset;

        /**
         * file length.
         */
        protected long fileLen;

        /**
         * is parser using auto healing capacity ?
         */
        private bool isLenient = true;

        protected bool initialParseDone = false;
        /**
         * Contains all found objects of a brute force search.
         */
        private Dictionary<CosObjectKey, long> bfSearchCOSObjectKeyOffsets = null;
        private long? lastEOFMarker = null;
        private List<long> bfSearchXRefTablesOffsets = null;
        private List<long> bfSearchXRefStreamsOffsets = null;

        /**
         * The security handler.
         */
        //protected SecurityHandler securityHandler = null;

        /**
         *  how many trailing bytes to read for EOF marker.
         */
        private int readTrailBytes = DEFAULT_TRAIL_BYTECOUNT;

        /** 
         * Collects all Xref/trailer objects and resolves them into single
         * object using startxref reference. 
         */
        protected XrefTrailerResolver xrefTrailerResolver = new XrefTrailerResolver();

        /**
         * The prefix for the temp file being used. 
         */
        public static readonly String TMP_FILE_PREFIX = "tmpPDF";

        /**
         * Default constructor.
         */
        public COSParser(IRandomAccessRead source) : base(new BufferSequentialSource(source))
        {
            this.source = source;
        }

        /**
         * Sets how many trailing bytes of PDF file are searched for EOF marker and 'startxref' marker. If not set we use
         * default value {@link #DEFAULT_TRAIL_BYTECOUNT}.
         * 
         * <p>We check that new value is at least 16. However for practical use cases this value should not be lower than
         * 1000; even 2000 was found to not be enough in some cases where some trailing garbage like HTML snippets followed
         * the EOF marker.</p>
         * 
         * <p>
         * In case system property {@link #SYSPROP_EOFLOOKUPRANGE} is defined this value will be set on initialization but
         * can be overwritten later.
         * </p>
         * 
         * @param byteCount number of trailing bytes
         */
        public void setEOFLookupRange(int byteCount)
        {
            if (byteCount > 15)
            {
                readTrailBytes = byteCount;
            }
        }
        
        /**
         * Searches last appearance of pattern within buffer. Lookup before _lastOff and goes back until 0.
         * 
         * @param pattern pattern to search for
         * @param buf buffer to search pattern in
         * @param endOff offset (exclusive) where lookup starts at
         * 
         * @return start offset of pattern within buffer or <code>-1</code> if pattern could not be found
         */
        protected int lastIndexOf(char[] pattern, byte[] buf, int endOff)
        {
            int lastPatternChOff = pattern.Length - 1;

            int bufOff = endOff;
            int patOff = lastPatternChOff;
            char lookupCh = pattern[patOff];

            while (--bufOff >= 0)
            {
                if (buf[bufOff] == lookupCh)
                {
                    if (--patOff < 0)
                    {
                        // whole pattern matched
                        return bufOff;
                    }
                    // matched current char, advance to preceding one
                    lookupCh = pattern[patOff];
                }
                else if (patOff < lastPatternChOff)
                {
                    // no char match but already matched some chars; reset
                    patOff = lastPatternChOff;
                    lookupCh = pattern[patOff];
                }
            }
            return -1;
        }

        /**
         * Return true if parser is lenient. Meaning auto healing capacity of the parser are used.
         *
         * @return true if parser is lenient
         */
        public bool getIsLenient()
        {
            return isLenient;
        }

        /**
         * Change the parser leniency flag.
         *
         * This method can only be called before the parsing of the file.
         *
         * @param lenient try to handle malformed PDFs.
         *
         */
        public void setLenient(bool lenient)
        {
            if (initialParseDone)
            {
                throw new ArgumentException("Cannot change leniency after parsing");
            }
            this.isLenient = lenient;
        }

        /**
         * Creates a unique object id using object number and object generation
         * number. (requires object number &lt; 2^31))
         */
        private long getObjectId(CosObject obj)
        {
            return obj.GetObjectNumber() << 32 | obj.GetGenerationNumber();
        }

        /**
         * Adds all from newObjects to toBeParsedList if it is not an CosObject or
         * we didn't add this CosObject already (checked via addedObjects).
         */
        private void addNewToList(Queue<CosBase> toBeParsedList, IReadOnlyCollection<CosBase> newObjects, HashSet<long> addedObjects)
        {
            foreach (CosBase newOibject in newObjects)
            {
                addNewToList(toBeParsedList, newOibject, addedObjects);
            }
        }

        /**
         * Adds newObject to toBeParsedList if it is not an CosObject or we didn't
         * add this CosObject already (checked via addedObjects).
         */
        private void addNewToList(Queue<CosBase> toBeParsedList, CosBase newObject, HashSet<long> addedObjects)
        {
            if (newObject is CosObject)
            {
                long objId = getObjectId((CosObject)newObject);
                if (!addedObjects.Add(objId))
                {
                    return;
                }
            }
            toBeParsedList.Enqueue(newObject);
        }

        private static T? TryGet<T, TKey>(TKey key, IReadOnlyDictionary<TKey, T> dictionary) where T : struct
        {
            return dictionary.TryGetValue(key, out var value) ? value : default(T?);
        }

        /**
         * Will parse every object necessary to load a single page from the pdf document. We try our
         * best to order objects according to offset in file before reading to minimize seek operations.
         *
         * @param dict the CosObject from the parent pages.
         * @param excludeObjects dictionary object reference entries with these names will not be parsed
         *
         * @throws InvalidOperationException if something went wrong
         */
        protected void parseDictObjects(CosDictionary dict, CosName[] excludeObjects, BruteForceSearcher searcher, 
            CosBaseParser baseParser,
            CosStreamParser streamParser,
            IRandomAccessRead reader,
            COSDocument document,
            bool isLenientParsing,
            CosObjectPool pool)
        {
            // ---- create queue for objects waiting for further parsing
            Queue<CosBase> toBeParsedList = new Queue<CosBase>();
            // offset ordered object map
            Dictionary<long, List<CosObject>> objToBeParsed = new Dictionary<long, List<CosObject>>();
            // in case of compressed objects offset points to stmObj
            HashSet<long> parsedObjects = new HashSet<long>();
            HashSet<long> addedObjects = new HashSet<long>();

            addExcludedToList(excludeObjects, dict, parsedObjects);
            addNewToList(toBeParsedList, dict.getValues(), addedObjects);

            // ---- go through objects to be parsed
            while (!(toBeParsedList.Count == 0 && objToBeParsed.Count == 0))
            {
                // -- first get all CosObject from other kind of objects and
                // put them in objToBeParsed; afterwards toBeParsedList is empty
                CosBase baseObj;
                while (toBeParsedList.Count > 0 && (baseObj = toBeParsedList.Dequeue()) != null)
                {
                    if (baseObj is CosDictionary)
                    {
                        addNewToList(toBeParsedList, ((CosDictionary)baseObj).getValues(), addedObjects);
                    }
                    else if (baseObj is COSArray)
                    {
                        foreach (CosBase CosBase in ((COSArray)baseObj))
                        {
                            addNewToList(toBeParsedList, CosBase, addedObjects);
                        }
                    }
                    else if (baseObj is CosObject)
                    {
                        CosObject obj = (CosObject)baseObj;
                        long objId = getObjectId(obj);
                        CosObjectKey objKey = new CosObjectKey(obj.GetObjectNumber(), obj.GetGenerationNumber());

                        if (!parsedObjects.Contains(objId))
                        {
                            long? fileOffset = TryGet(objKey, document.getXrefTable());
                            if (fileOffset == null && isLenient)
                            {
                                IReadOnlyDictionary<CosObjectKey, long> bfCOSObjectKeyOffsets = searcher.GetObjectLocations();
                                fileOffset = TryGet(objKey, bfCOSObjectKeyOffsets);
                                if (fileOffset != null)
                                {
                                    document.getXrefTable().Add(objKey, fileOffset.Value);
                                }
                            }

                            // it is allowed that object references point to null, thus we have to test
                            if (fileOffset != null && fileOffset != 0)
                            {
                                if (fileOffset > 0)
                                {
                                    objToBeParsed[fileOffset.Value] = new List<CosObject> {obj};
                                }
                                else
                                {
                                    // negative offset means we have a compressed
                                    // object within object stream;
                                    // get offset of object stream
                                    fileOffset = TryGet(new CosObjectKey((int)-fileOffset, 0), document.getXrefTable());

                                    if ((fileOffset == null) || (fileOffset <= 0))
                                    {
                                        throw new InvalidOperationException(
                                                "Invalid object stream xref object reference for key '" + objKey + "': "
                                                        + fileOffset);
                                    }
                                    
                                    if (!objToBeParsed.TryGetValue(fileOffset.Value, out List<CosObject> stmObjects))
                                    {
                                        stmObjects = new List<CosObject>();
                                        objToBeParsed.Add(fileOffset.Value, stmObjects);
                                    }
                                    // java does not have a test for immutable
                                    else if (!(stmObjects is ArrayList))
                                    {
                                        throw new InvalidOperationException(obj + " cannot be assigned to offset " +
                                                fileOffset + ", this belongs to " + stmObjects[0]);
                                    }
                                    stmObjects.Add(obj);
                                }
                            }
                            else
                            {
                                // NULL object
                                CosObject pdfObject = document.getObjectFromPool(objKey);
                                pdfObject.SetObject(CosNull.Null);
                            }
                        }
                    }
                }

                // ---- read first CosObject with smallest offset
                // resulting object will be added to toBeParsedList
                if (objToBeParsed.Count == 0)
                {
                    break;
                }

                var single = objToBeParsed.First();
                objToBeParsed.Remove(single.Key);

                foreach (CosObject obj in single.Value)
                {
                    CosBase parsedObj = parseObjectDynamically(obj, false, searcher, baseParser, streamParser, reader, isLenientParsing, document, pool);
                    if (parsedObj != null)
                    {
                        obj.SetObject(parsedObj);
                        addNewToList(toBeParsedList, parsedObj, addedObjects);
                        parsedObjects.Add(getObjectId(obj));
                    }
                }
            }
        }

        // add objects not to be parsed to list of already parsed objects
        private void addExcludedToList(CosName[] excludeObjects, CosDictionary dict, ISet<long> parsedObjects)
        {
            if (excludeObjects != null)
            {
                foreach (CosName objName in excludeObjects)
                {
                    CosBase baseObj = dict.getItem(objName);
                    if (baseObj is CosObject)
                    {
                        parsedObjects.Add(getObjectId((CosObject)baseObj));
                    }
                }
            }
        }

        /**
         * This will parse the next object from the stream and add it to the local state. 
         * 
         * @param obj object to be parsed (we only take object number and generation number for lookup start offset)
         * @param requireExistingNotCompressedObj if <code>true</code> object to be parsed must not be contained within
         * compressed stream
         * @return the parsed object (which is also added to document object)
         * 
         * @throws InvalidOperationException If an IO error occurs.
         */
        protected static CosBase parseObjectDynamically(CosObject obj,
                bool requireExistingNotCompressedObj,
                BruteForceSearcher searcher,
                CosBaseParser baseParser,
                CosStreamParser streamParser,
                IRandomAccessRead reader,
                bool isLenient,
                COSDocument document,
                CosObjectPool pool)
        {
            return parseObjectDynamically(obj.GetObjectNumber(),
                    obj.GetGenerationNumber(), requireExistingNotCompressedObj, searcher, baseParser, reader, document, isLenient, streamParser, pool);
        }

        /**
         * This will parse the next object from the stream and add it to the local state. 
         * It's reduced to parsing an indirect object.
         * 
         * @param objNr object number of object to be parsed
         * @param objGenNr object generation number of object to be parsed
         * @param requireExistingNotCompressedObj if <code>true</code> the object to be parsed must be defined in xref
         * (comment: null objects may be missing from xref) and it must not be a compressed object within object stream
         * (this is used to circumvent being stuck in a loop in a malicious PDF)
         * 
         * @return the parsed object (which is also added to document object)
         * 
         * @throws InvalidOperationException If an IO error occurs.
         */
        protected static CosBase parseObjectDynamically(long objNr, int objGenNr,
                bool requireExistingNotCompressedObj, BruteForceSearcher searcher, 
                CosBaseParser baseParser,
                IRandomAccessRead reader,
                COSDocument document,
                bool isLenientParsing,
                CosStreamParser streamParser,
                CosObjectPool pool)
        {
            // ---- create object key and get object (container) from pool
            CosObjectKey objKey = new CosObjectKey(objNr, objGenNr);
            CosObject pdfObject = document.getObjectFromPool(objKey);

            if (pdfObject.GetObject() == null)
            {
                // not previously parsed
                // ---- read offset or object stream object number from xref table
                long? offsetOrObjstmObNr = TryGet(objKey, document.getXrefTable());

                // sanity test to circumvent loops with broken documents
                if (requireExistingNotCompressedObj
                        && ((offsetOrObjstmObNr == null) || (offsetOrObjstmObNr <= 0)))
                {
                    throw new InvalidOperationException("Object must be defined and must not be compressed object: "
                            + objKey.Number + ":" + objKey.Generation);
                }

                // maybe something is wrong with the xref table -> perform brute force search for all objects
                if (offsetOrObjstmObNr == null && isLenientParsing)
                {
                    IReadOnlyDictionary<CosObjectKey, long> bfCOSObjectKeyOffsets = searcher.GetObjectLocations();
                    offsetOrObjstmObNr = TryGet(objKey, bfCOSObjectKeyOffsets);
                    if (offsetOrObjstmObNr != null)
                    {
                        document.getXrefTable().Add(objKey, offsetOrObjstmObNr.Value);
                    }
                }

                if (offsetOrObjstmObNr == null)
                {
                    // not defined object -> NULL object (Spec. 1.7, chap. 3.2.9)
                    pdfObject.SetObject(CosNull.Null);
                }
                else if (offsetOrObjstmObNr > 0)
                {
                    // offset of indirect object in file
                    parseFileObject(offsetOrObjstmObNr.Value, objKey, pdfObject, searcher, baseParser, pool, reader, isLenientParsing, streamParser);
                }
                else
                {
                    // xref value is object nr of object stream containing object to be parsed
                    // since our object was not found it means object stream was not parsed so far
                    parseObjectStream((int)-offsetOrObjstmObNr, searcher, baseParser, document, reader, isLenientParsing, streamParser, pool);
                }
            }
            return pdfObject.GetObject();
        }

        private static void parseFileObject(long offsetOrObjstmObNr, CosObjectKey objKey, CosObject pdfObject, 
            BruteForceSearcher searcher, CosBaseParser baseParser,
            CosObjectPool pool,
            IRandomAccessRead source,
            bool isLenient,
            CosStreamParser streamParser)
        {
            // ---- go to object start
            source.Seek(offsetOrObjstmObNr);

            // ---- we must have an indirect object
            long readObjNr = ObjectHelper.ReadObjectNumber(source);
            int readObjGen = ObjectHelper.ReadGenerationNumber(source);
            ReadHelper.ReadExpectedString(source, new string(OBJ_MARKER), true);

            // ---- consistency check
            if ((readObjNr != objKey.Number) || (readObjGen != objKey.Generation))
            {
                throw new InvalidOperationException("XREF for " + objKey.Number + ":"
                        + objKey.Generation + " points to wrong object: " + readObjNr
                        + ":" + readObjGen + " at offset " + offsetOrObjstmObNr);
            }

            ReadHelper.SkipSpaces(source);
            CosBase pb = baseParser.Parse(source, pool);
            string endObjectKey =  ReadHelper.ReadString(source);

            if (endObjectKey.Equals(STREAM_string))
            {
                source.Rewind(OtherEncodings.StringAsLatin1Bytes(endObjectKey).Length);
                if (pb is ContentStreamDictionary dict)
                {
                    RawCosStream stream = streamParser.Parse(source, dict, isLenient);

                    pb = stream;
                }
                else
                {
                    // this is not legal
                    // the combination of a dict and the stream/endstream
                    // forms a complete stream object
                    throw new InvalidOperationException("Stream not preceded by dictionary (offset: "
                            + offsetOrObjstmObNr + ").");
                }

                ReadHelper.SkipSpaces(source);
                endObjectKey = ReadHelper.ReadLine(source);

                // we have case with a second 'endstream' before endobj
                if (!endObjectKey.StartsWith(ENDOBJ_string) && endObjectKey.StartsWith(ENDSTREAM_string))
                {
                    endObjectKey = endObjectKey.Substring(9).Trim();
                    if (endObjectKey.Length == 0)
                    {
                        // no other characters in extra endstream line
                        // read next line
                        endObjectKey = ReadHelper.ReadLine(source);
                    }
                }
            }

            pdfObject.SetObject(pb);

            if (!endObjectKey.StartsWith(ENDOBJ_string))
            {
                if (isLenient)
                {
                    //LOG.warn("Object (" + readObjNr + ":" + readObjGen + ") at offset "
                    //        + offsetOrObjstmObNr + " does not end with 'endobj' but with '"
                    //        + endObjectKey + "'");
                }
                else
                {
                    throw new InvalidOperationException("Object (" + readObjNr + ":" + readObjGen
                            + ") at offset " + offsetOrObjstmObNr
                            + " does not end with 'endobj' but with '" + endObjectKey + "'");
                }
            }
        }

        private static void parseObjectStream(int objstmObjNr, BruteForceSearcher searcher, CosBaseParser baseParser, COSDocument document, 
            IRandomAccessRead reader, bool isLenient, CosStreamParser streamParser,
            CosObjectPool pool)
        {
            CosBase objstmBaseObj = parseObjectDynamically(objstmObjNr, 0, true, searcher, baseParser, reader, document, isLenient, streamParser, pool);
            if (objstmBaseObj is COSStream)
            {
                // parse object stream
                PDFObjectStreamParser parser;
                try
                {
                    parser = new PDFObjectStreamParser((COSStream)objstmBaseObj, document);
                }
                catch (InvalidOperationException ex)
                {
                    if (isLenient)
                    {
                        // LOG.error("object stream " + objstmObjNr + " could not be parsed due to an exception", ex);
                        return;
                    }
                    else
                    {
                        throw ex;
                    }
                }

                try
                {
                    parser.parse(baseParser, pool);
                }
                catch (InvalidOperationException exception)
                {
                    if (isLenient)
                    {
                        //  LOG.debug("Stop reading object stream " + objstmObjNr + " due to an exception", exception);
                        // the error is handled in parseDictObjects
                        return;
                    }
                    else
                    {
                        throw exception;
                    }
                }
                // register all objects which are referenced to be contained in object stream
                foreach (CosObject next in parser.getObjects())
                {
                    CosObjectKey stmObjKey = new CosObjectKey(next);
                    long? offset = TryGet(stmObjKey, document.getXrefTable());
                    if (offset != null && offset == -objstmObjNr)
                    {
                        CosObject stmObj = document.getObjectFromPool(stmObjKey);
                        stmObj.SetObject(next.GetObject());
                    }
                }
            }
        }
        
        private static readonly int STREAMCOPYBUFLEN = 8192;
        private readonly byte[] streamCopyBuf = new byte[STREAMCOPYBUFLEN];
        

        /**
         * This method will read through the current stream object until
         * we find the keyword "endstream" meaning we're at the end of this
         * object. Some pdf files, however, forget to write some endstream tags
         * and just close off objects with an "endobj" tag so we have to handle
         * this case as well.
         * 
         * This method is optimized using buffered IO and reduced number of
         * byte compare operations.
         * 
         * @param out  stream we write out to.
         * 
         * @throws InvalidOperationException if something went wrong
         */
        private void readUntilEndStream(IOutputStream output)
        {
            int bufSize;
            int charMatchCount = 0;
            byte[]
        keyw = ENDSTREAM;

            // last character position of shortest keyword ('endobj')
            int quickTestOffset = 5;

            // read next chunk into buffer; already matched chars are added to beginning of buffer
            while ((bufSize = source.Read(strmBuf, charMatchCount, STRMBUFLEN - charMatchCount)) > 0)
            {
                bufSize += charMatchCount;

                int bIdx = charMatchCount;
                int quickTestIdx;

                // iterate over buffer, trying to find keyword match
                for (int maxQuicktestIdx = bufSize - quickTestOffset; bIdx < bufSize; bIdx++)
                {
                    // reduce compare operations by first test last character we would have to
                    // match if current one matches; if it is not a character from keywords
                    // we can move behind the test character; this shortcut is inspired by the 
                    // Boyer-Moore string search algorithm and can reduce parsing time by approx. 20%
                    quickTestIdx = bIdx + quickTestOffset;
                    if (charMatchCount == 0 && quickTestIdx < maxQuicktestIdx)
                    {
                        byte ch = strmBuf[quickTestIdx];
                        if ((ch > 't') || (ch < 'a'))
                        {
                            // last character we would have to match if current character would match
                            // is not a character from keywords -> jump behind and start over
                            bIdx = quickTestIdx;
                            continue;
                        }
                    }

                    // could be negative - but we only compare to ASCII
                    byte ch1 = strmBuf[bIdx];

                    if (ch1 == keyw[charMatchCount])
                    {
                        if (++charMatchCount == keyw.Length)
                        {
                            // match found
                            bIdx++;
                            break;
                        }
                    }
                    else
                    {
                        if ((charMatchCount == 3) && (ch1 == ENDOBJ[charMatchCount]))
                        {
                            // maybe ENDSTREAM is missing but we could have ENDOBJ
                            keyw = ENDOBJ;
                            charMatchCount++;
                        }
                        else
                        {
                            // no match; incrementing match start by 1 would be dumb since we already know 
                            // matched chars depending on current char read we may already have beginning 
                            // of a new match: 'e': first char matched; 'n': if we are at match position 
                            // idx 7 we already read 'e' thus 2 chars matched for each other char we have 
                            // to start matching first keyword char beginning with next read position
                            charMatchCount = (ch1 == E) ? 1 : ((ch1 == N) && (charMatchCount == 7)) ? 2 : 0;
                            // search again for 'endstream'
                            keyw = ENDSTREAM;
                        }
                    }
                }

                int contentBytes = Math.Max(0, bIdx - charMatchCount);

                // write buffer content until first matched char to output stream
                if (contentBytes > 0)
                {
                    output.write(strmBuf, 0, contentBytes);
                }
                if (charMatchCount == keyw.Length)
                {
                    // keyword matched; unread matched keyword (endstream/endobj) and following buffered content
                    source.Rewind(bufSize - contentBytes);
                    break;
                }
                else
                {
                    // copy matched chars at start of buffer
                    Array.Copy(keyw, 0, strmBuf, 0, charMatchCount);
                }
            }
            // this writes a lonely CR or drops trailing CR LF and LF
            output.flush();
        }

        private void readValidStream(IOutputStream output, ICosNumber streamLengthObj)
        {
            long remainBytes = streamLengthObj.AsLong();
            while (remainBytes > 0)
            {
                int chunk = (remainBytes > STREAMCOPYBUFLEN) ? STREAMCOPYBUFLEN : (int)remainBytes;
                int readBytes = source.Read(streamCopyBuf, 0, chunk);
                if (readBytes <= 0)
                {
                    // shouldn't happen, the stream length has already been validated
                    throw new InvalidOperationException("read error at offset " + source.GetPosition()
                            + ": expected " + chunk + " bytes, but read() returns " + readBytes);
                }
                output.write(streamCopyBuf, 0, readBytes);
                remainBytes -= readBytes;
            }
        }

        private bool validateStreamLength(long streamLength)
        {
            bool streamLengthIsValid = true;
            long originOffset = source.GetPosition();
            long expectedEndOfStream = originOffset + streamLength;
            if (expectedEndOfStream > fileLen)
            {
                streamLengthIsValid = false;
                //LOG.warn("The end of the stream is out of range, using workaround to read the stream, "
                //        + "stream start position: " + originOffset + ", length: " + streamLength
                //        + ", expected end position: " + expectedEndOfStream);
            }
            else
            {
                source.Seek(expectedEndOfStream);
                SkipSpaces();
                if (!isString(ENDSTREAM))
                {
                    streamLengthIsValid = false;
                    //LOG.warn("The end of the stream doesn't point to the correct offset, using workaround to read the stream, "
                    //        + "stream start position: " + originOffset + ", length: " + streamLength
                    //        + ", expected end position: " + expectedEndOfStream);
                }
                source.Seek(originOffset);
            }
            return streamLengthIsValid;
        }
        
        /**
         * Try to find a fixed offset for the given xref table/stream.
         * 
         * @param objectOffset the given offset where to look at
         * @return the fixed offset
         * 
         * @throws InvalidOperationException if something went wrong
         */
        private long calculateXRefFixedOffset(long objectOffset)
        {
            if (objectOffset < 0)
            {
                // LOG.error("Invalid object offset " + objectOffset + " when searching for a xref table/stream");
                return 0;
            }
            // start a brute force search for all xref tables and try to find the offset we are looking for
            long newOffset = bfSearchForXRef(objectOffset);
            if (newOffset > -1)
            {
                // LOG.debug("Fixed reference for xref table/stream " + objectOffset + " -> " + newOffset);
                return newOffset;
            }
            // LOG.error("Can't find the object xref table/stream at offset " + objectOffset);
            return 0;
        }

        private bool validateXrefOffsets(Dictionary<CosObjectKey, long> xrefOffset)
        {
            if (xrefOffset == null)
            {
                return true;
            }
            foreach (var objectEntry in xrefOffset)
            {
                CosObjectKey objectKey = objectEntry.Key;
                long objectOffset = objectEntry.Value;
                // a negative offset number represents a object number itself
                // see type 2 entry in xref stream
                if (objectOffset != null && objectOffset >= 0
                        && !checkObjectKeys(objectKey, objectOffset))
                {
                    //LOG.debug("Stop checking xref offsets as at least one (" + objectKey
                    //        + ") couldn't be dereferenced");
                    return false;
                }
            }
            return true;
        }

        /**
         * Check the XRef table by dereferencing all objects and fixing the offset if necessary.
         * 
         * @throws InvalidOperationException if something went wrong.
         */
        private void checkXrefOffsets()
        {
            // repair mode isn't available in non-lenient mode
            if (!isLenient)
            {
                return;
            }
            Dictionary<CosObjectKey, long> xrefOffset = xrefTrailerResolver.getXrefTable();
            if (!validateXrefOffsets(xrefOffset))
            {
                Dictionary<CosObjectKey, long> bfCOSObjectKeyOffsets = getBFCosObjectOffsets();
                if (bfCOSObjectKeyOffsets.Count > 0)
        {
                    List<CosObjectKey> objStreams = new List<CosObjectKey>();
                    // find all object streams
                    foreach (var entry in xrefOffset)
                    {
                        long offset = entry.Value;
                        if (offset != null && offset < 0)
                        {
                            CosObjectKey objStream = new CosObjectKey(-offset, 0);
                            if (!objStreams.Contains(objStream))
                            {
                                objStreams.Add(new CosObjectKey(-offset, 0));
                            }
                        }
                    }
                    // remove all found object streams
                    if (objStreams.Count > 0)
                    {
                        foreach (CosObjectKey key in objStreams)
                        {
                            if (bfCOSObjectKeyOffsets.ContainsKey(key))
                            {
                                // remove all parsed objects which are part of an object stream
                                ISet<long> objects = xrefTrailerResolver
                                        .getContainedObjectNumbers((int)(key.Number));
                                foreach (long objNr in objects)
                                {
                                    CosObjectKey streamObjectKey = new CosObjectKey(objNr, 0);
                                  
                                    if (bfCOSObjectKeyOffsets.TryGetValue(streamObjectKey, out long streamObjectOffset) && streamObjectOffset > 0)
                                    {
                                        bfCOSObjectKeyOffsets.Remove(streamObjectKey);
                                    }
                                }
                            }
                            else
                            {
                                // remove all objects which are part of an object stream which wasn't found
                                ISet<long> objects = xrefTrailerResolver
                                        .getContainedObjectNumbers((int)(key.Number));
                                foreach (long objNr in objects)
                                {
                                    xrefOffset.Remove(new CosObjectKey(objNr, 0));
                                }
                            }
                        }
                    }

            foreach (var item in bfCOSObjectKeyOffsets)
            {
                xrefOffset.Add(item.Key, item.Value);
            }
            
                }
            }
        }

        /**
         * Check if the given object can be found at the given offset.
         * 
         * @param objectKey the object we are looking for
         * @param offset the offset where to look
         * @return returns true if the given object can be dereferenced at the given offset
         * @throws InvalidOperationException if something went wrong
         */
        private bool checkObjectKeys(CosObjectKey objectKey, long offset)
        {
            // there can't be any object at the very beginning of a pdf
            if (offset < MINIMUM_SEARCH_OFFSET)
            {
                return false;
            }
            long objectNr = objectKey.Number;
            long objectGen = objectKey.Generation;
            long originOffset = source.GetPosition();
            String objectString = createObjectString(objectNr, objectGen);
            try
            {
                source.Seek(offset);
                if (isString(OtherEncodings.StringAsLatin1Bytes(objectString)))
                {
                    // everything is ok, return origin object key
                    source.Seek(originOffset);
                    return true;
                }
            }
            catch (InvalidOperationException exception)
            {
                // Swallow the exception, obviously there isn't any valid object number
            }
            finally 
                {
                source.Seek(originOffset);
            }
            // no valid object number found
            return false;
        }
        /**
         * Create a string for the given object id.
         * 
         * @param objectID the object id
         * @param genID the generation id
         * @return the generated string
         */
        private String createObjectString(long objectID, long genID)
        {
            return $"{objectID} {genID} obj";
        }

        private Dictionary<CosObjectKey, long> getBFCosObjectOffsets()
        {
            if (bfSearchCOSObjectKeyOffsets == null)
            {
                bfSearchForObjects();
            }
            return bfSearchCOSObjectKeyOffsets;
        }

        /**
         * Brute force search for every object in the pdf.
         *   
         * @throws InvalidOperationException if something went wrong
         */
        private void bfSearchForObjects()
        {
            bfSearchForLastEOFMarker();
            bfSearchCOSObjectKeyOffsets = new Dictionary<CosObjectKey, long>();
            long originOffset = source.GetPosition();
            long currentOffset = MINIMUM_SEARCH_OFFSET;
            long lastObjectId = long.MinValue;
            int lastGenID = int.MinValue;
            long lastObjOffset = long.MinValue;
            char[] objString = " obj".ToCharArray();
            char[] endobjString = "endobj".ToCharArray();
            bool endobjFound = false;
            do
            {
                source.Seek(currentOffset);
                if (isString(objString))
                {
                    long tempOffset = currentOffset - 1;
                    source.Seek(tempOffset);
                    int genID = source.Peek();
                    // is the next char a digit?
                    if (isDigit(genID))
                    {
                        genID -= 48;
                        tempOffset--;
                        source.Seek(tempOffset);
                        if (isSpace())
                        {
                            while (tempOffset > MINIMUM_SEARCH_OFFSET && isSpace())
                            {
                                source.Seek(--tempOffset);
                            }
                            bool objectIDFound = false;
                            while (tempOffset > MINIMUM_SEARCH_OFFSET && isDigit())
                            {
                                source.Seek(--tempOffset);
                                objectIDFound = true;
                            }
                            if (objectIDFound)
                            {
                                source.Read();
                                long objectId = readObjectNumber();
                                if (lastObjOffset > 0)
                                {
                                    // add the former object ID only if there was a subsequent object ID
                                    bfSearchCOSObjectKeyOffsets[new CosObjectKey(lastObjectId, lastGenID)] = lastObjOffset;
                                }
                                lastObjectId = objectId;
                                lastGenID = genID;
                                lastObjOffset = tempOffset + 1;
                                currentOffset += objString.Length - 1;
                                endobjFound = false;
                            }
                        }
                    }
                }
                else if (isString(endobjString))
                {
                    endobjFound = true;
                    currentOffset += endobjString.Length - 1;
                }
                currentOffset++;
            } while (currentOffset < lastEOFMarker && !source.IsEof());
            if ((lastEOFMarker < long.MaxValue || endobjFound) && lastObjOffset > 0)
            {
                // if the pdf wasn't cut off in the middle or if the last object ends with a "endobj" marker
                // the last object id has to be added here so that it can't get lost as there isn't any subsequent object id
                bfSearchCOSObjectKeyOffsets[new CosObjectKey(lastObjectId, lastGenID)] = lastObjOffset;
            }
            // reestablish origin position
            source.Seek(originOffset);
        }

        /**
         * Search for the offset of the given xref table/stream among those found by a brute force search.
         * 
         * @return the offset of the xref entry
         * @throws InvalidOperationException if something went wrong
         */
        private long bfSearchForXRef(long xrefOffset)
        {
            long newOffset = -1;
            long newOffsetTable = -1;
            long newOffsetStream = -1;
            bfSearchForXRefTables();
            bfSearchForXRefStreams();
            if (bfSearchXRefTablesOffsets != null)
            {
                // TODO to be optimized, this won't work in every case
                newOffsetTable = searchNearestValue(bfSearchXRefTablesOffsets, xrefOffset);
            }
            if (bfSearchXRefStreamsOffsets != null)
            {
                // TODO to be optimized, this won't work in every case
                newOffsetStream = searchNearestValue(bfSearchXRefStreamsOffsets, xrefOffset);
            }
            // choose the nearest value
            if (newOffsetTable > -1 && newOffsetStream > -1)
            {
                long differenceTable = xrefOffset - newOffsetTable;
                long differenceStream = xrefOffset - newOffsetStream;
                if (Math.Abs(differenceTable) > Math.Abs(differenceStream))
                {
                    newOffset = newOffsetStream;
                    bfSearchXRefStreamsOffsets.Remove(newOffsetStream);
                }
                else
                {
                    newOffset = newOffsetTable;
                    bfSearchXRefTablesOffsets.Remove(newOffsetTable);
                }
            }
            else if (newOffsetTable > -1)
            {
                newOffset = newOffsetTable;
                bfSearchXRefTablesOffsets.Remove(newOffsetTable);
            }
            else if (newOffsetStream > -1)
            {
                newOffset = newOffsetStream;
                bfSearchXRefStreamsOffsets.Remove(newOffsetStream);
            }
            return newOffset;
        }

        private long searchNearestValue(List<long> values, long offset)
        {
            long newValue = -1;
            long? currentDifference = null;
            int currentOffsetIndex = -1;
            int numberOfOffsets = values.Count;
            // find the nearest value
            for (int i = 0; i < numberOfOffsets; i++)
            {
                long newDifference = offset - values[i];
                // find the nearest offset
                if (!currentDifference.HasValue || (Math.Abs(currentDifference.Value) > Math.Abs(newDifference)))
                {
                    currentDifference = newDifference;
                    currentOffsetIndex = i;
                }
            }
            if (currentOffsetIndex > -1)
            {
                newValue = values[currentOffsetIndex];
            }
            return newValue;
        }

        /**
         * Brute force search for the last EOF marker.
         * 
         * @throws InvalidOperationException if something went wrong
         */
        private void bfSearchForLastEOFMarker()
        {
            if (lastEOFMarker == null)
            {
                long originOffset = source.GetPosition();
                source.Seek(MINIMUM_SEARCH_OFFSET);
                while (!source.IsEof())
                {
                    // search for EOF marker
                    if (isString(EOF_MARKER))
                    {
                        long tempMarker = source.GetPosition();
                        source.Seek(tempMarker + 5);
                        try
                        {
                            // check if the following data is some valid pdf content
                            // which most likely indicates that the pdf is linearized,
                            // updated or just cut off somewhere in the middle
                            SkipSpaces();
                            readObjectNumber();
                            readGenerationNumber();
                        }
                        catch (InvalidOperationException exception)
                        {
                            // save the EOF marker as the following data is most likely some garbage
                            lastEOFMarker = tempMarker;
                        }
                    }
                    source.Read();
                }
                source.Seek(originOffset);
                // no EOF marker found
                if (lastEOFMarker == null)
                {
                    lastEOFMarker = long.MaxValue;
                }
            }
        }

        /**
         * Brute force search for all xref entries (tables).
         * 
         * @throws InvalidOperationException if something went wrong
         */
        private void bfSearchForXRefTables()
        {
            if (bfSearchXRefTablesOffsets == null)
            {
                // a pdf may contain more than one xref entry
                bfSearchXRefTablesOffsets = new List<long>();
                long originOffset = source.GetPosition();
                source.Seek(MINIMUM_SEARCH_OFFSET);
                // search for xref tables
                while (!source.IsEof())
                {
                    if (isString(XREF_TABLE))
                    {
                        long newOffset = source.GetPosition();
                        source.Seek(newOffset - 1);
                        // ensure that we don't read "startxref" instead of "xref"
                        if (isWhitespace())
                        {
                            bfSearchXRefTablesOffsets.Add(newOffset);
                        }
                        source.Seek(newOffset + 4);
                    }
                    source.Read();
                }
                source.Seek(originOffset);
            }
        }

        /**
         * Brute force search for the last startxref entry.
         * 
         * @throws InvalidOperationException if something went wrong
         */
        private long bfSearchForLastStartxrefEntry()
        {
            long lastStartxref = -1;
            source.Seek(MINIMUM_SEARCH_OFFSET);
            // search for startxref
            while (!source.IsEof())
            {
                if (isString(STARTXREF))
                {
                    lastStartxref = source.GetPosition();
                    source.Seek(lastStartxref + 9);
                }
                source.Read();
            }
            return lastStartxref;
        }

        /**
         * Brute force search for all /XRef entries (streams).
         * 
         * @throws InvalidOperationException if something went wrong
         */
        private void bfSearchForXRefStreams()
        {
            if (bfSearchXRefStreamsOffsets == null)
            {
                // a pdf may contain more than one /XRef entry
                bfSearchXRefStreamsOffsets = new List<long>();
                long originOffset = source.GetPosition();
                source.Seek(MINIMUM_SEARCH_OFFSET);
                // search for XRef streams
                String objString = " obj";
                char[] str = objString.ToCharArray();
                while (!source.IsEof())
                {
                    if (isString(XREF_STREAM))
                    {
                        // search backwards for the beginning of the stream
                        long newOffset = -1;
                        long xrefOffset = source.GetPosition();
                        bool objFound = false;
                        for (int i = 1; i < 40 && !objFound; i++)
                        {
                            long currentOffset = xrefOffset - (i * 10);
                            if (currentOffset > 0)
                            {
                                source.Seek(currentOffset);
                                for (int j = 0; j < 10; j++)
                                {
                                    if (isString(str))
                                    {
                                        long tempOffset = currentOffset - 1;
                                        source.Seek(tempOffset);
                                        int genID = source.Peek();
                                        // is the next char a digit?
                                        if (isDigit(genID))
                                        {
                                            tempOffset--;
                                            source.Seek(tempOffset);
                                            if (isSpace())
                                            {
                                                int length = 0;
                                                source.Seek(--tempOffset);
                                                while (tempOffset > MINIMUM_SEARCH_OFFSET && isDigit())
                                                {
                                                    source.Seek(--tempOffset);
                                                    length++;
                                                }
                                                if (length > 0)
                                                {
                                                    source.Read();
                                                    newOffset = source.GetPosition();
                                                }
                                            }
                                        }
                                        objFound = true;
                                        break;
                                    }
                                    else
                                    {
                                        currentOffset++;
                                        source.Read();
                                    }
                                }
                            }
                        }
                        if (newOffset > -1)
                        {
                            bfSearchXRefStreamsOffsets.Add(newOffset);
                        }
                        source.Seek(xrefOffset + 5);
                    }
                    source.Read();
                }
                source.Seek(originOffset);
            }
        }

        /**
         * Rebuild the trailer dictionary if startxref can't be found.
         *  
         * @return the rebuild trailer dictionary
         * 
         * @throws InvalidOperationException if something went wrong
         */
        //private CosDictionary rebuildTrailer()
        //{
        //    CosDictionary trailer = null;
        //    Dictionary<CosObjectKey, long> bfCOSObjectKeyOffsets = getBFCosObjectOffsets();
        //    // reset trailer resolver
        //    xrefTrailerResolver.reset();
        //    // use the found objects to rebuild the trailer resolver
        //    xrefTrailerResolver.nextXrefObj(0, XrefTrailerResolver.XRefType.TABLE);
        //    foreach (var entry in bfCOSObjectKeyOffsets)
        //    {
        //        xrefTrailerResolver.setXRef(entry.Key, entry.Value);
        //    }
        //    xrefTrailerResolver.setStartxref(0);
        //    trailer = xrefTrailerResolver.getTrailer();
        //    getDocument().setTrailer(trailer);
        //    // search for the different parts of the trailer dictionary
        //    foreach (var entry in bfCOSObjectKeyOffsets)
        //    {
        //        long offset = entry.Value;
        //        source.Seek(offset);
        //        readObjectNumber();
        //        readGenerationNumber();
        //        readExpectedstring(new string(OBJ_MARKER), true);
        //        try
        //        {
        //            if (source.Peek() != '<')
        //            {
        //                continue;
        //            }
        //            CosDictionary dictionary = parseCosDictionary();
        //            // document catalog
        //            if (isCatalog(dictionary))
        //            {
        //                trailer.setItem(CosName.ROOT, document.getObjectFromPool(entry.Key));
        //            }
        //            // info dictionary
        //            else if (dictionary.containsKey(CosName.MOD_DATE)
        //                    && (dictionary.containsKey(CosName.TITLE)
        //                            || dictionary.containsKey(CosName.AUTHOR)
        //                            || dictionary.containsKey(CosName.SUBJECT)
        //                            || dictionary.containsKey(CosName.KEYWORDS)
        //                            || dictionary.containsKey(CosName.CREATOR)
        //                            || dictionary.containsKey(CosName.PRODUCER)
        //                            || dictionary.containsKey(CosName.CREATION_DATE)))
        //            {
        //                trailer.setItem(CosName.INFO, document.getObjectFromPool(entry.Key));
        //            }
        //            // TODO encryption dictionary
        //        }
        //        catch (InvalidOperationException exception)
        //        {
        //        }
        //    }
        //    return trailer;
        //}

        /**
         * Tell if the dictionary is a PDF catalog. Override this for an FDF catalog.
         * 
         * @param dictionary
         * @return 
         */
        protected bool isCatalog(CosDictionary dictionary)
        {
            return CosName.CATALOG.Equals(dictionary.getCosName(CosName.TYPE));
        }

        /**
         * This will parse the startxref section from the stream.
         * The startxref value is ignored.
         *
         * @return the startxref value or -1 on parsing error
         * @throws InvalidOperationException If an IO error occurs.
         */
        private long parseStartXref()
        {
            long startXref = -1;
            if (isString(STARTXREF))
            {
                readstring();
                SkipSpaces();
                // This integer is the byte offset of the first object referenced by the xref or xref stream
                startXref = readLong();
            }
            return startXref;
        }

        /**
         * Checks if the given string can be found at the current offset.
         * 
         * @param string the bytes of the string to look for
         * @return true if the bytes are in place, false if not
         * @throws InvalidOperationException if something went wrong
         */
        private bool isString(byte[] str)
        {
            bool matches(byte[] one, byte[] two)
            {
                if (one.Length != two.Length)
                {
                    return false;
                }

                for (int i = 0; i < one.Length; i++)
                {
                    if (one[i] != two[i])
                    {
                        return false;
                    }
                }

                return true;
            }


            bool bytesMatching = false;
            if (source.Peek() == str[0])
            {
                int length = str.Length;
                byte[] bytesRead = new byte[length];
                int numberOfBytes = source.Read(bytesRead, 0, length);
                while (numberOfBytes < length)
                {
                    int readMore = source.Read(bytesRead, numberOfBytes, length - numberOfBytes);
                    if (readMore < 0)
                    {
                        break;
                    }
                    numberOfBytes += readMore;
                }
                bytesMatching = matches(str, bytesRead);
                source.Rewind(numberOfBytes);
            }
            return bytesMatching;
        }

        /**
         * Checks if the given string can be found at the current offset.
         * 
         * @param string the bytes of the string to look for
         * @return true if the bytes are in place, false if not
         * @throws InvalidOperationException if something went wrong
         */
        private bool isString(string str) => isString(str.ToCharArray());
        private bool isString(char[] str)
        {
            bool bytesMatching = true;
            long originOffset = source.GetPosition();
            foreach (char c in str)
            {
                if (source.Read() != c)
                {
                    bytesMatching = false;
                    break;
                }
            }
            source.Seek(originOffset);
            return bytesMatching;
        }
        
        /**
         * This will parse the xref table from the stream and add it to the state
         * The XrefTable contents are ignored.
         * @param startByteOffset the offset to start at
         * @return false on parsing error
         * @throws InvalidOperationException If an IO error occurs.
         */
        protected bool parseXrefTable(long startByteOffset)
        {
            long xrefTableStartOffset = source.GetPosition();
            if (source.Peek() != 'x')
            {
                return false;
            }
            String xref = readstring();
            if (!xref.Trim().Equals("xref"))
            {
                return false;
            }

            // check for trailer after xref
            String str = readstring();
            byte[]
        b = OtherEncodings.StringAsLatin1Bytes(str);
            source.Rewind(b.Length);

            // signal start of new XRef
            xrefTrailerResolver.nextXrefObj(startByteOffset, XrefTrailerResolver.XRefType.TABLE);

            if (str.StartsWith("trailer"))
            {
               // LOG.warn("skipping empty xref table");
                return false;
            }

            // Xref tables can have multiple sections. Each starts with a starting object id and a count.
            while (true)
            {
                String currentLine = readLine();
                String[] splitString = currentLine.Split(new[]{"\\s"}, StringSplitOptions.RemoveEmptyEntries);
                if (splitString.Length != 2)
                {
                    //LOG.warn("Unexpected XRefTable Entry: " + currentLine);
                    break;
                }
                // first obj id
                long currObjID = long.Parse(splitString[0]);
                // the number of objects in the xref table
                int count = int.Parse(splitString[1]);

                SkipSpaces();
                for (int i = 0; i < count; i++)
                {
                    if (source.IsEof() || isEndOfName((char)source.Peek()))
                    {
                        break;
                    }
                    if (source.Peek() == 't')
                    {
                        break;
                    }
                    //Ignore table contents
                    currentLine = readLine();
                    splitString = currentLine.Split(new []{"\\s"}, StringSplitOptions.RemoveEmptyEntries);
                    if (splitString.Length < 3)
                    {
                        //LOG.warn("invalid xref line: " + currentLine);
                        break;
                    }
                    /* This supports the corrupt table as reported in
                     * PDFBOX-474 (XXXX XXX XX n) */
                    if (splitString[splitString.Length - 1].Equals("n"))
                    {
                        try
                        {
                            long currOffset = long.Parse(splitString[0]);
                            if (currOffset >= xrefTableStartOffset && currOffset <= source.GetPosition())
                            {
                                // PDFBOX-3923: offset points inside this table - that can't be good
                                throw new InvalidOperationException("XRefTable offset " + currOffset +
                                        " is within xref table for " + currObjID);
                            }
                            int currGenID = int.Parse(splitString[1]);
                            CosObjectKey objKey = new CosObjectKey(currObjID, currGenID);
                            xrefTrailerResolver.setXRef(objKey, currOffset);
                        }
                        catch (FormatException e)
                        {
                            throw new InvalidOperationException("Bad", e);
                        }
                    }
                    else if (!splitString[2].Equals("f"))
                    {
                        throw new InvalidOperationException("Corrupt XRefTable Entry - ObjID:" + currObjID);
                    }
                    currObjID++;
                    SkipSpaces();
                }
                SkipSpaces();
                if (!isDigit())
                {
                    break;
                }
            }
            return true;
        }

        /**
         * Fills XRefTrailerResolver with data of given stream.
         * Stream must be of type XRef.
         * @param stream the stream to be read
         * @param objByteOffset the offset to start at
         * @param isStandalone should be set to true if the stream is not part of a hybrid xref table
         * @throws InvalidOperationException if there is an error parsing the stream
         */
        private void parseXrefStream(COSStream stream, long objByteOffset, bool isStandalone)
        {
            // the cross reference stream of a hybrid xref table will be added to the existing one
            // and we must not override the offset and the trailer
            if (isStandalone)
            {
                xrefTrailerResolver.nextXrefObj(objByteOffset, XrefTrailerResolver.XRefType.STREAM);
                xrefTrailerResolver.setTrailer(stream);
            }
          //  PDFXrefStreamParser parser = new PDFXrefStreamParser(stream, document, xrefTrailerResolver);
            //parser.parse();
        }

        /**
         * This will get the document that was parsed.  parse() must be called before this is called.
         * When you are done with this document you must call close() on it to release
         * resources.
         *
         * @return The document that was parsed.
         *
         * @throws InvalidOperationException If there is an error getting the document.
         */
        public COSDocument getDocument()
        {
            if (document == null)
            {
                throw new InvalidOperationException("You must call parse() before calling getDocument()");
            }
            return document;
        }

        /**
         * Parse the values of the trailer dictionary and return the root object.
         *
         * @param trailer The trailer dictionary.
         * @return The parsed root object.
         * @throws InvalidOperationException If an IO error occurs or if the root object is
         * missing in the trailer dictionary.
         */
        protected static CosBase parseTrailerValuesDynamically(CosDictionary trailer, BruteForceSearcher searcher, 
            CosBaseParser baseParser, IRandomAccessRead reader, bool isLenientParsing, COSDocument document, CosStreamParser streamParser,
            CosObjectPool pool)
        {
            // PDFBOX-1557 - ensure that all CosObject are loaded in the trailer
            // PDFBOX-1606 - after securityHandler has been instantiated
            foreach (CosBase trailerEntry in trailer.getValues())
            {
                if (trailerEntry is CosObject)
            {
                CosObject tmpObj = (CosObject)trailerEntry;
                parseObjectDynamically(tmpObj, false, searcher, baseParser, streamParser, reader, isLenientParsing, document, pool);
            }
        }
        // parse catalog or root object
        CosObject root = (CosObject)trailer.getItem(CosName.ROOT);
        if (root == null)
        {
        throw new InvalidOperationException("Missing root object specification in trailer.");
    }
        return parseObjectDynamically(root, false, searcher, baseParser, streamParser, reader, isLenientParsing, document, pool);
}

}

}
