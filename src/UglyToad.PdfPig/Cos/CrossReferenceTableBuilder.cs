namespace UglyToad.PdfPig.Cos
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ContentStream;
    using ContentStream.TypedAccessors;
    using Logging;

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// The table contains a one-line entry for each indirect object, specifying the location of that object within the body of the file. 
    /// </remarks>
    internal class CrossReferenceTableBuilder
    {
        private readonly List<CrossReferenceTablePart> parts = new List<CrossReferenceTablePart>();
        public IReadOnlyList<CrossReferenceTablePart> Parts => parts;

        public void Add(CrossReferenceTablePart part)
        {
            if (part == null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            parts.Add(part);
        }

        public CrossReferenceTable Build(long startXrefOffset, ILog log)
        {
            CrossReferenceType type = CrossReferenceType.Table;
            PdfDictionary trailerDictionary = new PdfDictionary();
            Dictionary<IndirectReference, long> objectOffsets = new Dictionary<IndirectReference, long>();

            List<long> xrefSeqBytePos = new List<long>();

            var currentPart = parts.FirstOrDefault(x => x.Offset == startXrefOffset);
            
            if (currentPart == null)
            {
                // no XRef at given position
                log.Warn("Did not found XRef object at specified startxref position " + startXrefOffset);

                // use all objects in byte position order (last entries overwrite previous ones)
                xrefSeqBytePos.AddRange(parts.Select(x => x.Offset));
                xrefSeqBytePos.Sort();
            }
            else
            {
                // copy xref type
                type = currentPart.Type;
                

                // found starting Xref object
                // add this and follow chain defined by 'Prev' keys
                xrefSeqBytePos.Add(startXrefOffset);

                while (currentPart.Dictionary != null)
                {
                    long prevBytePos = currentPart.Dictionary.GetLongOrDefault(CosName.PREV, -1L);
                    if (prevBytePos == -1)
                    {
                        break;
                    }

                    currentPart = parts.FirstOrDefault(x => x.Offset == prevBytePos);
                    if (currentPart == null)
                    {
                        log.Warn("Did not found XRef object pointed to by 'Prev' key at position " + prevBytePos);
                        break;
                    }

                    xrefSeqBytePos.Add(prevBytePos);

                    // sanity check to prevent infinite loops
                    if (xrefSeqBytePos.Count >= parts.Count)
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
                var currentObject = parts.First(x => x.Offset == bPos);
                if (currentObject.Dictionary != null)
                {
                    foreach (var entry in currentObject.Dictionary)
                    {
                        /*
                         * If we're at a second trailer, we have a linearized pdf file, meaning that the first Size entry represents
                         * all of the objects so we don't need to grab the second.
                         */
                        if (!entry.Key.Name.Equals("Size")
                            || !trailerDictionary.ContainsKey(CosName.Create("Size")))
                        {
                            trailerDictionary.Set(entry.Key, entry.Value);
                        }
                    }
                }

                foreach (var item in currentObject.ObjectOffsets)
                {
                    objectOffsets[item.Key] = item.Value;
                }
            }

            return new CrossReferenceTable(type, objectOffsets, trailerDictionary);
        }
    }
}
