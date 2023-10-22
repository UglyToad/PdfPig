namespace UglyToad.PdfPig.Filters
{
    using System.Collections.Generic;
    using System.Linq;
    using Tokens;
    using UglyToad.PdfPig.Filters.Jbig2;

    internal class Jbig2DecodeFilter : IFilter
    {
        /// <inheritdoc />
        public bool IsSupported { get; } = true;

        /// <inheritdoc />
        public byte[] Decode(IReadOnlyList<byte> input, DictionaryToken streamDictionary, int filterIndex)
        {
            var decodeParms = DecodeParameterResolver.GetFilterParameters(streamDictionary, filterIndex);
            Jbig2Document globalDocument = null;
            if (decodeParms.TryGet(NameToken.Jbig2Globals, out StreamToken tok))
            {
                globalDocument = new Jbig2Document(new ImageInputStream(tok.Data.ToArray()));
            }

            using (var jbig2 = new Jbig2Document(new ImageInputStream(input.ToArray()),
                globalDocument != null ? globalDocument.GetGlobalSegments() : null))
            {
                var page = jbig2.GetPage(1);
                var bitmap = page.GetBitmap();

                var pageInfo =
                    (PageInformation)page.GetPageInformationSegment().GetSegmentData();

                if (globalDocument != null)
                {
                    globalDocument.Dispose();
                }

                var isImageMask = streamDictionary.ContainsKey(NameToken.ImageMask) ||
                    streamDictionary.ContainsKey(NameToken.Im);

                // Invert bits if the default pixel value is black
                return (pageInfo.DefaultPixelValue != 0 || isImageMask) ?
                     bitmap.GetByteArray().Select(x => (byte)~x).ToArray() :
                     bitmap.GetByteArray();
            }
        }
    }
}