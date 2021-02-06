namespace UglyToad.PdfPig.Writer.Fonts
{
    using System.IO;
    using Core;
    using Tokens;

    internal interface IWritingFont
    {
        bool HasWidths { get; }

        string Name { get; }

        bool TryGetBoundingBox(char character, out PdfRectangle boundingBox);

        bool TryGetAdvanceWidth(char character, out double width);

        TransformationMatrix GetFontMatrix();

        IndirectReferenceToken WriteFont(IPdfStreamWriter writer, NameToken fontKeyName);

        byte GetValueForCharacter(char character);
    }
}