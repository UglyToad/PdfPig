namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands
{
    using System;
    using System.Collections.Generic;
    using Core;

    internal class Type1BuildCharContext
    {
        private readonly Func<int, List<PdfSubpath>> characterByIndexFactory;
        private readonly Func<string, List<PdfSubpath>> characterByNameFactory;
        public IReadOnlyDictionary<int, Type1CharStrings.CommandSequence> Subroutines { get; }

        public double WidthX { get; set; }

        public double WidthY { get; set; }

        public double LeftSideBearingX { get; set; }

        public double LeftSideBearingY { get; set; }

        public bool IsFlexing { get; set; }

        public List<PdfSubpath> Path { get; private set; } = new List<PdfSubpath>();

        public PdfPoint CurrentPosition { get; set; }

        public CharStringStack Stack { get; } = new CharStringStack();

        public CharStringStack PostscriptStack { get; } = new CharStringStack();

        public IReadOnlyList<PdfPoint> FlexPoints { get; }

        public Type1BuildCharContext(IReadOnlyDictionary<int, Type1CharStrings.CommandSequence> subroutines,
            Func<int, List<PdfSubpath>> characterByIndexFactory,
            Func<string, List<PdfSubpath>> characterByNameFactory)
        {
            this.characterByIndexFactory = characterByIndexFactory ?? throw new ArgumentNullException(nameof(characterByIndexFactory));
            this.characterByNameFactory = characterByNameFactory ?? throw new ArgumentNullException(nameof(characterByNameFactory));
            Subroutines = subroutines ?? throw new ArgumentNullException(nameof(subroutines));
        }

        public void AddFlexPoint(PdfPoint point)
        {

        }

        public List<PdfSubpath> GetCharacter(int characterCode)
        {
            return characterByIndexFactory(characterCode);
        }

        public List<PdfSubpath> GetCharacter(string characterName)
        {
            return characterByNameFactory(characterName);
        }

        public void SetPath(List<PdfSubpath> path)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public void ClearFlexPoints()
        {

        }
    }
}
