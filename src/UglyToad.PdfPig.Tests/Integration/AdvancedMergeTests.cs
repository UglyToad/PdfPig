namespace UglyToad.PdfPig.Tests.Integration;

using PdfPig.Core;
using PdfPig.Tokens;
using PdfPig.Writer;

public class AdvanceMergeTests
{
    [Fact]
    public void TestAdvanceMerge()
    {
        using var inputFile = File.Open(IntegrationHelpers.GetDocumentPath("Various Content Types.pdf"), FileMode.Open, FileAccess.Read);
        using var input = new MemoryStream();
        inputFile.CopyTo(input);
        input.Seek(0, SeekOrigin.Begin);

        using var outputFile = File.Open(IntegrationHelpers.GetDocumentPath("EmptyPdf.pdf"), FileMode.Open);
        using var output = new MemoryStream();
        outputFile.CopyTo(output);
        output.Seek(0, SeekOrigin.Begin);

        using var result = Merge(input, output);
        result.Seek(0, SeekOrigin.Begin);
        using var outputPdf = PdfDocument.Open(result);
        
        Assert.Equal(1, outputPdf.NumberOfPages);
        Assert.Equal(2, outputPdf.Structure.CrossReferenceTable.Parts.Count);  // since we did incremental update, there are 2 xrefs
        Assert.True(outputPdf.Structure.CrossReferenceTable.ObjectOffsets.Count > 3);  // we add more objects into empty pdf (has 3 objects)
    }
    
