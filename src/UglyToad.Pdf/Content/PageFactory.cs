namespace UglyToad.Pdf.Content
{
    using System;
    using ContentStream;
    using Cos;
    using Filters;
    using Geometry;
    using Graphics;
    using IO;
    using Parser;

    internal class PageFactory : IPageFactory
    {
        private readonly IResourceStore resourceStore;
        private readonly IPdfObjectParser pdfObjectParser;
        private readonly IFilterProvider filterProvider;
        private readonly IPageContentParser pageContentParser;

        public PageFactory(IResourceStore resourceStore, IPdfObjectParser pdfObjectParser, IFilterProvider filterProvider,
            IPageContentParser pageContentParser)
        {
            this.resourceStore = resourceStore;
            this.pdfObjectParser = pdfObjectParser;
            this.filterProvider = filterProvider;
            this.pageContentParser = pageContentParser;
        }

        public Page Create(int number, PdfDictionary dictionary, PageTreeMembers pageTreeMembers, IRandomAccessRead reader,
            bool isLenientParsing)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            var type = dictionary.GetName(CosName.TYPE);

            if (type != null && !type.Equals(CosName.PAGE) && !isLenientParsing)
            {
                throw new InvalidOperationException($"Page {number} had its type was specified as {type} rather than 'Page'.");
            }

            MediaBox mediaBox = GetMediaBox(number, dictionary, pageTreeMembers, isLenientParsing);
            CropBox cropBox = GetCropBox(dictionary, pageTreeMembers, mediaBox);

            if (dictionary.GetItemOrDefault(CosName.RESOURCES) is PdfDictionary resource)
            {
                resourceStore.LoadResourceDictionary(resource, reader, isLenientParsing);
            }

            UserSpaceUnit userSpaceUnit = GetUserSpaceUnits(dictionary);

            PageContent content = default(PageContent);

            var contentObject = dictionary.GetItemOrDefault(CosName.CONTENTS) as CosObject;
            if (contentObject != null)
            {
                var contentStream = pdfObjectParser.Parse(contentObject.ToIndirectReference(), reader, false) as PdfRawStream;

                if (contentStream == null)
                {
                    throw new InvalidOperationException("Failed to parse the content for the page: " + number);
                }

                var contents = contentStream.Decode(filterProvider);

                var operations = pageContentParser.Parse(new ByteArrayInputBytes(contents));

                var context = new ContentStreamProcessor(mediaBox.Bounds, resourceStore, userSpaceUnit);

                content = context.Process(operations);
            }

            var page = new Page(number, mediaBox, cropBox, content);

            return page;
        }

        private static UserSpaceUnit GetUserSpaceUnits(PdfDictionary dictionary)
        {
            var spaceUnits = UserSpaceUnit.Default;
            if (dictionary.TryGetValue(CosName.USER_UNIT, out var userUnitCosBase) && userUnitCosBase is ICosNumber userUnitNumber)
            {
                spaceUnits = new UserSpaceUnit(userUnitNumber.AsInt());
            }

            return spaceUnits;
        }

        private static CropBox GetCropBox(PdfDictionary dictionary, PageTreeMembers pageTreeMembers, MediaBox mediaBox)
        {
            CropBox cropBox;
            if (dictionary.TryGetItemOfType(CosName.CROP_BOX, out COSArray cropBoxArray))
            {
                var x1 = cropBoxArray.getInt(0);
                var y1 = cropBoxArray.getInt(1);
                var x2 = cropBoxArray.getInt(2);
                var y2 = cropBoxArray.getInt(3);

                cropBox = new CropBox(new PdfRectangle(x1, y1, x2, y2));
            }
            else
            {
                cropBox = pageTreeMembers.GetCropBox() ?? new CropBox(mediaBox.Bounds);
            }

            return cropBox;
        }

        private static MediaBox GetMediaBox(int number, PdfDictionary dictionary, PageTreeMembers pageTreeMembers, bool isLenientParsing)
        {
            MediaBox mediaBox;
            if (dictionary.TryGetItemOfType(CosName.MEDIA_BOX, out COSArray mediaboxArray))
            {
                var x1 = mediaboxArray.getInt(0);
                var y1 = mediaboxArray.getInt(1);
                var x2 = mediaboxArray.getInt(2);
                var y2 = mediaboxArray.getInt(3);

                mediaBox = new MediaBox(new PdfRectangle(x1, y1, x2, y2));
            }
            else
            {
                mediaBox = pageTreeMembers.GetMediaBox();

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
