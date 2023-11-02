namespace UglyToad.PdfPig.Content
{
    using System;
    using System.Collections.Generic;
    using Core;
    using Filters;
    using Geometry;
    using Graphics;
    using Graphics.Operations;
    using Outline.Destinations;
    using Parser;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    /// <summary>
    /// Page factory abstract class.
    /// </summary>
    /// <typeparam name="TPage">The type of page the page factory creates.</typeparam>
    public abstract class BasePageFactory<TPage> : IPageFactory<TPage>
    {
        /// <summary>
        /// The parsing options.
        /// </summary>
        public readonly ParsingOptions ParsingOptions;

        /// <summary>
        /// The Pdf token scanner.
        /// </summary>
        public readonly IPdfTokenScanner PdfScanner;

        /// <summary>
        /// The resource store.
        /// </summary>
        public readonly IResourceStore ResourceStore;

        /// <summary>
        /// The filter provider.
        /// </summary>
        public readonly ILookupFilterProvider FilterProvider;

        /// <summary>
        /// The page content parser.
        /// </summary>
        public readonly IPageContentParser PageContentParser;

        /// <summary>
        /// Create a <see cref="BasePageFactory{TPage}"/>.
        /// </summary>
        protected BasePageFactory(
            IPdfTokenScanner pdfScanner,
            IResourceStore resourceStore,
            ILookupFilterProvider filterProvider,
            IPageContentParser pageContentParser,
            ParsingOptions parsingOptions)
        {
            this.ResourceStore = resourceStore;
            this.FilterProvider = filterProvider;
            this.PageContentParser = pageContentParser;
            this.PdfScanner = pdfScanner;
            this.ParsingOptions = parsingOptions;
        }

        /// <inheritdoc/>
        public TPage Create(int number, DictionaryToken dictionary, PageTreeMembers pageTreeMembers,
             NamedDestinations namedDestinations)
        {
            if (dictionary == null)
            {
                throw new ArgumentNullException(nameof(dictionary));
            }

            var type = dictionary.GetNameOrDefault(NameToken.Type);

            if (type != null && !type.Equals(NameToken.Page))
            {
                ParsingOptions.Logger.Error($"Page {number} had its type specified as {type} rather than 'Page'.");
            }

            var rotation = new PageRotationDegrees(pageTreeMembers.Rotation);
            if (dictionary.TryGet(NameToken.Rotate, PdfScanner, out NumericToken rotateToken))
            {
                rotation = new PageRotationDegrees(rotateToken.Int);
            }

            var stackDepth = 0;

            while (pageTreeMembers.ParentResources.Count > 0)
            {
                var resource = pageTreeMembers.ParentResources.Dequeue();

                ResourceStore.LoadResourceDictionary(resource);
                stackDepth++;
            }

            if (dictionary.TryGet(NameToken.Resources, PdfScanner, out DictionaryToken resources))
            {
                ResourceStore.LoadResourceDictionary(resources);
                stackDepth++;
            }

            UserSpaceUnit userSpaceUnit = GetUserSpaceUnits(dictionary);

            MediaBox mediaBox = GetMediaBox(number, dictionary, pageTreeMembers);
            CropBox cropBox = GetCropBox(dictionary, pageTreeMembers, mediaBox);

            var initialMatrix = OperationContextHelper.GetInitialMatrix(userSpaceUnit, mediaBox, cropBox, rotation, ParsingOptions.Logger);

            ApplyTransformNormalise(initialMatrix, ref mediaBox, ref cropBox);

            TPage page;

            if (!dictionary.TryGet(NameToken.Contents, out var contents))
            {
                // ignored for now, is it possible? check the spec...
                page = ProcessPageInternal(number, dictionary, namedDestinations, mediaBox, cropBox, userSpaceUnit, rotation, initialMatrix, null);
            }
            else if (DirectObjectFinder.TryGet<ArrayToken>(contents, PdfScanner, out var array))
            {
                var bytes = new List<byte>();

                for (var i = 0; i < array.Data.Count; i++)
                {
                    var item = array.Data[i];

                    if (!(item is IndirectReferenceToken obj))
                    {
                        throw new PdfDocumentFormatException($"The contents contained something which was not an indirect reference: {item}.");
                    }

                    var contentStream = DirectObjectFinder.Get<StreamToken>(obj, PdfScanner);

                    if (contentStream == null)
                    {
                        throw new InvalidOperationException($"Could not find the contents for object {obj}.");
                    }

                    bytes.AddRange(contentStream.Decode(FilterProvider, PdfScanner));

                    if (i < array.Data.Count - 1)
                    {
                        bytes.Add((byte)'\n');
                    }
                }

                page = ProcessPageInternal(number, dictionary, namedDestinations, mediaBox, cropBox, userSpaceUnit, rotation, initialMatrix, bytes);
            }
            else
            {
                var contentStream = DirectObjectFinder.Get<StreamToken>(contents, PdfScanner);

                if (contentStream == null)
                {
                    throw new InvalidOperationException("Failed to parse the content for the page: " + number);
                }

                var bytes = contentStream.Decode(FilterProvider, PdfScanner);

                page = ProcessPageInternal(number, dictionary, namedDestinations, mediaBox, cropBox, userSpaceUnit, rotation, initialMatrix, bytes);
            }

            for (var i = 0; i < stackDepth; i++)
            {
                ResourceStore.UnloadResourceDictionary();
            }

            return page;
        }

        private TPage ProcessPageInternal(
            int pageNumber,
            DictionaryToken dictionary,
            NamedDestinations namedDestinations,
            MediaBox mediaBox,
            CropBox cropBox,
            UserSpaceUnit userSpaceUnit,
            PageRotationDegrees rotation,
            TransformationMatrix initialMatrix,
            IReadOnlyList<byte> contentBytes)
        {
            IReadOnlyList<IGraphicsStateOperation> operations;

            if (contentBytes == null || contentBytes.Count == 0)
            {
                operations = EmptyArray<IGraphicsStateOperation>.Instance;
            }
            else
            {
                operations = PageContentParser.Parse(pageNumber,
                    new ByteArrayInputBytes(contentBytes),
                    ParsingOptions.Logger);
            }

            return ProcessPage(pageNumber,
                dictionary,
                namedDestinations,
                mediaBox,
                cropBox,
                userSpaceUnit,
                rotation,
                initialMatrix,
                operations);
        }

        /// <summary>
        /// Process a page with content.
        /// </summary>
        /// <param name="pageNumber">The page number, starts at 1.</param>
        /// <param name="dictionary"></param>
        /// <param name="namedDestinations"></param>
        /// <param name="mediaBox">The page media box.</param>
        /// <param name="cropBox">The page effective crop box, computed as the intersection of the initial crop box and the media box.</param>
        /// <param name="userSpaceUnit"></param>
        /// <param name="rotation">The page rotation.</param>
        /// <param name="initialMatrix"></param>
        /// <param name="operations">The page operations. Can be empty if the page has no content.</param>
        protected abstract TPage ProcessPage(
            int pageNumber,
            DictionaryToken dictionary,
            NamedDestinations namedDestinations,
            MediaBox mediaBox,
            CropBox cropBox,
            UserSpaceUnit userSpaceUnit,
            PageRotationDegrees rotation,
            TransformationMatrix initialMatrix,
            IReadOnlyList<IGraphicsStateOperation> operations);

        /// <summary>
        /// Get the user space units.
        /// </summary>
        protected static UserSpaceUnit GetUserSpaceUnits(DictionaryToken dictionary)
        {
            if (dictionary.TryGet(NameToken.UserUnit, out var userUnitBase) && userUnitBase is NumericToken userUnitNumber)
            {
                return new UserSpaceUnit(userUnitNumber.Int);
            }

            return UserSpaceUnit.Default;
        }

        /// <summary>
        /// Get the crop box.
        /// </summary>
        protected CropBox GetCropBox(DictionaryToken dictionary, PageTreeMembers pageTreeMembers, MediaBox mediaBox)
        {
            CropBox cropBox;
            if (dictionary.TryGet(NameToken.CropBox, out var cropBoxObject) &&
                DirectObjectFinder.TryGet(cropBoxObject, PdfScanner, out ArrayToken cropBoxArray))
            {
                if (cropBoxArray.Length != 4)
                {
                    ParsingOptions.Logger.Error(
                        $"The CropBox was the wrong length in the dictionary: {dictionary}. Array was: {cropBoxArray}. Using MediaBox.");

                    cropBox = new CropBox(mediaBox.Bounds);

                    return cropBox;
                }

                cropBox = new CropBox(cropBoxArray.ToRectangle(PdfScanner));
            }
            else
            {
                cropBox = pageTreeMembers.GetCropBox() ?? new CropBox(mediaBox.Bounds);
            }

            return cropBox;
        }

        /// <summary>
        /// Get the media box.
        /// </summary>
        protected MediaBox GetMediaBox(int number, DictionaryToken dictionary, PageTreeMembers pageTreeMembers)
        {
            MediaBox mediaBox;
            if (dictionary.TryGet(NameToken.MediaBox, out var mediaBoxObject)
                && DirectObjectFinder.TryGet(mediaBoxObject, PdfScanner, out ArrayToken mediaBoxArray))
            {
                if (mediaBoxArray.Length != 4)
                {
                    ParsingOptions.Logger.Error(
                        $"The MediaBox was the wrong length in the dictionary: {dictionary}. Array was: {mediaBoxArray}. Defaulting to US Letter.");

                    mediaBox = MediaBox.Letter;

                    return mediaBox;
                }

                mediaBox = new MediaBox(mediaBoxArray.ToRectangle(PdfScanner));
            }
            else
            {
                mediaBox = pageTreeMembers.MediaBox;

                if (mediaBox == null)
                {
                    ParsingOptions.Logger.Error(
                        $"The MediaBox was the wrong missing for page {number}. Using US Letter.");

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
        protected static void ApplyTransformNormalise(TransformationMatrix transformationMatrix, ref MediaBox mediaBox, ref CropBox cropBox)
        {
            if (transformationMatrix != TransformationMatrix.Identity)
            {
                mediaBox = new MediaBox(transformationMatrix.Transform(mediaBox.Bounds).Normalise());
                cropBox = new CropBox(transformationMatrix.Transform(cropBox.Bounds).Normalise());
            }
        }
    }
}
