namespace UglyToad.PdfPig.Fonts.CompactFontFormat
{
    using System.Collections.Generic;

    internal class CompactFontFormatSubroutinesSelector
    {
        private readonly CompactFontFormatIndex global;
        private readonly CompactFontFormatIndex local;

        // CID fonts have per sub-font subroutines.
        private readonly bool isCid;
        private readonly ICompactFontFormatFdSelect fdSelect;
        private readonly IReadOnlyList<CompactFontFormatIndex> perFontLocalSubroutines;

        public CompactFontFormatSubroutinesSelector(CompactFontFormatIndex global, CompactFontFormatIndex local)
        {
            this.global = global;
            this.local = local;
        }

        public CompactFontFormatSubroutinesSelector(CompactFontFormatIndex global, CompactFontFormatIndex local,
            ICompactFontFormatFdSelect fdSelect,
            IReadOnlyList<CompactFontFormatIndex> perFontLocalSubroutines)
        {
            this.global = global;
            this.local = local;

            this.fdSelect = fdSelect;
            this.perFontLocalSubroutines = perFontLocalSubroutines;

            isCid = true;
        }

        public (CompactFontFormatIndex global, CompactFontFormatIndex local) GetSubroutines(int glyphId)
        {
            if (!isCid)
            {
                return (global, local);
            }

            var fdIndex = fdSelect.GetFontDictionaryIndex(glyphId);

            if (fdIndex < 0 || fdIndex >= perFontLocalSubroutines.Count)
            {
                return (global, local);
            }

            var localPerFont = perFontLocalSubroutines[fdIndex];

            return (global, localPerFont ?? local);
        }
    }
}
