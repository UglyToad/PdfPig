namespace UglyToad.PdfPig.Parser
{
    using System.Collections.Generic;
    using System.Linq;
    using Cos;

    internal class XrefTrailerResolver
    {

        /**
         * A class which represents a xref/trailer object.
         */
        private class XrefTrailerObj
        {
            public CosDictionary trailer = null;

            public XRefType xrefType;

            public readonly Dictionary<CosObjectKey, long> xrefTable = new Dictionary<CosObjectKey, long>();

            /**
             *  Default constructor.
             */
            public XrefTrailerObj()
            {
                xrefType = XRefType.TABLE;
            }

            public void reset()
            {
                xrefTable.Clear();
            }
        }

        /** 
         * The XRefType of a trailer.
         */
        public enum XRefType
        {
            /**
             * XRef table type.
             */
            TABLE,
            /**
             * XRef stream type.
             */
            STREAM
        }

        private readonly Dictionary<long, XrefTrailerObj> bytePosToXrefMap = new Dictionary<long, XrefTrailerObj>();
        private XrefTrailerObj curXrefTrailerObj = null;
        private XrefTrailerObj resolvedXrefTrailer = null;

        /**
         * Returns the first trailer if at least one exists.
         * 
         * @return the first trailer or null
         */
        public CosDictionary getFirstTrailer()
        {
            return getPositionalTrailer(Position.Last);
        }

        /**
         * Returns the last trailer if at least one exists.
         * 
         * @return the last trailer ir null
         */
        public CosDictionary getLastTrailer()
        {
            return getPositionalTrailer(Position.Last);
        }

        private enum Position
        {
            First,
            Last
        }

        private CosDictionary getPositionalTrailer(Position position)
        {
            if (bytePosToXrefMap.Count == 0)
            {
                return null;
            }

            var ordered = bytePosToXrefMap.Keys.OrderBy(x => x);

            var key = position == Position.First ? ordered.First() : ordered.Last();

            if (!bytePosToXrefMap.TryGetValue(key, out XrefTrailerObj result))
            {
                return null;
            }

            return result.trailer;
        }

        /**
         * Returns the count of trailers.
         *
         * @return the count of trailers.
         */
        public int getTrailerCount()
        {
            return bytePosToXrefMap.Count;
        }

        /**
         * Signals that a new XRef object (table or stream) starts.
         * @param startBytePos the offset to start at
         * @param type the type of the Xref object
         */
        public void nextXrefObj(long startBytePos, XRefType type)
        {
            bytePosToXrefMap[startBytePos] = curXrefTrailerObj = new XrefTrailerObj();
            curXrefTrailerObj.xrefType = type;
        }

        /**
         * Returns the XRefTxpe of the resolved trailer.
         * 
         * @return the XRefType or null.
         */
        public XRefType? getXrefType()
        {
            return resolvedXrefTrailer?.xrefType;
        }

        /**
         * Populate XRef HashMap of current XRef object.
         * Will add an Xreftable entry that maps ObjectKeys to byte offsets in the file.
         * @param objKey The objkey, with id and gen numbers
         * @param offset The byte offset in this file
         */
        public void setXRef(CosObjectKey objKey, long offset)
        {
            if (curXrefTrailerObj == null)
            {
                // should not happen...
                // LOG.warn("Cannot add XRef entry for '" + objKey.getNumber() + "' because XRef start was not signalled.");
                return;
            }
            // PDFBOX-3506 check before adding to the map, to avoid entries from the table being 
            // overwritten by obsolete entries in hybrid files (/XRefStm entry)
            if (!curXrefTrailerObj.xrefTable.ContainsKey(objKey))
            {
                curXrefTrailerObj.xrefTable[objKey] = offset;
            }
        }

        /**
         * Adds trailer information for current XRef object.
         *
         * @param trailer the current document trailer dictionary
         */
        public void setTrailer(CosDictionary trailer)
        {
            if (curXrefTrailerObj == null)
            {
                // should not happen...
                //LOG.warn("Cannot add trailer because XRef start was not signalled.");
                return;
            }
            curXrefTrailerObj.trailer = trailer;
        }

        /**
         * Returns the trailer last set by {@link #setTrailer(COSDictionary)}.
         * 
         * @return the current trailer.
         * 
         */
        public CosDictionary getCurrentTrailer()
        {
            return curXrefTrailerObj.trailer;
        }

        /**
         * Sets the byte position of the first XRef
         * (has to be called after very last startxref was read).
         * This is used to resolve chain of active XRef/trailer.
         *
         * In case startxref position is not found we output a
         * warning and use all XRef/trailer objects combined
         * in byte position order.
         * Thus for incomplete PDF documents with missing
         * startxref one could call this method with parameter value -1.
         * 
         * @param startxrefBytePosValue starting position of the first XRef
         * 
         */
        public void setStartxref(long startxrefBytePosValue)
        {
            if (resolvedXrefTrailer != null)
            {
                //LOG.warn("Method must be called only ones with last startxref value.");
                return;
            }

            resolvedXrefTrailer = new XrefTrailerObj { trailer = new CosDictionary() };

            bytePosToXrefMap.TryGetValue(startxrefBytePosValue, out XrefTrailerObj curObj);

            List<long> xrefSeqBytePos = new List<long>();

            if (curObj == null)
            {
                // no XRef at given position
                //LOG.warn("Did not found XRef object at specified startxref position " + startxrefBytePosValue);

                // use all objects in byte position order (last entries overwrite previous ones)
                xrefSeqBytePos.AddRange(bytePosToXrefMap.Keys);
                xrefSeqBytePos.Sort();
            }
            else
            {
                // copy xref type
                resolvedXrefTrailer.xrefType = curObj.xrefType;
                // found starting Xref object
                // add this and follow chain defined by 'Prev' keys
                xrefSeqBytePos.Add(startxrefBytePosValue);
                while (curObj.trailer != null)
                {
                    long prevBytePos = curObj.trailer.getLong(CosName.PREV, -1L);
                    if (prevBytePos == -1)
                    {
                        break;
                    }

                    bytePosToXrefMap.TryGetValue(prevBytePos, out curObj);
                    if (curObj == null)
                    {
                        //LOG.warn("Did not found XRef object pointed to by 'Prev' key at position " + prevBytePos);
                        break;
                    }
                    xrefSeqBytePos.Add(prevBytePos);

                    // sanity check to prevent infinite loops
                    if (xrefSeqBytePos.Count >= bytePosToXrefMap.Count)
                    {
                        break;
                    }
                }
                // have to reverse order so that later XRefs will overwrite previous ones
                xrefSeqBytePos.Reverse();
            }

            // merge used and sorted XRef/trailer
            foreach (long bPos in xrefSeqBytePos)
            {
                bytePosToXrefMap.TryGetValue(bPos, out curObj);
                if (curObj.trailer != null)
                {
                    resolvedXrefTrailer.trailer.addAll(curObj.trailer);
                }

                foreach (var item in curObj.xrefTable)
                {
                    resolvedXrefTrailer.xrefTable[item.Key] = item.Value;
                }
            }

        }

        /**
         * Gets the resolved trailer. Might return <code>null</code> in case
         * {@link #setStartxref(long)} was not called before.
         *
         * @return the trailer if available
         */
        public CosDictionary getTrailer()
        {
            return (resolvedXrefTrailer == null) ? null : resolvedXrefTrailer.trailer;
        }

        /**
         * Gets the resolved xref table. Might return <code>null</code> in case
         *  {@link #setStartxref(long)} was not called before.
         *
         * @return the xrefTable if available
         */
        public Dictionary<CosObjectKey, long> getXrefTable()
        {
            return (resolvedXrefTrailer == null) ? null : resolvedXrefTrailer.xrefTable;
        }

        /** Returns object numbers which are referenced as contained
         *  in object stream with specified object number.
         *  
         *  This will scan resolved xref table for all entries having negated
         *  stream object number as value.
         *
         *  @param objstmObjNr  object number of object stream for which contained object numbers
         *                      should be returned
         *                       
         *  @return set of object numbers referenced for given object stream
         *          or <code>null</code> if {@link #setStartxref(long)} was not
         *          called before so that no resolved xref table exists
         */
        public ISet<long> getContainedObjectNumbers(int objstmObjNr)
        {
            if (resolvedXrefTrailer == null)
            {
                return null;
            }
            ISet<long> refObjNrs = new HashSet<long>();
            long cmpVal = -objstmObjNr;

            foreach (var xrefEntry in resolvedXrefTrailer.xrefTable)
            {
                if (xrefEntry.Value == cmpVal)
                {
                    refObjNrs.Add(xrefEntry.Key.Number);
                }
            }
            return refObjNrs;
        }

        /**
         * Reset all data so that it can be used to rebuild the trailer.
         * 
         */
        public void reset()
        {
            foreach (XrefTrailerObj trailerObj in bytePosToXrefMap.Values)
            {
                trailerObj.reset();
            }
            curXrefTrailerObj = null;
            resolvedXrefTrailer = null;
        }
    }
}
