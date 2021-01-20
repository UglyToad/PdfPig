namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands
{
    using System;
    using System.Collections.Generic;
    using Core;

    internal class Type1BuildCharContext
    {
        private readonly Func<int, PdfSubpath> characterByIndexFactory;
        private readonly Func<string, PdfSubpath> characterByNameFactory;
        public IReadOnlyDictionary<int, Type1CharStrings.CommandSequence> Subroutines { get; }

        public double WidthX { get; set; }

        public double WidthY { get; set; }

        public double LeftSideBearingX { get; set; }

        public double LeftSideBearingY { get; set; }

        public bool IsFlexing { get; set; }

        public PdfSubpath Path { get; private set; } = new PdfSubpath();

        public PdfPoint CurrentPosition { get; set; }

        public CharStringStack Stack { get; } = new CharStringStack();

        public CharStringStack PostscriptStack { get; } = new CharStringStack();

        public List<PdfPoint> FlexPoints { get; } = new List<PdfPoint>();

        public Type1BuildCharContext(IReadOnlyDictionary<int, Type1CharStrings.CommandSequence> subroutines,
            Func<int, PdfSubpath> characterByIndexFactory,
            Func<string, PdfSubpath> characterByNameFactory)
        {
            this.characterByIndexFactory = characterByIndexFactory ?? throw new ArgumentNullException(nameof(characterByIndexFactory));
            this.characterByNameFactory = characterByNameFactory ?? throw new ArgumentNullException(nameof(characterByNameFactory));
            Subroutines = subroutines ?? throw new ArgumentNullException(nameof(subroutines));
        }

        public void AddFlexPoint(PdfPoint point)
        {
            FlexPoints.Add(point);
        }

        public PdfSubpath GetCharacter(int characterCode)
        {
            return characterByIndexFactory(characterCode);
        }

        public PdfSubpath GetCharacter(string characterName)
        {
            return characterByNameFactory(characterName);
        }

        public void SetPath(PdfSubpath path)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public void ClearFlexPoints()
        {
            FlexPoints.Clear();
        }
    }
}
