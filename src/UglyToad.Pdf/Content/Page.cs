namespace UglyToad.Pdf.Content
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using ContentStream;
    using Cos;
    using Filters;
    using Geometry;
    using Graphics;
    using IO;
    using Parser;
    using Util;

    public class Page
    {
        private readonly ParsingArguments parsingArguments;
        private readonly ContentStreamDictionary dictionary;

        /// <summary>
        /// The 1 indexed page number.
        /// </summary>
        public int Number { get; }

        public MediaBox MediaBox { get; }

        internal PageContent Content { get; }

        public IReadOnlyList<string> Text => Content?.Text ?? new string[0];

        internal Page(int number, ContentStreamDictionary dictionary, PageTreeMembers pageTreeMembers, ParsingArguments parsingArguments)
        {
            if (number <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(number), "Page number cannot be 0 or negative.");
            }
            
            this.dictionary = dictionary ?? throw new ArgumentNullException(nameof(dictionary));
            this.parsingArguments = parsingArguments ?? throw new ArgumentNullException(nameof(parsingArguments));

            Number = number;

            var type = dictionary.GetName(CosName.TYPE);

            if (type != null && !type.Equals(CosName.PAGE) && !parsingArguments.IsLenientParsing)
            {
                throw new InvalidOperationException($"Created page number {number} but its type was specified as {type} rather than 'Page'.");
            }

            if (dictionary.TryGetItemOfType(CosName.MEDIA_BOX, out COSArray mediaboxArray))
            {
                var x1 = mediaboxArray.getInt(0);
                var y1 = mediaboxArray.getInt(1);
                var x2 = mediaboxArray.getInt(2);
                var y2 = mediaboxArray.getInt(3);

                MediaBox = new MediaBox(new PdfRectangle(x1, y1, x2, y2));
            }
            else
            {
                MediaBox = pageTreeMembers.GetMediaBox();

                if (MediaBox == null)
                {
                    if (parsingArguments.IsLenientParsing)
                    {
                        MediaBox = MediaBox.A4;
                    }
                    else
                    {
                        throw new InvalidOperationException("No mediabox was present for page: " + number);
                    }
                }
            }

            if (dictionary.GetItemOrDefault(CosName.RESOURCES) is ContentStreamDictionary resource)
            {
                parsingArguments.CachingProviders.ResourceContainer.LoadResourceDictionary(resource, parsingArguments);
            }

            var contentObject = dictionary.GetItemOrDefault(CosName.CONTENTS) as CosObject;
            if (contentObject != null)
            {
                var contentStream = parsingArguments.Container.Get<DynamicParser>()
                    .Parse(parsingArguments, contentObject, false) as RawCosStream;

                if (contentStream == null)
                {
                    throw new InvalidOperationException("Failed to parse the content for the page: " + number);
                }

                var contents = contentStream.Decode(parsingArguments.Container.Get<IFilterProvider>());
                
                var operations = parsingArguments.Container.Get<PageContentParser>()
                    .Parse(parsingArguments.Container.Get<IGraphicsStateOperationFactory>(), new ByteArrayInputBytes(contents));

                var context = new ContentStreamProcessor(MediaBox.Bounds, parsingArguments.CachingProviders.ResourceContainer);

                var content = context.Process(operations);

                Content = content;
            }
        }
    }
}