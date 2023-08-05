namespace UglyToad.PdfPig.Content
{
    using Core;
    using System;
    using System.Collections.Generic;
    using UglyToad.PdfPig.Filters;
    using UglyToad.PdfPig.Geometry;
    using UglyToad.PdfPig.Logging;
    using UglyToad.PdfPig.Outline;
    using UglyToad.PdfPig.Parser;
    using UglyToad.PdfPig.Parser.Parts;
    using UglyToad.PdfPig.Tokenization.Scanner;
    using UglyToad.PdfPig.Tokens;
    using UglyToad.PdfPig.Util;

    /// <summary>
    /// Page factory abstract class.
    /// </summary>
    /// <typeparam name="TPage">The type of page the page factory creates.</typeparam>
    public abstract class PageFactoryBase<TPage> : IPageFactory<TPage>
    {
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
        /// The <see cref="ILog"/> used to record messages raised by the parsing process.
        /// </summary>
        public readonly ILog Log;

        /// <summary>
        /// Create a <see cref="PageFactoryBase{TPage}"/>.
        /// </summary>
        protected PageFactoryBase(
            IPdfTokenScanner pdfScanner,
            IResourceStore resourceStore,
            ILookupFilterProvider filterProvider,
            IPageContentParser pageContentParser,
            ILog log)
        {
            ResourceStore = resourceStore;
            FilterProvider = filterProvider;
            PageContentParser = pageContentParser;
            PdfScanner = pdfScanner;
            Log = log;
        }

        /// <inheritdoc/>
        public TPage Create(int number, DictionaryToken dictionary, PageTreeMembers pageTreeMembers,
            NamedDestinations namedDestinations, IParsingOptions parsingOptions)
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

            MediaBox mediaBox = GetMediaBox(number, dictionary, pageTreeMembers);
            CropBox cropBox = GetCropBox(dictionary, pageTreeMembers, mediaBox);

            var rotation = new PageRotationDegrees(pageTreeMembers.Rotation);
            // TODO - check if NameToken.Rotate is already looked for in Pages.cs, we don't need to look again
            if (dictionary.TryGet(NameToken.Rotate, PdfScanner, out NumericToken rotateToken))
            {
                rotation = new PageRotationDegrees(rotateToken.Int);
            }

            var stackDepth = 0;

            while (pageTreeMembers.ParentResources.Count > 0)
            {
                var resource = pageTreeMembers.ParentResources.Dequeue();

                ResourceStore.LoadResourceDictionary(resource, parsingOptions);
                stackDepth++;
            }

            if (dictionary.TryGet(NameToken.Resources, PdfScanner, out DictionaryToken resources))
            {
                ResourceStore.LoadResourceDictionary(resources, parsingOptions);
                stackDepth++;
            }

            UserSpaceUnit userSpaceUnit = GetUserSpaceUnits(dictionary);

            TPage page;

            if (!dictionary.TryGet(NameToken.Contents, out var contents))
            {
                page = ProcessPage(number, dictionary, namedDestinations, cropBox, userSpaceUnit, rotation, mediaBox, parsingOptions);
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

                page = ProcessPage(number, dictionary, namedDestinations, bytes, cropBox, userSpaceUnit, rotation, mediaBox, parsingOptions);
            }
            else
            {
                var contentStream = DirectObjectFinder.Get<StreamToken>(contents, PdfScanner);

                if (contentStream == null)
                {
                    throw new InvalidOperationException("Failed to parse the content for the page: " + number);
                }

                var bytes = contentStream.Decode(FilterProvider, PdfScanner);

                page = ProcessPage(number, dictionary, namedDestinations, bytes, cropBox, userSpaceUnit, rotation, mediaBox, parsingOptions);
            }

            for (var i = 0; i < stackDepth; i++)
            {
                ResourceStore.UnloadResourceDictionary();
            }

            return page;
        }

        /// <summary>
        /// Process a page with no content.
        /// </summary>
        protected abstract TPage ProcessPage(
            int pageNumber,
            DictionaryToken dictionary,
            NamedDestinations namedDestinations,
            IReadOnlyList<byte> contentBytes,
            CropBox cropBox,
            UserSpaceUnit userSpaceUnit,
            PageRotationDegrees rotation,
            MediaBox mediaBox,
            IParsingOptions parsingOptions);

        /// <summary>
        /// Process a page with no content.
        /// </summary>
        protected abstract TPage ProcessPage(
            int pageNumber,
            DictionaryToken dictionary,
            NamedDestinations namedDestinations,
            CropBox cropBox,
            UserSpaceUnit userSpaceUnit,
            PageRotationDegrees rotation,
            MediaBox mediaBox,
            IParsingOptions parsingOptions);

        /// <summary>
        /// Get the user space units.
        /// </summary>
        protected static UserSpaceUnit GetUserSpaceUnits(DictionaryToken dictionary)
        {
            var spaceUnits = UserSpaceUnit.Default;
            if (dictionary.TryGet(NameToken.UserUnit, out var userUnitBase) && userUnitBase is NumericToken userUnitNumber)
            {
                spaceUnits = new UserSpaceUnit(userUnitNumber.Int);
            }

            return spaceUnits;
        }

        /// <summary>
        /// Get the crop box.
        /// </summary>
        protected CropBox GetCropBox(DictionaryToken dictionary, PageTreeMembers pageTreeMembers, MediaBox mediaBox)
        {
            if (dictionary.TryGet(NameToken.CropBox, out var cropBoxObject) &&
                DirectObjectFinder.TryGet(cropBoxObject, PdfScanner, out ArrayToken cropBoxArray))
            {
                if (cropBoxArray.Length != 4)
                {
                    Log.Error($"The CropBox was the wrong length in the dictionary: {dictionary}. Array was: {cropBoxArray}. Using MediaBox.");

                    return new CropBox(mediaBox.Bounds);
                }

                return new CropBox(cropBoxArray.ToRectangle(PdfScanner));
            }
            else
            {
                return pageTreeMembers.GetCropBox() ?? new CropBox(mediaBox.Bounds);
            }
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
                    Log.Error($"The MediaBox was the wrong length in the dictionary: {dictionary}. Array was: {mediaBoxArray}. Defaulting to US Letter.");

                    return MediaBox.Letter;
                }

                mediaBox = new MediaBox(mediaBoxArray.ToRectangle(PdfScanner));
            }
            else
            {
                mediaBox = pageTreeMembers.MediaBox;

                if (mediaBox == null)
                {
                    Log.Error($"The MediaBox was the wrong missing for page {number}. Using US Letter.");

                    // PDFBox defaults to US Letter.
                    mediaBox = MediaBox.Letter;
                }
            }

            return mediaBox;
        }
    }
}
