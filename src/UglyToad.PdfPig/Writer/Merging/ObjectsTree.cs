namespace UglyToad.PdfPig.Writer.Merging
{
    using Content;
    using CrossReference;
    using Tokens;

    // I don't see the purpose of this class, @Elliot maybe you can explain to me the purpose
    internal class ObjectsTree
    {
        public TrailerDictionary TrailerDictionary { get; }

        public ObjectToken TrailerObject { get; }

        public Catalog Catalog { get; }

        public ObjectsTree(TrailerDictionary trailerDictionary, ObjectToken trailerObject,
            Catalog catalog)
        {
            TrailerDictionary = trailerDictionary;
            TrailerObject = trailerObject;
            Catalog = catalog;
        }
    }
}