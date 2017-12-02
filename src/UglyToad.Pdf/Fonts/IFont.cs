namespace UglyToad.Pdf.Fonts
{
    using Cmap;
    using Cos;
    using Geometry;
    using IO;

    internal interface IFont
    {
        CosName Name { get; }

        CosName SubType { get; }

        string BaseFontType { get; }

        bool IsVertical { get; }

        int ReadCharacterCode(IInputBytes bytes, out int codeLength);

        string GetUnicode(int characterCode);

        PdfVector GetDisplacement(int characterCode);
    }

    internal class CompositeFont : IFont
    {
        public CosName Name { get; }

        public CosName SubType { get; }

        public string BaseFontType { get; }
        public bool IsVertical { get; }

        public CMap ToUnicode { get; }

        public int ReadCharacterCode(IInputBytes bytes, out int codeLength)
        {
            throw new System.NotImplementedException();
        }

        public string GetUnicode(int characterCode)
        {
            throw new System.NotImplementedException();
        }

        public PdfVector GetDisplacement(int characterCode)
        {
            throw new System.NotImplementedException();
        }
    }
}
