using System;
using System.Collections.Generic;
using System.Text;

namespace UglyToad.Pdf.Parser
{
    using Cos;
    using Filters;
    using IO;
    using Logging;
    using Parts;
    using Parts.CrossReference;

    internal class PDFParser : COSParser
    {
        private String password = "";
        private IInputStream keyStoreInputStream = null;
        private String keyAlias = null;
        private FileHeaderParser headerParser = new FileHeaderParser(null);
        private FileTrailerParser trailerParser = new FileTrailerParser();

        public PDFParser(IRandomAccessRead source, string decryptionPassword, IInputStream keyStore,
                         String alias) : base(source)
        {
            fileLen = source.Length();
            password = decryptionPassword;
            keyStoreInputStream = keyStore;
            keyAlias = alias;
            init();

        }

        private void init()
        {
            document = new COSDocument();
        }

        /**
         * The initial parse will first parse only the trailer, the xrefstart and all xref tables to have a pointer (offset)
         * to all the pdf's objects. It can handle linearized pdfs, which will have an xref at the end pointing to an xref
         * at the beginning of the file. Last the root object is parsed.
         * 
         * @throws InvalidPasswordException If the password is incorrect.
         * @throws IOException If something went wrong.
         */
        protected void initialParse(bool isLenient)
        {
            // Find the cross reference table at the offset given at the end of the document
            var xrefOffset = trailerParser.GetXrefOffset(source, isLenient);

            ILog log = null;
            var bruteForceSearcher = new BruteForceSearcher(source);
            var nameParser = new CosNameParser();
            var dictionaryParser = new CosDictionaryParser(nameParser, log);
            var baseParser = new CosBaseParser(nameParser, new CosStringParser(), dictionaryParser, new CosArrayParser());
            var streamParser = new CosStreamParser(log);
            var filterProvider = new MemoryFilterProvider(new DecodeParameterResolver(log), new PngPredictor(), log);
            var crossReferenceParser = new CrossReferenceStreamParser(filterProvider);

            var crossReferenceTableParser = new FileCrossReferenceTableParser(log, dictionaryParser, baseParser, streamParser, crossReferenceParser,
                new CrossReferenceTableParser(log, dictionaryParser, baseParser));

            var pool = new CosObjectPool();

            var table = crossReferenceTableParser.Parse(source, isLenient, xrefOffset, pool);
            
            CosBase baseObj = parseTrailerValuesDynamically(document.trailer, bruteForceSearcher, baseParser, source, isLenient, document, streamParser, pool);
            if (!(baseObj is CosDictionary))
            {
                throw new InvalidOperationException("Expected root dictionary, but got this: " + baseObj);
            }

            CosDictionary root = (CosDictionary)baseObj;
            // in some pdfs the type value "Catalog" is missing in the root object
            if (isLenient && !root.containsKey(CosName.TYPE))
            {
                root.setItem(CosName.TYPE, CosName.CATALOG);
            }

            CosObject catalogObj = document.getCatalog();
            if (catalogObj.GetObject() is CosDictionary)
            {
                parseDictObjects((CosDictionary)catalogObj.GetObject(), (CosName[])null, bruteForceSearcher, baseParser, streamParser, source, document, isLenient, pool);

                CosBase infoBase = document.trailer.getDictionaryObject(CosName.INFO);
                if (infoBase is CosDictionary)
                {
                    parseDictObjects((CosDictionary)infoBase, (CosName[])null, bruteForceSearcher, baseParser, streamParser, source, document, isLenient, pool);
                }

                document.IsDecrypted = true;
            }
            initialParseDone = true;
        }

        /**
         * This will parse the stream and populate the COSDocument object.  This will close
         * the keystore stream when it is done parsing.
         *
         * @throws InvalidPasswordException If the password is incorrect.
         * @throws IOException If there is an error reading from the stream or corrupt data
         * is found.
         */
        public void Parse(bool isLenientParsing)
        {
            // set to false if all is processed
            bool exceptionOccurred = true;

            try
            {
                // Read the version from the top of the file
                var version = headerParser.ReadHeader(source, getIsLenient());
                document.Version = version.Version;

                if (!initialParseDone)
                {
                    initialParse(isLenientParsing);
                }

                exceptionOccurred = false;
            }
            finally
            {
                IOUtils.closeQuietly(keyStoreInputStream);

                if (exceptionOccurred && document != null)
                {
                    IOUtils.closeQuietly(document);
                    document = null;
                }
            }
        }
    }

}
