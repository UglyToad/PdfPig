namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.StartFinishOutline
{
    /// <summary>
    /// Sets left sidebearing and the character width vector.
    /// This command also sets the current point to(sbx, sby), but does not place the point in the character path.
    /// </summary>
    internal class SbwCommand
    {
        public const string Name = "sbw";

        public static readonly byte First = 12;
        public static readonly byte? Second = 7;

        public bool TakeFromStackBottom { get; } = true;
        public bool ClearsOperandStack { get; } = true;

        /// <summary>
        /// The X value of the left sidebearing point.
        /// </summary>
        public int LeftSidebearingX { get; set; }

        /// <summary>
        /// The Y value of the left sidebearing point.
        /// </summary>
        public int LeftSidebearingY { get; set; }

        /// <summary>
        /// The X value of the character width vector.
        /// </summary>
        public int CharacterWidthX { get; }

        /// <summary>
        /// The Y value of the character width vector.
        /// </summary>
        public int CharacterWidthY { get; }

        /// <summary>
        /// Create a new <see cref="SbwCommand"/>.
        /// </summary>
        /// <param name="leftSidebearingX">The X value of the left sidebearing point.</param>
        /// <param name="leftSidebearingY">The Y value of the left sidebearing point.</param>
        /// <param name="characterWidthX">The X value of the character width vector.</param>
        /// <param name="characterWidthY">The Y value of the character width vector.</param>
        public SbwCommand(int leftSidebearingX, int leftSidebearingY, int characterWidthX, int characterWidthY)
        {
            LeftSidebearingX = leftSidebearingX;
            LeftSidebearingY = leftSidebearingY;
            CharacterWidthX = characterWidthX;
            CharacterWidthY = characterWidthY;
        }

        public override string ToString()
        {
            return $"{LeftSidebearingX} {LeftSidebearingY} {CharacterWidthX} {CharacterWidthY} {Name}";
        }
    }
}
