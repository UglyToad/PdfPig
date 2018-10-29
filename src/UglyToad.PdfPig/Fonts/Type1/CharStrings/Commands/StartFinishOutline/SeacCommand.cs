namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.StartFinishOutline
{
    /// <summary>
    /// Standard encoding accented character.
    /// Makes an accented character from two other characters in the font program.
    /// </summary>
    internal class SeacCommand
    {
        public const string Name = "seac";

        public static readonly byte First = 12;
        public static readonly byte? Second = 6;

        public bool TakeFromStackBottom { get; } = true;
        public bool ClearsOperandStack { get; } = true;

        /// <summary>
        /// The x component of the left sidebearing of the accent.
        /// </summary>
        public int AccentLeftSidebearingX { get; set; }

        /// <summary>
        /// The x position of the origin of the accent character relative to the base character.
        /// </summary>
        public int AccentOriginX { get; set; }

        /// <summary>
        /// The y position of the origin of the accent character relative to the base character.
        /// </summary>
        public int AccentOriginY { get; set; }

        /// <summary>
        /// The character code of the base character.
        /// </summary>
        public int BaseCharacterCode { get; set; }

        /// <summary>
        /// The character code of the accent character.
        /// </summary>
        public int AccentCharacterCode { get; set; }

        /// <summary>
        /// Create a new <see cref="SeacCommand"/>.
        /// </summary>
        /// <param name="accentLeftSidebearingX">The x component of the left sidebearing of the accent.</param>
        /// <param name="accentOriginX">The x position of the origin of the accent character relative to the base character.</param>
        /// <param name="accentOriginY">The y position of the origin of the accent character relative to the base character.</param>
        /// <param name="baseCharacterCode">The character code of the base character.</param>
        /// <param name="accentCharacterCode">The character code of the accent character.</param>
        public SeacCommand(int accentLeftSidebearingX, int accentOriginX, int accentOriginY, int baseCharacterCode, int accentCharacterCode)
        {
            AccentLeftSidebearingX = accentLeftSidebearingX;
            AccentOriginX = accentOriginX;
            AccentOriginY = accentOriginY;
            BaseCharacterCode = baseCharacterCode;
            AccentCharacterCode = accentCharacterCode;
        }

        public override string ToString()
        {
            return $"{AccentLeftSidebearingX} {AccentOriginX} {AccentOriginY} {BaseCharacterCode} {AccentCharacterCode} {Name}";
        }
    }
}
