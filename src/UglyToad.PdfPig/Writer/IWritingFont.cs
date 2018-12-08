namespace UglyToad.PdfPig.Writer
{
    using System.IO;
    using Geometry;
    using Tokens;

    internal interface IWritingFont
    {
        bool HasWidths { get; }

        string Name { get; }

        bool TryGetBoundingBox(char character, out PdfRectangle boundingBox);

        bool TryGetAdvanceWidth(char character, out decimal width);

        ObjectToken WriteFont(NameToken fontKeyName, Stream outputStream, BuilderContext context);
    }
}