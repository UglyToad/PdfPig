namespace UglyToad.PdfPig.Content
{
    using Tokens;

    internal interface IPageFactory
    {
        Page Create(int number, DictionaryToken dictionary, PageTreeMembers pageTreeMembers, 
            bool isLenientParsing);
    }
}