    private static Stream Merge(Stream input, Stream output)
    {
        using var pdf = PdfDocument.Open(input);

        if (!pdf.Structure.Catalog.CatalogDictionary.TryGet<IndirectReferenceToken>(NameToken.Pages, out var pages))
        {
            throw new ArgumentException("No pages reference were found");
        }

        if (ResolveIndirect(pdf, pages) is not DictionaryToken pagesObj)
        {
            throw new ArgumentException("No pages object were found");
        }

        // Assume, we have only 1 page in here
        if (!pagesObj.TryGet(NameToken.Kids, out ArrayToken kids) || kids.Length != 1)
        {
            throw new ArgumentException("Invalid catalog dictionary");
        }

        var kidReference = kids.Data[0] as IndirectReferenceToken;
        if (ResolveIndirect(pdf, kidReference) is not DictionaryToken pageObj)
        {
            throw new ArgumentException("Invalid catalog dictionary");
        }

        // Skip all pdf meta structure objects 
        var skippedRefs = new HashSet<IndirectReference>
        {
            pages.Data,  // Pages
            kidReference!.Data,  // Page
            pdf.Structure.Trailer.Root,  // Catalog
        };
        
        // Skip all refs from "skippedRefs" and order it by object number
        var oldRefs = pdf.Structure.CrossReferenceTable.ObjectOffsets.Keys
            .Where(k => !skippedRefs.Contains(k))
            .OrderBy(k => k.ObjectNumber)
            .ToList();
            
        using var outputPdf = PdfDocument.Open(output);
        
        // Building refs map, to rebind old objects to their new values
        var refMap = new Dictionary<IndirectReference, IndirectReference>();
        var currentObjectNumber = outputPdf.Structure.Trailer.Size;
        foreach (var oldRef in oldRefs)
        {
            var newRef = new IndirectReference(currentObjectNumber++, 0);
            refMap[oldRef] = newRef;
        }
        
        output.Seek(0, SeekOrigin.End);
        output.WriteByte((byte)'\n');  // without endline pdf wouldn't render in some readers
        
        var newPdfObjects = new Dictionary<IndirectReference, XrefLocation>();
        
        foreach (var oldRef in oldRefs)
        {
            var newObjRef = refMap[oldRef];
            var newXref = XrefLocation.File(output.Position);
            var token = ResolveIndirect(pdf, oldRef);
            var updatedToken = ReplaceReferences(token, refMap);
            
            newPdfObjects[newObjRef] = newXref;
            output.Seek(0, SeekOrigin.End);
            TokenWriter.Instance.WriteToken(new ObjectToken(newXref, newObjRef, updatedToken), output);
        }
        
        // Bind input content to the last output page
        var lastPageRef = FindLastPage(outputPdf);
        
        if (ResolveIndirect(outputPdf, lastPageRef) is not DictionaryToken outputPage)
        {
            throw new ArgumentException("Invalid catalog dictionary");
        }

        if (!pageObj.TryGet<IndirectReferenceToken>(NameToken.Contents, out var contentObj))
        {
            throw new ArgumentException("Invalid catalog dictionary");
        }

        // Assume we have resources needed for content to render
        if (!pageObj.TryGet<IndirectReferenceToken>(NameToken.Resources, out var resources))
        {
            throw new ArgumentException("Invalid page object");
        }

        output.Seek(0, SeekOrigin.End);
        var xrefLocation = XrefLocation.File(output.Position);
        var newPageObject = outputPage
            .With(NameToken.Contents, new IndirectReferenceToken(refMap[contentObj.Data]))
            .With(NameToken.Resources, new IndirectReferenceToken(refMap[resources.Data]));
        TokenWriter.Instance.WriteToken(new ObjectToken(xrefLocation, lastPageRef.Data, newPageObject), output);
        
        newPdfObjects[lastPageRef.Data] = xrefLocation;
        
        // Writer new xref table
        TokenWriter.Instance.WriteCrossReferenceTable(
            newPdfObjects.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value1),
            outputPdf.Structure.Trailer.Root,
            output,
            null,
            outputPdf.Structure.XrefOffset);
        return output;
    }
    
    private static IToken ReplaceReferences(IToken token, Dictionary<IndirectReference, IndirectReference> mapping)
    {
        return token switch
        {
            IndirectReferenceToken irt => mapping.TryGetValue(irt.Data, out var newRef) ? new IndirectReferenceToken(newRef) : token,
            DictionaryToken dict => ReplaceDictionary(dict, mapping),
            ArrayToken arr => ReplaceArray(arr, mapping),
            StreamToken stream => ReplaceStream(stream, mapping),
            _ => token
        };
    }

    private static DictionaryToken ReplaceDictionary(DictionaryToken original, Dictionary<IndirectReference, IndirectReference> mapping)
    {
        var newDict = new Dictionary<NameToken, IToken>(original.Data.Count);
        foreach (var kvp in original.Data)
        {
            newDict[NameToken.Create(kvp.Key)] = ReplaceReferences(kvp.Value, mapping);
        }
        return new DictionaryToken(newDict);
    }

    private static ArrayToken ReplaceArray(ArrayToken original, Dictionary<IndirectReference, IndirectReference> mapping)
    {
        var newData = new IToken[original.Length];
        for (var i = 0; i < original.Length; i++)
        {
            newData[i] = ReplaceReferences(original.Data[i], mapping);
        }
        return new ArrayToken(newData);
    }

    private static StreamToken ReplaceStream(StreamToken original, Dictionary<IndirectReference, IndirectReference> mapping)
    {
        var updatedDict = ReplaceDictionary(original.StreamDictionary, mapping);
        // We create a new StreamToken with the replaced dictionary, preserving the original byte stream.
        return new StreamToken(updatedDict, original.Data);
    }
    
    private const int MaxIndirectResolutionDepth = 32;

    private static IndirectReferenceToken FindLastPage(PdfDocument pdf)
    {
        if (!pdf.Structure.Catalog.CatalogDictionary.TryGet<IndirectReferenceToken>(NameToken.Pages, out var pagesRef))
        {
            throw new ArgumentException("No pages were found in the input document.");
        }

        if (ResolveIndirect(pdf, pagesRef) is not DictionaryToken pages)
        {
            throw new ArgumentException("No pages were found in the input file.");
        }

        if (!pages.TryGet<ArrayToken>(NameToken.Kids, out var kids))
        {
            throw new ArgumentException("No pages were found in the input document.");
        }

        return FindLastPage(pdf, kids);
    }

    private static IndirectReferenceToken FindLastPage(PdfDocument pdf, ArrayToken pageTree)
    {
        while (true)
        {
            if (pageTree.Length == 0)
            {
                throw new ArgumentException("No leaf in page tree");
            }

            var root = pageTree.Data.Last()!;
            if (ResolveIndirect(pdf, root) is not DictionaryToken newRoot)
            {
                throw new ArgumentException("Indirect page tree");
            }

            if (newRoot.Data[NameToken.Type] is not NameToken type)
            {
                throw new ArgumentException("Indirect page tree");
            }

            if (type.Data == NameToken.Page)
            {
                return (root as IndirectReferenceToken)!;
            }
            pageTree = (newRoot.Data[NameToken.Kids] as ArrayToken)!;
        }
    }

    private static IToken ResolveIndirect(PdfDocument doc, IndirectReference reference)
    {
        return ResolveIndirect(doc, new IndirectReferenceToken(reference));
    }
    
    private static IToken ResolveIndirect(PdfDocument doc, IToken token)
    {
        var depth = 0;
        while (token is IndirectReferenceToken ir)
        {
            if (++depth > MaxIndirectResolutionDepth)
            {
                throw new ArgumentException(
                    "Cyclic or excessively deep indirect reference in PDF signature dictionary.");
            }

            var obj = doc.Structure.GetObject(ir.Data);
            token = obj.Data ?? throw new ArgumentException("Failed to parse PDF digital signature.");
        }

        return token;
    }
}