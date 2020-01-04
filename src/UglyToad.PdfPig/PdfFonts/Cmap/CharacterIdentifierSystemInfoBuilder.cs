namespace UglyToad.PdfPig.PdfFonts.Cmap
{
    internal class CharacterIdentifierSystemInfoBuilder
    {
        private string registry;
        public string Registry
        {
            get => registry;
            set
            {
                registry = value;
                HasRegistry = true;
            }
        }

        public bool HasRegistry { get; private set; }

        private string ordering;
        public string Ordering
        {
            get => ordering;
            set
            {
                ordering = value;
                HasOrdering = true;
            }
        }

        public bool HasOrdering { get; private set; }

        private int supplement;
        public int Supplement
        {
            get => supplement;
            set
            {
                supplement = value;
                HasSupplement = true;
            }
        }

        public bool HasSupplement { get; private set; }
    }
}