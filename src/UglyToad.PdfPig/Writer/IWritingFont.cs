namespace UglyToad.PdfPig.Writer
{
    using System.Collections.Generic;
    using Geometry;
    using Tokens;

    internal interface IWritingFont
    {
        bool HasWidths { get; }

        bool TryGetBoundingBox(char character, out PdfRectangle boundingBox);

        IReadOnlyDictionary<IToken, IToken> GetDictionary(NameToken fontKeyName);
    }
}