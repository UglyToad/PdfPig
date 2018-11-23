namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands
{
    using System.Collections.Generic;
    using Geometry;

    internal class Type1BuildCharContext
    {
        public IReadOnlyDictionary<int, Type1CharStrings.CommandSequence> Subroutines { get; }

        public decimal WidthX { get; set; }

        public decimal WidthY { get; set; }

        public decimal LeftSideBearingX { get; set; }

        public decimal LeftSideBearingY { get; set; }

        public bool IsFlexing { get; set; }

        public CharacterPath Path { get; } = new CharacterPath();

        public PdfPoint CurrentPosition { get; set; }

        public CharStringStack Stack { get; } = new CharStringStack();

        public CharStringStack PostscriptStack { get; } = new CharStringStack();

        public IReadOnlyList<PdfPoint> FlexPoints { get; }

        public Type1BuildCharContext(IReadOnlyDictionary<int, Type1CharStrings.CommandSequence> subroutines)
        {
            Subroutines = subroutines;
        }

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
