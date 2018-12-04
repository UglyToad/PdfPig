namespace UglyToad.PdfPig.Writer
{
    using System.Collections.Generic;
    using System.IO;
    using Geometry;
    using Tokens;

    internal interface IWritingFont
    {
        bool HasWidths { get; }

        bool TryGetBoundingBox(char character, out PdfRectangle boundingBox);

        ObjectToken WriteFont(NameToken fontKeyName, Stream outputStream, BuilderContext context);
    }
}