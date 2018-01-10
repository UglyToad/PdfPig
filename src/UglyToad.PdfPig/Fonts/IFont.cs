namespace UglyToad.PdfPig.Fonts
{
    using Core;
    using Cos;
    using Geometry;
    using IO;

    internal interface IFont
    {
        CosName Name { get; }
        
        bool IsVertical { get; }

        int ReadCharacterCode(IInputBytes bytes, out int codeLength);

        bool TryGetUnicode(int characterCode, out string value);

        PdfVector GetDisplacement(int characterCode);

        decimal GetWidth(int characterCode);

        TransformationMatrix GetFontMatrix();
    }
}
