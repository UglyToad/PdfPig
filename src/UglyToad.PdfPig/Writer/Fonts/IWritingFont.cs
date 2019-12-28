namespace UglyToad.PdfPig.Writer.Fonts
{
    using System.IO;
    using Core;
    using Geometry;
    using Tokens;

    internal interface IWritingFont
    {
        bool HasWidths { get; }

        string Name { get; }

        bool TryGetBoundingBox(char character, out PdfRectangle boundingBox);

        bool TryGetAdvanceWidth(char character, out double width);

        TransformationMatrix GetFontMatrix();

        ObjectToken WriteFont(NameToken fontKeyName, Stream outputStream, BuilderContext context);

        byte GetValueForCharacter(char character);
    }
}