namespace UglyToad.PdfPig.Writer.Fonts
{
    using Core;
    using Tokens;

    internal interface IWritingFont
    {
        bool HasWidths { get; }

        string Name { get; }

        bool TryGetBoundingBox(char character, out PdfRectangle boundingBox);

        bool TryGetAdvanceWidth(char character, out double width);

        TransformationMatrix GetFontMatrix();

        IndirectReferenceToken WriteFont(IPdfStreamWriter writer, IndirectReferenceToken? reservedIndirect = null);

        byte GetValueForCharacter(char character);
    }
}