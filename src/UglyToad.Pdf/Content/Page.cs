namespace UglyToad.Pdf.Content
{
    using System;
    using ContentStream;
    using ContentStream.TypedAccessors;
    using Cos;
    using Filters;
    using Logging;
    using Parser;
    using Text;
    using Util;

    public class Page
    {
        private readonly ParsingArguments parsingArguments;
        private readonly ContentStreamDictionary dictionary;
        public int Number { get; }

        public bool Loaded { get; private set; }

        internal Page(int number, ContentStreamDictionary dictionary, ParsingArguments parsingArguments)
        {
            if (number <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(number), "Page number cannot be 0 or negative.");
            }

            Number = number;
            Loaded = false;
            this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            this.parsingArguments = parsingArguments ?? throw new ArgumentNullException(nameof(parsingArguments));

            var mediabox = dictionary.GetDictionaryObject(CosName.MEDIA_BOX) as COSArray;
            var contents = dictionary.GetItemOrDefault(CosName.CONTENTS);
            var raw = contents as RawCosStream;
            var obj = parsingArguments.CachingProviders.ObjectPool.Get(new CosObjectKey(7, 0));
            var parser = parsingArguments.Container.Get<DynamicParser>()
                .Parse(parsingArguments, obj, false) as RawCosStream;
            var rw = parser.Decode(parsingArguments.Container.Get<IFilterProvider>());
            var format = OtherEncodings.BytesAsLatin1String(rw);
            var pee = new TextSectionParser(new NoOpLog()).ReadTextObjects(new ByteTextScanner(rw));
            var font0 = parsingArguments.CachingProviders.ObjectPool.Get(new CosObjectKey(16, 0));
            var cmpa = parsingArguments.CachingProviders.ObjectPool.Get(new CosObjectKey(9, 0));
            var toad = parsingArguments.Container.Get<DynamicParser>()
                .Parse(parsingArguments, new CosObjectKey(9, 0), false);
            var bigsby = (toad as RawCosStream).Decode(parsingArguments.Container.Get<IFilterProvider>());

            var ssss = OtherEncodings.BytesAsLatin1String(bigsby);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    ///  The positive x axis extends horizontally to the right and the positive y axis vertically upward, as in standard mathematical practice
    /// </remarks>
    public struct Rectangle
    {
        public decimal Width { get; }

        public decimal Height { get; }

        public decimal Left { get; }

        public decimal Top { get; }

        public decimal Right { get; }

        public decimal Bottom { get; }

        public Rectangle(decimal x1, decimal y1, decimal x2, decimal y2)
        {
            Width = 0;
            Height = 0;
            Top = 0;
            Left = 0;
            Right = 0;
            Bottom = 0;
        }
    }

    public struct Coordinate
    {
        public decimal X { get; set; }

        public decimal Y { get; set; }
    }
}