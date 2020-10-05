namespace UglyToad.PdfPig.PdfFonts
{
    using Core;
    using System.Collections.Generic;
    using Tokens;

    internal interface IFont
    {
        NameToken Name { get; }
        
        bool IsVertical { get; }

        FontDetails Details { get; }

        int ReadCharacterCode(IInputBytes bytes, out int codeLength);

        bool TryGetUnicode(int characterCode, out string value);

        CharacterBoundingBox GetBoundingBox(int characterCode);

        bool TryGetPath(int characterCode, out List<PdfSubpath> path);

        TransformationMatrix GetFontMatrix();
    }
}
