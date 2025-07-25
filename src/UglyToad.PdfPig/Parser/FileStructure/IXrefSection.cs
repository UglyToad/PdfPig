namespace UglyToad.PdfPig.Parser.FileStructure;

using Core;
using Tokens;

internal interface IXrefSection
{
    public long Offset { get; }

    public IReadOnlyDictionary<IndirectReference, long> ObjectOffsets { get; }

    public DictionaryToken? Dictionary { get; }

    public long? GetPrevious();
}