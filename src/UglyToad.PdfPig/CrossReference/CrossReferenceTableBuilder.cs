﻿namespace UglyToad.PdfPig.CrossReference
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;
    using Logging;
    using Tokens;

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
            if (part is null)
            {
                throw new ArgumentNullException(nameof(part));
            }

            parts.Add(part);
        }

        public CrossReferenceTable Build(long firstCrossReferenceOffset, long offsetCorrection, bool isLenientParsing, ILog log)
        {
            CrossReferenceType type = CrossReferenceType.Table;
            DictionaryToken trailerDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>());
            Dictionary<IndirectReference, long> objectOffsets = new Dictionary<IndirectReference, long>();

            var xrefPartToBytePositionOrder = new List<long>();

            var currentPart = parts.FirstOrDefault(x => x.Offset == firstCrossReferenceOffset);
            
            if (currentPart is null)
            {
                // no XRef at given position
                log.Warn($"Did not find an XRef object at the specified startxref position {firstCrossReferenceOffset}");

                // use all objects in byte position order (last entries overwrite previous ones)
                xrefPartToBytePositionOrder.AddRange(parts.Select(x => x.Offset));
                xrefPartToBytePositionOrder.Sort();
            }
            else
            {
                // copy xref type
                type = currentPart.Type;

                // found starting Xref object
                // add this and follow chain defined by 'Prev' keys
                xrefPartToBytePositionOrder.Add(firstCrossReferenceOffset);

                // Get any streams that are tied to this table.
                var activePart = currentPart;
                var dependents = parts.Where(x => x.TiedToXrefAtOffset == activePart.Offset);
                foreach (var dependent in dependents)
                {
                    xrefPartToBytePositionOrder.Add(dependent.Offset);
                }

                while (currentPart.Dictionary != null)
                {
                    long prevBytePos = currentPart.GetPreviousOffset();
                    if (prevBytePos == -1)
                    {
                        break;
                    }

                    currentPart = parts.FirstOrDefault(x => x.Offset == prevBytePos || x.Offset == prevBytePos + offsetCorrection);
                    if (currentPart is null)
                    {
                        log.Warn("Did not found XRef object pointed to by 'Prev' key at position " + prevBytePos);
                        break;
                    }

                    xrefPartToBytePositionOrder.Add(prevBytePos);

                    // sanity check to prevent infinite loops
                    if (xrefPartToBytePositionOrder.Count >= parts.Count)
                    {
                        break;
                    }
                }

                // have to reverse order so that later XRefs will overwrite previous ones
                xrefPartToBytePositionOrder.Reverse();
            }

            // merge used and sorted XRef/trailer
            foreach (long bPos in xrefPartToBytePositionOrder)
            {
                var currentObject = parts.First(x => x.Offset == bPos || x.Offset == bPos + offsetCorrection);
                if (currentObject.Dictionary != null)
                {
                    foreach (var entry in currentObject.Dictionary.Data)
                    {
                        /*
                         * If we're at a second trailer, we have a linearized pdf file, meaning that the first Size entry represents
                         * all of the objects so we don't need to grab the second.
                         */
                        if (!entry.Key.Equals("Size", StringComparison.OrdinalIgnoreCase)
                            || !trailerDictionary.ContainsKey(NameToken.Size))
                        {
                            trailerDictionary = trailerDictionary.With(entry.Key, entry.Value);
                        }
                    }
                }

                foreach (var item in currentObject.ObjectOffsets)
                {
                    objectOffsets[item.Key] = item.Value;
                }
            }

            return new CrossReferenceTable(type, objectOffsets, new TrailerDictionary(trailerDictionary, isLenientParsing), 
                parts.Select(x =>
                {
                    var prev = x.GetPreviousOffset();

                    return new CrossReferenceTable.CrossReferenceOffset(x.Offset, prev >= 0 ? prev : default(long?));
                }).ToList());
        }
    }
}
