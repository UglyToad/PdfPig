namespace UglyToad.PdfPig.Parser
{
    using System;
    using System.Collections.Generic;
    using Annotations;
    using Content;
    using Core;
    using Filters;
    using Geometry;
    using Graphics;
    using Graphics.Operations;
    using Logging;
    using Outline.Destinations;
    using Parts;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal class PageFactory : IPageFactory
    {
        private readonly ParsingOptions parsingOptions;
        private readonly IPdfTokenScanner pdfScanner;
        private readonly IResourceStore resourceStore;
        private readonly ILookupFilterProvider filterProvider;
        private readonly IPageContentParser pageContentParser;

        public PageFactory(
            IPdfTokenScanner pdfScanner,
            IResourceStore resourceStore,
            ILookupFilterProvider filterProvider,
            IPageContentParser pageContentParser,
            ParsingOptions parsingOptions)
        {
            this.resourceStore = resourceStore;
            this.filterProvider = filterProvider;
            this.pageContentParser = pageContentParser;
            this.pdfScanner = pdfScanner;
            this.parsingOptions = parsingOptions;
        }

        public Page Create(int number, DictionaryToken dictionary, PageTreeMembers pageTreeMembers,
            NamedDestinations namedDestinations)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            var type = dictionary.GetNameOrDefault(NameToken.Type);

            if (type != null && !type.Equals(NameToken.Page))
            {
                parsingOptions.Logger.Error($"Page {number} had its type specified as {type} rather than 'Page'.");
            }

            var rotation = new PageRotationDegrees(pageTreeMembers.Rotation);
            if (dictionary.TryGet(NameToken.Rotate, pdfScanner, out NumericToken rotateToken))
            {
                rotation = new PageRotationDegrees(rotateToken.Int);
            }

            var stackDepth = 0;

            while (pageTreeMembers.ParentResources.Count > 0)
            {
                var resource = pageTreeMembers.ParentResources.Dequeue();

                resourceStore.LoadResourceDictionary(resource);
                stackDepth++;
            }

            if (dictionary.TryGet(NameToken.Resources, pdfScanner, out DictionaryToken resources))
            {
                resourceStore.LoadResourceDictionary(resources);
                stackDepth++;
            }

            UserSpaceUnit userSpaceUnit = GetUserSpaceUnits(dictionary);

            MediaBox mediaBox = GetMediaBox(number, dictionary, pageTreeMembers);
            CropBox cropBox = GetCropBox(dictionary, pageTreeMembers, mediaBox);

            var initialMatrix = OperationContextHelper.GetInitialMatrix(userSpaceUnit, mediaBox, cropBox, rotation, parsingOptions.Logger);

            ApplyTransformNormalise(initialMatrix, ref mediaBox, ref cropBox);

            PageContent content;

            if (!dictionary.TryGet(NameToken.Contents, out var contents))
            {
                content = new PageContent(EmptyArray<IGraphicsStateOperation>.Instance,
                    EmptyArray<Letter>.Instance,
                    EmptyArray<PdfPath>.Instance,
                    EmptyArray<Union<XObjectContentRecord, InlineImage>>.Instance,
                    EmptyArray<MarkedContentElement>.Instance,
                    pdfScanner,
                    filterProvider,
                    resourceStore);
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

                    bytes.AddRange(contentStream.Decode(filterProvider, pdfScanner));

                    if (i < array.Data.Count - 1)
                    {
                        bytes.Add((byte)'\n');
                    }
                }

                content = GetContent(number, bytes, cropBox, userSpaceUnit, rotation, initialMatrix, parsingOptions);
            }
            else
            {
                var contentStream = DirectObjectFinder.Get<StreamToken>(contents, pdfScanner);

                if (contentStream == null)
                {
                    throw new InvalidOperationException("Failed to parse the content for the page: " + number);
                }

                var bytes = contentStream.Decode(filterProvider, pdfScanner);

                content = GetContent(number, bytes, cropBox, userSpaceUnit, rotation, initialMatrix, parsingOptions);
            }

            var annotationProvider = new AnnotationProvider(pdfScanner, dictionary, initialMatrix, namedDestinations, parsingOptions.Logger);
            var page = new Page(number, dictionary, mediaBox, cropBox, rotation, content, annotationProvider, pdfScanner);

            for (var i = 0; i < stackDepth; i++)
            {
                resourceStore.UnloadResourceDictionary();
            }

            return page;
        }

        private PageContent GetContent(
            int pageNumber,
            IReadOnlyList<byte> contentBytes,
            CropBox cropBox,
            UserSpaceUnit userSpaceUnit,
            PageRotationDegrees rotation,
            TransformationMatrix initialMatrix,
            ParsingOptions parsingOptions)
        {
            var operations = pageContentParser.Parse(pageNumber, new ByteArrayInputBytes(contentBytes),
                parsingOptions.Logger);

            var context = new ContentStreamProcessor(
                pageNumber,
                resourceStore,
                userSpaceUnit,
                cropBox,
                initialMatrix,
                rotation,
                pdfScanner,
                pageContentParser,
                filterProvider,
                parsingOptions);

            return context.Process(pageNumber, operations);
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

        private CropBox GetCropBox(
            DictionaryToken dictionary,
            PageTreeMembers pageTreeMembers,
            MediaBox mediaBox)
        {
            CropBox cropBox;
            if (dictionary.TryGet(NameToken.CropBox, out var cropBoxObject) &&
                DirectObjectFinder.TryGet(cropBoxObject, pdfScanner, out ArrayToken cropBoxArray))
            {
                if (cropBoxArray.Length != 4)
                {
                    parsingOptions.Logger.Error($"The CropBox was the wrong length in the dictionary: {dictionary}. Array was: {cropBoxArray}. Using MediaBox.");

                    cropBox = new CropBox(mediaBox.Bounds);

                    return cropBox;
                }

                cropBox = new CropBox(cropBoxArray.ToRectangle(pdfScanner));
            }
            else
            {
                cropBox = pageTreeMembers.GetCropBox() ?? new CropBox(mediaBox.Bounds);
            }

            return cropBox;
        }

        private MediaBox GetMediaBox(
            int number,
            DictionaryToken dictionary,
            PageTreeMembers pageTreeMembers)
        {
            MediaBox mediaBox;
            if (dictionary.TryGet(NameToken.MediaBox, out var mediaBoxObject)
                && DirectObjectFinder.TryGet(mediaBoxObject, pdfScanner, out ArrayToken mediaBoxArray))
            {
                if (mediaBoxArray.Length != 4)
                {
                    parsingOptions.Logger.Error($"The MediaBox was the wrong length in the dictionary: {dictionary}. Array was: {mediaBoxArray}. Defaulting to US Letter.");

                    mediaBox = MediaBox.Letter;

                    return mediaBox;
                }

                mediaBox = new MediaBox(mediaBoxArray.ToRectangle(pdfScanner));
            }
            else
            {
                mediaBox = pageTreeMembers.MediaBox;

                if (mediaBox == null)
                {
                    parsingOptions.Logger.Error($"The MediaBox was the wrong missing for page {number}. Using US Letter.");

                    // PDFBox defaults to US Letter.
                    mediaBox = MediaBox.Letter;
                }
            }

            return mediaBox;
        }

        /// <summary>
        /// Apply the matrix transform to the media box and crop box.
        /// Then Normalise() in order to obtain rectangles with rotation=0
        /// and width and height as viewed on screen.
        /// </summary>
        /// <param name="transformationMatrix"></param>
        /// <param name="mediaBox"></param>
        /// <param name="cropBox"></param>
        private static void ApplyTransformNormalise(TransformationMatrix transformationMatrix, ref MediaBox mediaBox, ref CropBox cropBox)
        {
            if (transformationMatrix != TransformationMatrix.Identity)
            {
                mediaBox = new MediaBox(transformationMatrix.Transform(mediaBox.Bounds).Normalise());
                cropBox = new CropBox(transformationMatrix.Transform(cropBox.Bounds).Normalise());
            }
        }
    }
}
