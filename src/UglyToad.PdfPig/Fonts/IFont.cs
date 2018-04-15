namespace UglyToad.PdfPig.Fonts
{
    using Core;
    using Geometry;
    using IO;
    using Tokenization.Tokens;

    internal interface IFont
    {
        NameToken Name { get; }
        
        bool IsVertical { get; }

        int ReadCharacterCode(IInputBytes bytes, out int codeLength);

        bool TryGetUnicode(int characterCode, out string value);

        PdfRectangle GetBoundingBox(int characterCode);

        TransformationMatrix GetFontMatrix();
    }
}
