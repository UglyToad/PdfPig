namespace UglyToad.PdfPig.Parser
{
    using System;
    using System.Collections.Generic;
    using Content;
    using Exceptions;
    using Filters;
    using Geometry;
    using Graphics;
    using IO;
    using Parts;
    using Tokenization.Scanner;
    using Tokens;
    using Util;
    using XObjects;

    internal class PageFactory : IPageFactory
    {
        private readonly IResourceStore resourceStore;
        private readonly IFilterProvider filterProvider;
        private readonly IPageContentParser pageContentParser;
        private readonly XObjectFactory xObjectFactory;
        private readonly IPdfTokenScanner pdfScanner;

        public PageFactory(IPdfTokenScanner pdfScanner, IResourceStore resourceStore, IFilterProvider filterProvider,
            IPageContentParser pageContentParser,
            XObjectFactory xObjectFactory)
        {
            this.resourceStore = resourceStore;
            this.filterProvider = filterProvider;
            this.pageContentParser = pageContentParser;
            this.xObjectFactory = xObjectFactory;
            this.pdfScanner = pdfScanner;
        }

        public Page Create(int number, DictionaryToken dictionary, PageTreeMembers pageTreeMembers,
            bool isLenientParsing)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            var type = dictionary.GetNameOrDefault(NameToken.Type);

            if (type != null && !type.Equals(NameToken.Page) && !isLenientParsing)
            {
                throw new InvalidOperationException($"Page {number} had its type was specified as {type} rather than 'Page'.");
            }

            MediaBox mediaBox = GetMediaBox(number, dictionary, pageTreeMembers, isLenientParsing);
            CropBox cropBox = GetCropBox(dictionary, pageTreeMembers, mediaBox);
            
            UserSpaceUnit userSpaceUnit = GetUserSpaceUnits(dictionary);

            LoadResources(dictionary, isLenientParsing);

            PageContent content = default(PageContent);

            if (!dictionary.TryGet(NameToken.Contents, out var contents))
            {
                 // ignored for now, is it possible? check the spec...
            }
            else if (DirectObjectFinder.TryGet<ArrayToken>(contents, pdfScanner, out var array))
            {
                var bytes = new List<byte>();
                
                foreach (var item in array.Data)
                {
                    if (!(item is IndirectReferenceToken obj))
                    {
                        throw new PdfDocumentFormatException($"The contents contained something which was not an indirect reference: {item}.");
                    }

                    var contentStream = DirectObjectFinder.Get<StreamToken>(obj, pdfScanner);
                    
                    if (contentStream == null)
                    {
                        throw new InvalidOperationException($"Could not find the contents for object {obj}.");
                    }

                    bytes.AddRange(contentStream.Decode(filterProvider));
                }

                content = GetContent(bytes, cropBox, userSpaceUnit, isLenientParsing);
            }
            else
            {
                var contentStream = DirectObjectFinder.Get<StreamToken>(contents, pdfScanner);

                if (contentStream == null)
                {
                    throw new InvalidOperationException("Failed to parse the content for the page: " + number);
                }

                var bytes = contentStream.Decode(filterProvider);

                content = GetContent(bytes, cropBox, userSpaceUnit, isLenientParsing);
            }

            var page = new Page(number, mediaBox, cropBox, content);

            return page;
        }

        private PageContent GetContent(IReadOnlyList<byte> contentBytes, CropBox cropBox, UserSpaceUnit userSpaceUnit, bool isLenientParsing)
        {
            var operations = pageContentParser.Parse(new ByteArrayInputBytes(contentBytes));

            var context = new ContentStreamProcessor(cropBox.Bounds, resourceStore, userSpaceUnit, isLenientParsing, pdfScanner, xObjectFactory);

            return context.Process(operations);
        }

        private static UserSpaceUnit GetUserSpaceUnits(DictionaryToken dictionary)
        {
            var spaceUnits = UserSpaceUnit.Default;
            if (dictionary.TryGet(NameToken.UserUnit, out var userUnitBase) && userUnitBase is NumericToken userUnitNumber)
            {
                spaceUnits = new UserSpaceUnit(userUnitNumber.Int);
            }

            return spaceUnits;
        }

        private static CropBox GetCropBox(DictionaryToken dictionary, PageTreeMembers pageTreeMembers, MediaBox mediaBox)
        {
            CropBox cropBox;
            if (dictionary.TryGet(NameToken.CropBox, out var cropBoxObject) && cropBoxObject is ArrayToken cropBoxArray)
            {
                var x1 = cropBoxArray.GetNumeric(0).Int;
                var y1 = cropBoxArray.GetNumeric(1).Int;
                var x2 = cropBoxArray.GetNumeric(2).Int;
                var y2 = cropBoxArray.GetNumeric(3).Int;

                cropBox = new CropBox(new PdfRectangle(x1, y1, x2, y2));
            }
            else
            {
                cropBox = pageTreeMembers.GetCropBox() ?? new CropBox(mediaBox.Bounds);
            }

            return cropBox;
        }

        private static MediaBox GetMediaBox(int number, DictionaryToken dictionary, PageTreeMembers pageTreeMembers, bool isLenientParsing)
        {
            MediaBox mediaBox;
            if (dictionary.TryGet(NameToken.MediaBox, out var mediaboxObject) && mediaboxObject is ArrayToken mediaboxArray)
            {
                var x1 = mediaboxArray.GetNumeric(0).Int;
                var y1 = mediaboxArray.GetNumeric(1).Int;
                var x2 = mediaboxArray.GetNumeric(2).Int;
                var y2 = mediaboxArray.GetNumeric(3).Int;

                mediaBox = new MediaBox(new PdfRectangle(x1, y1, x2, y2));
            }
            else
            {
                mediaBox = pageTreeMembers.MediaBox;

                if (mediaBox == null)
                {
                    if (isLenientParsing)
                    {
                        mediaBox = MediaBox.A4;
                    }
                    else
                    {
                        throw new InvalidOperationException("No mediabox was present for page: " + number);
                    }
                }
            }

            return mediaBox;
        }

        public void LoadResources(DictionaryToken dictionary, bool isLenientParsing)
        {
            if (!dictionary.TryGet(NameToken.Resources, out var token))
            {
                return;
            }

            var resources = DirectObjectFinder.Get<DictionaryToken>(token, pdfScanner);

            resourceStore.LoadResourceDictionary(resources, isLenientParsing);
        }
    }
}
