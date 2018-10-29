namespace UglyToad.PdfPig.Fonts.Type1.CharStrings.Commands.StartFinishOutline
{
    /// <summary>
    /// The name hsbw stands for horizontal sidebearing and width; 
    /// horizontal indicates that the y component of both the sidebearing and width is 0. 
    /// This command sets the left sidebearing point at (sbx, 0) and sets the character width vector to(wx, 0) in character space.
    /// This command also sets the current point to (sbx, 0), but does not place the point in the character path.
    /// </summary>
    internal class HsbwCommand
    {
        public const string Name = "hsbw";

        public static readonly byte First = 13;
        public static readonly byte? Second = null;

        public bool TakeFromStackBottom { get; } = true;
        public bool ClearsOperandStack { get; } = true;

        public int LeftSidebearingPointX { get; }

        public int CharacterWidthVectorX { get; }

        public HsbwCommand(int leftSidebearingPointX, int characterWidthVectorX)
        {
            LeftSidebearingPointX = leftSidebearingPointX;
            CharacterWidthVectorX = characterWidthVectorX;
        }
    }
}
