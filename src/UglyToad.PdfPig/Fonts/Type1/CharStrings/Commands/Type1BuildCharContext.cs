namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands
{
    using System.Collections.Generic;
    using Geometry;

    internal class Type1BuildCharContext
    {
        public decimal WidthX { get; set; }

        public decimal WidthY { get; set; }

        public decimal LeftSideBearingX { get; set; }

        public decimal LeftSideBearingY { get; set; }

        public bool IsFlexing { get; set; }

        public CharacterPath Path { get; } = new CharacterPath();

        public PdfPoint CurrentPosition { get; set; }

        public Type1Stack Stack { get; } = new Type1Stack();

        public Type1Stack PostscriptStack { get; } = new Type1Stack();

        public IReadOnlyList<PdfPoint> FlexPoints { get; }

        public void AddFlexPoint(PdfPoint point)
        {

        }

        public CharacterPath GetCharacter(int characterCode)
        {
            return null;
        }

        public void ClearFlexPoints()
        {
            
        }
    }
}
