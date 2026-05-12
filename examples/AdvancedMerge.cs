using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Tokens;
using UglyToad.PdfPig.Writer;

namespace UglyToad.Examples;

public class AdvancedMerge
{
    public static void Run(Stream input, Stream output)
    {
        using var pdf = PdfDocument.Open(input);
        var pdfObjects = pdf.Structure
            .CrossReferenceTable
            .ObjectOffsets
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        if (!pdf.Structure.Catalog.CatalogDictionary.TryGet<IndirectReferenceToken>(NameToken.Pages, out var pages))
            throw new ArgumentException("No pages reference were found");
        
        if (ResolveIndirect(pdf, pages) is not DictionaryToken pagesObj)
            throw new ArgumentException("No pages object were found");
        
        // Assume, we have only 1 page in here
        if (!pagesObj.TryGet(NameToken.Kids, out ArrayToken kids) || kids.Length != 1)
            throw new ArgumentException("Invalid catalog dictionary");
        
        if (ResolveIndirect(pdf, kids.Data[0]) is not DictionaryToken pageObj)
            throw new ArgumentException("Invalid catalog dictionary");
        
        // Skip all pdf meta structure objects 
        var skippedRefs = new HashSet<IndirectReference>
        {
            pages.Data,  // Pages
            pdf.Structure.Trailer.Root,  // Catalog
            (kids.Data[0] as IndirectReferenceToken)!.Data,  // Page
        };
        
        // Skip all refs from "skippedRefs" and order it by object number
        var oldRefs = pdf.Structure.CrossReferenceTable.ObjectOffsets.Keys
            .Where(k => !skippedRefs.Contains(k))
            .OrderBy(k => k.ObjectNumber)
            .ToList();
        
        // Building refs map, to rebind old objects to their new values
        var refMap = new Dictionary<IndirectReference, IndirectReference>();
        var currentObjectNumber = pdf.Structure.Trailer.Size;
        foreach (var oldRef in oldRefs)
        {
            var newRef = new IndirectReference(currentObjectNumber++, 0);
            refMap[oldRef] = newRef;
        }
        
        foreach (var oldRef in oldRefs)
        {
            var newObjRef = refMap[oldRef];
            var newXref = XrefLocation.File(output.Position);
            var token = ResolveIndirect(pdf, oldRef);
            var updatedToken = ReplaceReferences(token, refMap);
            
            pdfObjects[newObjRef] = newXref;
            TokenWriter.Instance.WriteToken(new ObjectToken(newXref, newObjRef, updatedToken), output);
        }
        
        // Writer new xref table
        TokenWriter.Instance.WriteCrossReferenceTable(
            pdfObjects.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Value1),
            pdf.Structure.Trailer.Root,
            output,
            (pdf.Structure.Trailer.Info as IndirectReferenceToken)?.Data,
            pdf.Structure.Trailer.PreviousCrossReferenceOffset);
    }
    
    /// <summary>
    /// Recursively replaces IndirectReferenceToken in the token tree according to the map.
    /// </summary>
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
        for (int i = 0; i < original.Length; i++)
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

    private static IndirectReferenceToken FindLastPage(PdfDocument pdf, DictionaryToken pageTree)
    {
        return FindLastPage(pdf, (pageTree.Data[NameToken.Kids] as ArrayToken)!);
    }

    private static IndirectReferenceToken FindLastPage(PdfDocument pdf, ArrayToken pageTree)
    {
        while (true)
        {
            if (pageTree.Length == 0) 
                throw new ArgumentException("No leaf in page tree");

            var root = pageTree.Data.Last()!;
            if (ResolveIndirect(pdf, root) is not DictionaryToken newRoot) 
                throw new ArgumentException("Indirect page tree");

            if (newRoot.Data[NameToken.Type] is not NameToken type) 
                throw new ArgumentException("Indirect page tree");

            if (type.Data == NameToken.Page) 
                return (root as IndirectReferenceToken)!;
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
                throw new ArgumentException(
                    "Cyclic or excessively deep indirect reference in PDF signature dictionary.");

            var obj = doc.Structure.GetObject(ir.Data);
            token = obj.Data ?? throw new ArgumentException("Failed to parse PDF digital signature.");
        }

        return token;
    }
}