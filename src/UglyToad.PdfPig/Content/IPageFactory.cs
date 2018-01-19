namespace UglyToad.PdfPig.Content
{
    using IO;
    using Tokenization.Tokens;

    internal interface IPageFactory
    {
        Page Create(int number, DictionaryToken dictionary, PageTreeMembers pageTreeMembers, IRandomAccessRead reader,
            bool isLenientParsing);

        void LoadResources(DictionaryToken dictionary, bool isLenientParsing);
    }
}