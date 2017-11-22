namespace UglyToad.Pdf.Fonts
{
    using Cmap;
    using Cos;

    public interface IFont
    {
        CosName SubType { get; }

        string BaseFontType { get; }

        CMap ToUnicode { get; }
    }

    public class CompositeFont : IFont
    {
        public CosName SubType { get; }

        public string BaseFontType { get; }

        public CMap ToUnicode { get; }
    }
}
