namespace UglyToad.PdfPig.Parser.FileStructure;

using Core;
using Logging;
using System.Linq;
using Tokenization.Scanner;
using Tokens;

internal static partial class FirstPassParser
{
    public static FirstPassResults Parse(
        FileHeaderOffset fileHeaderOffset,
        IInputBytes input,
        ISeekableTokenScanner scanner,
        ILog? log = null)
    {
        log ??= new NoOpLog();

        IReadOnlyDictionary<IndirectReference, long>? bruteForceOffsets = null;
        var didBruteForce = false;
        DictionaryToken? bruteForceTrailer = null;

        // 1. Find the "startxref" declared in the file and its corresponding offset value.
        var startXrefLocation = GetFirstCrossReferenceOffset(input, scanner, log);

        // 2. Read all XRef streams and tables using the offsets provided by the file.
        var streamsAndTables = GetXrefPartsDirectly(
            fileHeaderOffset,
            input,
            scanner,
            startXrefLocation,
            log);

        if (streamsAndTables.Count == 0)
        {
            // 3. If we can't parse the XRefs using the file data then fall back to brute-forcing every part.
            var bruteForce = XrefBruteForcer.FindAllXrefsInFileOrder(input, scanner, log);

            streamsAndTables = bruteForce.XRefParts;
            bruteForceOffsets = bruteForce.ObjectOffsets;
            bruteForceTrailer = bruteForce.LastTrailer;

            didBruteForce = true;

            if (streamsAndTables.Count == 0
                && (bruteForceOffsets == null || bruteForceOffsets.Count == 0))
            {
                throw new PdfDocumentFormatException(
                    "Could not find any xref tables or streams in this document and could not resolve brute force positions.");
            }
        }

        // 4. Order the xrefs with the leaf last and apply the objects in order.
        var orderedXrefs = new List<IXrefSection>();
        if (didBruteForce)
        {
            // If we brute force just treat the last item in file as the most important.
            orderedXrefs.AddRange(
                streamsAndTables
                    .OrderBy(x => x.Offset));
        }
        else
        {
            // If we didn't brute force then use the previous position for ordering.
            foreach (var obj in streamsAndTables)
            {
                var added = false;
                for (var i = 0; i < orderedXrefs.Count; i++)
                {
                    var orderedXref = orderedXrefs[i];
                    if (orderedXref.GetPrevious() == obj.Offset)
                    {
                        orderedXrefs.Insert(i, obj);
                        added = true;
                        break;
                    }
                }

                if (!added)
                {
                    orderedXrefs.Add(obj);
                }
            }
        }

        DictionaryToken? lastTrailer = null;
        var flattenedOffsets = new Dictionary<IndirectReference, long>();
        foreach (var xrefPart in orderedXrefs)
        {
            if (xrefPart.Dictionary != null)
            {
                // Prefer a dictionary with a root object irrespective of order.
                if (xrefPart.Dictionary.ContainsKey(NameToken.Root)
                    || lastTrailer == null
                    || !lastTrailer.ContainsKey(NameToken.Root))
                {
                    lastTrailer = xrefPart.Dictionary;
                }
            }

            foreach (var objectOffset in xrefPart.ObjectOffsets)
            {
                flattenedOffsets[objectOffset.Key] = objectOffset.Value;
            }
        }

        var result = new FirstPassResults(
            streamsAndTables.ToList(),
            bruteForceOffsets,
            flattenedOffsets,
            lastTrailer ?? bruteForceTrailer);

        return result;
    }

    private static IReadOnlyList<IXrefSection> GetXrefPartsDirectly(
        FileHeaderOffset offset,
        IInputBytes input,
        ISeekableTokenScanner scanner,
        StartXRefLocation startLocation,
        ILog log)
    {
        if (!startLocation.StartXRefDeclaredOffset.HasValue
            || !startLocation.IsValidOffset(input))
        {
            return [];
        }

        var visitedLocations = new HashSet<long>();
        var results = new List<IXrefSection>();
        long? nextLocation = startLocation.StartXRefDeclaredOffset.Value;
        do
        {
            var streamOrTable = GetXrefStreamOrTable(
                offset,
                input,
                scanner,
                nextLocation.Value,
                log);

            if (!visitedLocations.Add(nextLocation.Value))
            {
                // Circular reference.
                return [];
            }

            if (streamOrTable == null)
            {
                return [];
            }

            if (streamOrTable is XrefTable table)
            {
                results.Add(table);
                nextLocation = table.GetPrevious();
            }
            else if (streamOrTable is XrefStream stream)
            {
                results.Add(stream);
                nextLocation = stream.GetPrevious();
            }
        } while (nextLocation.HasValue);

        return results;
    }

    private static IXrefSection? GetXrefStreamOrTable(
        FileHeaderOffset fileHeaderOffset,
        IInputBytes input,
        ISeekableTokenScanner scanner,
        long location,
        ILog log)
    {
        var table = XrefTableParser.TryReadTableAtOffset(
            fileHeaderOffset,
            location,
            input,
            scanner,
            log);

        if (table != null)
        {
            return table;
        }

        var stream = XrefStreamParser.TryReadStreamAtOffset(
            fileHeaderOffset,
            location,
            input,
            scanner,
            log);

        return stream;
    }
}


internal class FirstPassResults
{
    /// <summary>
    /// All xref tables found by the parse operation.
    /// </summary>
    public IReadOnlyList<IXrefSection> Parts { get; }

    /// <summary>
    /// All offsets found if a brute-force search was applied.
    /// </summary>
    public IReadOnlyDictionary<IndirectReference, long>? BruteForceOffsets { get; }

    /// <summary>
    /// All offsets found from the leaf xref.
    /// </summary>
    public IReadOnlyDictionary<IndirectReference, long> XrefOffsets { get; }

    /// <summary>
    /// The trailer dictionary of the leaf xref if we found any.
    /// </summary>
    public DictionaryToken? Trailer { get; }

    public FirstPassResults(
        IReadOnlyList<IXrefSection> parts,
        IReadOnlyDictionary<IndirectReference, long>? bruteForceOffsets,
        IReadOnlyDictionary<IndirectReference, long> xrefOffsets,
        DictionaryToken? trailer)
    {
        Parts = parts;
        BruteForceOffsets = bruteForceOffsets;
        XrefOffsets = xrefOffsets;
        Trailer = trailer;
    }
}