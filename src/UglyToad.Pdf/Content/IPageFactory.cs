namespace UglyToad.Pdf.Content
{
    using ContentStream;
    using IO;

    internal interface IPageFactory
    {
        Page Create(int number, PdfDictionary dictionary, PageTreeMembers pageTreeMembers, IRandomAccessRead reader,
            bool isLenientParsing);
    }
}