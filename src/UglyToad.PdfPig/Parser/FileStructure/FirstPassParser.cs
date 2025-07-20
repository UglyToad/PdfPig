namespace UglyToad.PdfPig.Parser.FileStructure;

using Core;
using Logging;
using Tokenization.Scanner;

internal static partial class FirstPassParser
{
    public static FirstPassResults Parse(IInputBytes input, ISeekableTokenScanner scanner, ILog? log = null)
    {
        log ??= new NoOpLog();

        // 1. Find the "startxref" declared in the file and its corresponding offset value.
        var startXrefLocation = GetFirstCrossReferenceOffset(input, scanner, log);

        // 2. Read all XRef streams and tables using the offsets provided by the file.
        var streamsAndTables = GetXrefPartsDirectly(
            input,
            scanner,
            startXrefLocation,
            log);

        if (streamsAndTables.Count == 0)
        {
            // 3. If we can't parse the XRefs using the file data then fall back to brute-forcing every part.
            var bruteForce = XrefBruteForcer.FindAllXrefsInFileOrder(input, scanner, log);

            if (streamsAndTables.Count == 0)
            {
                // Error, failed to find any XRefs in file.
                return new FirstPassResults();
            }
        }

        // Concretize the xref
        return new FirstPassResults();
    }

    private static IReadOnlyList<object> GetXrefPartsDirectly(
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
        var results = new List<object>();
        long? nextLocation = startLocation.StartXRefDeclaredOffset.Value;
        do
        {
            var streamOrTable = GetXrefStreamOrTable(
                input,
                scanner,
                nextLocation.Value!,
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

    private static object? GetXrefStreamOrTable(
        IInputBytes input,
        ISeekableTokenScanner scanner,
        long location,
        ILog log)
    {
        var table = XrefTableParser.TryReadTableAtOffset(
            location,
            input,
            scanner,
            log);

        if (table != null)
        {
            return table;
        }

        var stream = XrefStreamParser.TryReadStreamAtOffset(location,
            input,
            scanner,
            log);

        return stream;
    }

    private static IReadOnlyList<object> BruteForceXrefs(
        IInputBytes input,
        ISeekableTokenScanner scanner,
        ILog log)
    {
        return [];
    }

}


internal class FirstPassResults
{

}