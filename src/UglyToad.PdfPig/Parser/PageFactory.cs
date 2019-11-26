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

    internal class PageFactory : IPageFactory
    {
        private readonly IPdfTokenScanner pdfScanner;
        private readonly IResourceStore resourceStore;
        private readonly IFilterProvider filterProvider;
        private readonly IPageContentParser pageContentParser;
        private readonly ILog log;

        public PageFactory(IPdfTokenScanner pdfScanner, IResourceStore resourceStore, IFilterProvider filterProvider,
            IPageContentParser pageContentParser,
            ILog log)
        {
            this.resourceStore = resourceStore;
            this.filterProvider = filterProvider;
            this.pageContentParser = pageContentParser;
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

            var rotation = new PageRotationDegrees(pageTreeMembers.Rotation);
            if (dictionary.TryGet(NameToken.Rotate, pdfScanner, out NumericToken rotateToken))
            {
                rotation = new PageRotationDegrees(rotateToken.Int);
            }

            MediaBox mediaBox = GetMediaBox(number, dictionary, pageTreeMembers, isLenientParsing);
            CropBox cropBox = GetCropBox(dictionary, pageTreeMembers, mediaBox, isLenientParsing);

            var stackDepth = 0;

            while (pageTreeMembers.ParentResources.Count > 0)
            {
                var resource = pageTreeMembers.ParentResources.Dequeue();

                resourceStore.LoadResourceDictionary(resource, isLenientParsing);
                stackDepth++;
            }

            if (dictionary.TryGet(NameToken.Resources, pdfScanner, out DictionaryToken resources))
            {
                resourceStore.LoadResourceDictionary(resources, isLenientParsing);
                stackDepth++;
            }
            
            UserSpaceUnit userSpaceUnit = GetUserSpaceUnits(dictionary);

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
                
                content = GetContent(bytes, cropBox, userSpaceUnit, rotation, isLenientParsing);
            }
            else
            {
                var contentStream = DirectObjectFinder.Get<StreamToken>(contents, pdfScanner);

                if (contentStream == null)
                {
                    throw new InvalidOperationException("Failed to parse the content for the page: " + number);
                }

                var bytes = contentStream.Decode(filterProvider);

                content = GetContent(bytes, cropBox, userSpaceUnit, rotation, isLenientParsing);
            }

            var page = new Page(number, dictionary, mediaBox, cropBox, rotation, content, new AnnotationProvider(pdfScanner, dictionary, isLenientParsing));

            for (var i = 0; i < stackDepth; i++)
            {
                resourceStore.UnloadResourceDictionary();
            }

            return page;
        }

        private PageContent GetContent(IReadOnlyList<byte> contentBytes, CropBox cropBox, UserSpaceUnit userSpaceUnit, PageRotationDegrees rotation, bool isLenientParsing)
        {
            var operations = pageContentParser.Parse(new ByteArrayInputBytes(contentBytes));

            var context = new ContentStreamProcessor(cropBox.Bounds, resourceStore, userSpaceUnit, rotation, isLenientParsing, pdfScanner, 
                pageContentParser,
                filterProvider, 
                log);

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

        private CropBox GetCropBox(DictionaryToken dictionary, PageTreeMembers pageTreeMembers, MediaBox mediaBox, bool isLenientParsing)
        {
            CropBox cropBox;
            if (dictionary.TryGet(NameToken.CropBox, out var cropBoxObject) &&
                DirectObjectFinder.TryGet(cropBoxObject, pdfScanner, out ArrayToken cropBoxArray))
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

        private MediaBox GetMediaBox(int number, DictionaryToken dictionary, PageTreeMembers pageTreeMembers, bool isLenientParsing)
        {
            MediaBox mediaBox;
            if (dictionary.TryGet(NameToken.MediaBox, out var mediaboxObject) 
                && DirectObjectFinder.TryGet(mediaboxObject, pdfScanner, out ArrayToken mediaboxArray))
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
    }
}
