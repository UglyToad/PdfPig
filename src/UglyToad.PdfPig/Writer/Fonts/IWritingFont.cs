namespace UglyToad.PdfPig.Writer.Fonts
{
    using System.Collections.Generic;
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

        /// <summary>
        /// Adds the character code which selects the glyph for the character to the bytes of a string shown by a text operator.
        /// </summary>
        void AppendCharacterCode(char character, List<byte> textBytes);
    }
}