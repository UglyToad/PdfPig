namespace UglyToad.PdfPig.Parser
{
    using System;
    using System.Collections.Generic;
    using Annotations;
    using Content;
    using Exceptions;
    using Filters;
    using Geometry;
    using Graphics;
    using IO;
    using Logging;
    using Parts;
    using Tokenization.Scanner;
    using Tokens;
    using Util;
    using XObjects;

    internal class PageFactory : IPageFactory
    {
        private readonly IPdfTokenScanner pdfScanner;
        private readonly IResourceStore resourceStore;
        private readonly IFilterProvider filterProvider;
        private readonly IPageContentParser pageContentParser;
        private readonly XObjectFactory xObjectFactory;
        private readonly ILog log;

        public PageFactory(IPdfTokenScanner pdfScanner, IResourceStore resourceStore, IFilterProvider filterProvider,
            IPageContentParser pageContentParser,
            XObjectFactory xObjectFactory,
            ILog log)
        {
            this.resourceStore = resourceStore;
            this.filterProvider = filterProvider;
            this.pageContentParser = pageContentParser;
            this.xObjectFactory = xObjectFactory;
            this.log = log;
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
                throw new InvalidOperationException($"Page {number} had its type specified as {type} rather than 'Page'.");
            }

            MediaBox mediaBox = GetMediaBox(number, dictionary, pageTreeMembers, log, isLenientParsing);
            CropBox cropBox = GetCropBox(dictionary, pageTreeMembers, mediaBox, log, isLenientParsing);
            
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

                for (var i = 0; i < array.Data.Count; i++)
                {
                    var item = array.Data[i];

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

                    if (i < array.Data.Count - 1)
                    {
                        bytes.Add((byte)'\n');
                    }
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

            var page = new Page(number, dictionary, mediaBox, cropBox, content, new AnnotationProvider(pdfScanner, dictionary, isLenientParsing));

            return page;
        }

        private PageContent GetContent(IReadOnlyList<byte> contentBytes, CropBox cropBox, UserSpaceUnit userSpaceUnit, bool isLenientParsing)
        {
            var operations = pageContentParser.Parse(new ByteArrayInputBytes(contentBytes));

            var context = new ContentStreamProcessor(cropBox.Bounds, resourceStore, userSpaceUnit, isLenientParsing, pdfScanner, xObjectFactory, log);

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

        private static CropBox GetCropBox(DictionaryToken dictionary, PageTreeMembers pageTreeMembers, MediaBox mediaBox, ILog log, bool isLenientParsing)
        {
            CropBox cropBox;
            if (dictionary.TryGet(NameToken.CropBox, out var cropBoxObject) && cropBoxObject is ArrayToken cropBoxArray)
            {
                if (cropBoxArray.Length != 4 && isLenientParsing)
                {
                    log.Error($"The CropBox was the wrong length in the dictionary: {dictionary}. Array was: {cropBoxArray}.");
                    
                    cropBox = new CropBox(mediaBox.Bounds);

                    return cropBox;
                }

                cropBox = new CropBox(cropBoxArray.ToIntRectangle());
            }
            else
            {
                cropBox = pageTreeMembers.GetCropBox() ?? new CropBox(mediaBox.Bounds);
            }

            return cropBox;
        }

        private static MediaBox GetMediaBox(int number, DictionaryToken dictionary, PageTreeMembers pageTreeMembers, ILog log, bool isLenientParsing)
        {
            MediaBox mediaBox;
            if (dictionary.TryGet(NameToken.MediaBox, out var mediaboxObject) && mediaboxObject is ArrayToken mediaboxArray)
            {
                if (mediaboxArray.Length != 4 && isLenientParsing)
                {
                    log.Error($"The MediaBox was the wrong length in the dictionary: {dictionary}. Array was: {mediaboxArray}.");

                    mediaBox = MediaBox.A4;

                    return mediaBox;
                }

                mediaBox = new MediaBox(mediaboxArray.ToIntRectangle());
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
