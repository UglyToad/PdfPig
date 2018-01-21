namespace UglyToad.PdfPig.Content
{
    using Tokenization.Tokens;

    internal interface IPageFactory
    {
        Page Create(int number, DictionaryToken dictionary, PageTreeMembers pageTreeMembers, 
            bool isLenientParsing);

        void LoadResources(DictionaryToken dictionary, bool isLenientParsing);
    }
}