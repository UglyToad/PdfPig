namespace UglyToad.Pdf.Parser.Parts.CrossReference
{
    using System;
    using ContentStream;
    using ContentStream.TypedAccessors;
    using Cos;
    using IO;
    using Logging;
    using Util;

    internal class CrossReferenceTableParser
    {
        private const string InUseEntry = "n";
        private const string FreeEntry = "f";

        private readonly ILog log;
        private readonly IDictionaryParser dictionaryParser;
        private readonly IBaseParser baseParser;

        public CrossReferenceTableParser(ILog log, IDictionaryParser dictionaryParser, IBaseParser baseParser)
        {
            this.log = log;
            this.dictionaryParser = dictionaryParser;
            this.baseParser = baseParser;
        }

        public bool TryParse(IRandomAccessRead source, long offset, bool isLenientParsing, CosObjectPool pool, out CrossReferenceTablePartBuilder builder)
        {
            builder = null;

            var tableStartOffset = source.GetPosition();

            if (source.Peek() != 'x')
            {
                return false;
            }

            var xref = ReadHelper.ReadString(source);
            if (!xref.Trim().Equals("xref"))
            {
                return false;
            }

            // check for trailer after xref
            var str = ReadHelper.ReadString(source);
            byte[] b = OtherEncodings.StringAsLatin1Bytes(str);

            source.Rewind(b.Length);
            
            if (str.StartsWith("trailer"))
            {
                log.Warn("skipping empty xref table");
                return false;
            }

            builder = new CrossReferenceTablePartBuilder
            {
                Offset = offset,
                XRefType = CrossReferenceType.Table
            };

            // Tables can have multiple sections. Each starts with a starting object id and a count.
            while (true)
            {
                if (!TableSubsectionDefinition.TryRead(log, source, out var subsectionDefinition))
                {
                    log.Warn($"Unexpected subsection definition in the cross-reference table at offset {offset}");

                    if (isLenientParsing)
                    {
                        
                        break;
                    }

                    return false;
                }

                var currentObjectId = subsectionDefinition.FirstNumber;

                ReadHelper.SkipSpaces(source);
                for (var i = 0; i < subsectionDefinition.Count; i++)
                {
                    if (source.IsEof() || ReadHelper.IsEndOfName((char)source.Peek()))
                    {
                        break;
                    }

                    if (source.Peek() == 't')
                    {
                        break;
                    }

                    //Ignore table contents
                    var currentLine = ReadHelper.ReadLine(source);
                    var splitString = currentLine.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                    if (splitString.Length < 3)
                    {
                        log.Warn("invalid xref line: " + currentLine);
                        break;
                    }

                    // This supports the corrupt table as reported in PDFBOX-474 (XXXX XXX XX n)
                    if (splitString[splitString.Length - 1].Equals(InUseEntry))
                    {
                        try
                        {
                            var objectOffset = long.Parse(splitString[0]);

                            if (objectOffset >= tableStartOffset && objectOffset <= source.GetPosition())
                            {
                                // PDFBOX-3923: offset points inside this table - that can't be good
                                throw new InvalidOperationException(
                                    $"Object offset {objectOffset} is within its own cross-reference table for object {currentObjectId}");
                            }

                            var generation = int.Parse(splitString[1]);
                            builder.Add(currentObjectId, generation, objectOffset);
                        }
                        catch (FormatException e)
                        {
                            throw new InvalidOperationException("Bad", e);
                        }
                    }
                    else if (!splitString[2].Equals(FreeEntry))
                    {
                        throw new InvalidOperationException(
                            $"Corrupt cross-reference table entry for object {currentObjectId}. The indicator was not 'n' or 'f' but {splitString[2]}.");
                    }

                    currentObjectId++;

                    ReadHelper.SkipSpaces(source);
                }

                ReadHelper.SkipSpaces(source);
                if (!ReadHelper.IsDigit(source))
                {
                    break;
                }
            }

            if (!TryParseTrailer(source, isLenientParsing, pool, out var trailer))
            {
                throw new InvalidOperationException($"Something went wrong trying to read the XREF table at {offset}.");
            }
            
            builder.Dictionary = trailer;
            builder.Previous = trailer.GetLongOrDefault(CosName.PREV);
            
            return true;
        }

        private bool TryParseTrailer(IRandomAccessRead source, bool isLenientParsing, CosObjectPool pool, out PdfDictionary trailer)
        {
            trailer = null;
            // parse the last trailer.
            var trailerOffset = source.GetPosition();
            // PDFBOX-1739 skip extra xref entries in RegisSTAR documents
            if (isLenientParsing)
            {
                int nextCharacter = source.Peek();
                while (nextCharacter != 't' && ReadHelper.IsDigit(nextCharacter))
                {
                    if (source.GetPosition() == trailerOffset)
                    {
                        // warn only the first time
                        //LOG.warn("Expected trailer object at position " + trailerOffset
                        //        + ", keep trying");
                    }
                    ReadHelper.ReadLine(source);
                    nextCharacter = source.Peek();
                }
            }
            if (source.Peek() != 't')
            {
                return false;
            }
            //read "trailer"
            long currentOffset = source.GetPosition();
            string nextLine = ReadHelper.ReadLine(source);
            if (!nextLine.Trim().Equals("trailer"))
            {
                // in some cases the EOL is missing and the trailer immediately
                // continues with "<<" or with a blank character
                // even if this does not comply with PDF reference we want to support as many PDFs as possible
                // Acrobat reader can also deal with this.
                if (nextLine.StartsWith("trailer"))
                {
                    // we can't just unread a portion of the read data as we don't know if the EOL consist of 1 or 2 bytes
                    int len = "trailer".Length;
                    // jump back right after "trailer"
                    source.Seek(currentOffset + len);
                }
                else
                {
                    return false;
                }
            }

            // in some cases the EOL is missing and the trailer continues with " <<"
            // even if this does not comply with PDF reference we want to support as many PDFs as possible
            // Acrobat reader can also deal with this.
            ReadHelper.SkipSpaces(source);

            PdfDictionary parsedTrailer = dictionaryParser.Parse(source, baseParser, pool);

            trailer = parsedTrailer;

            ReadHelper.SkipSpaces(source);
            return true;
        }
    }
}
