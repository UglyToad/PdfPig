namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands
{
    using Geometry;

    internal class Type1BuildCharContext
    {
        public decimal Width { get; set; }

        public decimal LeftSideBearing { get; set; }

        public bool IsFlexing { get; set; }

        public CharacterPath Path { get; } = new CharacterPath();

        public PdfPoint CurrentPosition { get; set; }

        public Type1Stack Stack { get; } = new Type1Stack();

        public Type1Stack PostscriptStack { get; } = new Type1Stack();
    }
}
