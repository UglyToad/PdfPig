namespace UglyToad.PdfPig.Parser.Parts
{
    using System;
    using Cos;
    using IO;

    internal static class DirectObjectFinder
    {
        public static T Find<T>(CosObject baseObject, IPdfObjectParser parser, IRandomAccessRead reader,
            bool isLenientParsing) where T : CosBase
        {
            var result = parser.Parse(baseObject.ToIndirectReference(), reader, isLenientParsing);

            if (result is T resultT)
            {
                return resultT;
            }

            if (result is CosObject obj)
            {
                return Find<T>(obj, parser, reader, isLenientParsing);
            }

            if (result is COSArray arr && arr.Count == 1 && arr.get(0) is CosObject arrayObject)
            {
                return Find<T>(arrayObject, parser, reader, isLenientParsing);
            }

            throw new InvalidOperationException($"Could not find the object {baseObject.ToIndirectReference()} with type {typeof(T).Name}.");
        }
    }
}
