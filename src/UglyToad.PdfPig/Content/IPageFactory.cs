namespace UglyToad.PdfPig.Content
{
    using Outline.Destinations;
    using Tokens;

    internal interface IPageFactory
    {
        Page Create(int number,
            DictionaryToken dictionary,
            PageTreeMembers pageTreeMembers,
            NamedDestinations annotationProvider,
            InternalParsingOptions parsingOptions);
    }
}