namespace UglyToad.Pdf.Content
{
    using ContentStream;
    using Cos;
    using Fonts;
    using IO;

    internal interface IResourceStore
    {
        void LoadResourceDictionary(PdfDictionary dictionary, IRandomAccessRead reader, bool isLenientParsing);

        IFont GetFont(CosName name);
    }
}