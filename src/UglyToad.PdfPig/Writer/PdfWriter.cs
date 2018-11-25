namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices.ComTypes;
    using Content;
    using Fonts.TrueType;
    using Fonts.TrueType.Parser;
    using Geometry;
    using IO;

    internal class PdfDocumentBuilder
    {
        private static readonly TrueTypeFontParser Parser = new TrueTypeFontParser();

        private readonly Dictionary<int, PdfPageBuilder> pages = new Dictionary<int, PdfPageBuilder>();
        private readonly Dictionary<Guid, TrueTypeFontProgram> fonts = new Dictionary<Guid, TrueTypeFontProgram>();

        public IReadOnlyDictionary<int, PdfPageBuilder> Pages => pages;
        public IReadOnlyDictionary<Guid, TrueTypeFontProgram> Fonts => fonts;

        public AddedFont AddTrueTypeFont(IReadOnlyList<byte> fontFileBytes)
        {
            try
            {
                var font = Parser.Parse(new TrueTypeDataBytes(new ByteArrayInputBytes(fontFileBytes)));
                var id = Guid.NewGuid();
                fonts[id] = font;

                return new AddedFont(id);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Writing only supports TrueType fonts, please provide a valid TrueType font.", ex);
            }
        }

        public PdfPageBuilder AddPage(PageSize size, bool isPortrait = true)
        {
            if (!size.TryGetPdfRectangle(out var rectangle))
            {
                throw new ArgumentException($"No rectangle found for Page Size {size}.");
            }

            if (!isPortrait)
            {
                rectangle = new PdfRectangle(0, 0, rectangle.Height, rectangle.Width);
            }

            PdfPageBuilder builder = null;
            for (var i = 0; i < pages.Count; i++)
            {
                if (!pages.ContainsKey(i + 1))
                {
                    builder = new PdfPageBuilder(i + 1, this);
                    break;
                }
            }

            if (builder == null)
            {
                builder = new PdfPageBuilder(pages.Count + 1, this);
            }

            builder.PageSize = rectangle;
            pages[builder.PageNumber] = builder;

            return builder;
        }

        public void Generate(IStream stream)
        {

        }

        public void Generate(string fileName)
        {

        }

        public class AddedFont
        {
            public Guid Id { get; }

            internal AddedFont(Guid id)
            {
                Id = id;
            }
        }
    }

}
