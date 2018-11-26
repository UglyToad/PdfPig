namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands
{
    using System;
    using System.Collections.Generic;
    using Geometry;
    using Util.JetBrains.Annotations;

    internal class Type1BuildCharContext
    {
        private readonly Func<int, CharacterPath> characterByIndexFactory;
        public IReadOnlyDictionary<int, Type1CharStrings.CommandSequence> Subroutines { get; }

        public decimal WidthX { get; set; }

        public decimal WidthY { get; set; }

        public decimal LeftSideBearingX { get; set; }

        public decimal LeftSideBearingY { get; set; }

        public bool IsFlexing { get; set; }

        [NotNull]
        public CharacterPath Path { get; private set; } = new CharacterPath();

        public PdfPoint CurrentPosition { get; set; }

        public CharStringStack Stack { get; } = new CharStringStack();

        public CharStringStack PostscriptStack { get; } = new CharStringStack();

        public IReadOnlyList<PdfPoint> FlexPoints { get; }

        public Type1BuildCharContext(IReadOnlyDictionary<int, Type1CharStrings.CommandSequence> subroutines,
            Func<int, CharacterPath> characterByIndexFactory)
        {
            this.characterByIndexFactory = characterByIndexFactory ?? throw new ArgumentNullException(nameof(characterByIndexFactory));
            Subroutines = subroutines ?? throw new ArgumentNullException(nameof(subroutines));
        }

        public void AddFlexPoint(PdfPoint point)
        {

        }

        public CharacterPath GetCharacter(int characterCode)
        {
            return characterByIndexFactory(characterCode);
        }

        public void SetPath(CharacterPath path)
        {
            Path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public void ClearFlexPoints()
        {
            
        }
    }
}
