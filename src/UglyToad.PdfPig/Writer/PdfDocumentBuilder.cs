namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Content;
    using Fonts.TrueType;
    using Fonts.TrueType.Parser;
    using Geometry;
    using IO;
    using Tokens;
    using Util;

    internal class PdfDocumentBuilder
    {
        private static readonly byte Break = (byte) '\n';
        private static readonly TrueTypeFontParser Parser = new TrueTypeFontParser();

        private readonly Dictionary<int, PdfPageBuilder> pages = new Dictionary<int, PdfPageBuilder>();
        private readonly Dictionary<Guid, FontStored> fonts = new Dictionary<Guid, FontStored>();

        public IReadOnlyDictionary<int, PdfPageBuilder> Pages => pages;
        public IReadOnlyDictionary<Guid, TrueTypeFontProgram> Fonts => fonts.ToDictionary(x => x.Key, x => x.Value.FontProgram);

        public AddedFont AddTrueTypeFont(IReadOnlyList<byte> fontFileBytes)
        {
            try
            {
                var font = Parser.Parse(new TrueTypeDataBytes(new ByteArrayInputBytes(fontFileBytes)));
                var id = Guid.NewGuid();
                var i = fonts.Count;
                var added = new AddedFont(id, NameToken.Create($"F{i}"));
                fonts[id] = new FontStored(added, font);

                return added;
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

        public void Generate(Stream stream)
        {

        }

        public void Generate(string fileName)
        {

        }

        public byte[] Build()
        {
            var objectLocations = new Dictionary<IndirectReference, long>();
            var fontsWritten = new Dictionary<Guid, ObjectToken>();
            var number = 1;
            using (var memory = new MemoryStream())
            {
                // Header
                WriteString("%PDF-1.7", memory);

                // Body
                foreach (var font in fonts)
                {
                    var widths = new ArrayToken(new [] { new NumericToken(0), new NumericToken(255) });
                    var widthsObj = WriteObject(widths, memory, objectLocations, ref number);

                    var descriptorRef = new IndirectReference(number++, 0);
                    
                    var dictionary = new DictionaryToken(new Dictionary<IToken, IToken>
                    {
                        { NameToken.Type, NameToken.Font },
                        { NameToken.Subtype, NameToken.TrueType },
                        { NameToken.FirstChar, new NumericToken(0) },
                        { NameToken.LastChar, new NumericToken(255) },
                        { NameToken.Encoding, NameToken.WinAnsiEncoding },
                        { NameToken.Widths, widthsObj },
                        { NameToken.FontDesc, new IndirectReferenceToken(descriptorRef) }
                    });

                    var fontObj = WriteObject(dictionary, memory, objectLocations, ref number);
                    fontsWritten.Add(font.Key, fontObj);
                }

                var fontsDictionary = new DictionaryToken(fontsWritten.Select(x => ((IToken)fonts[x.Key].FontKey.Name, (IToken)new IndirectReferenceToken(x.Value.Number)))
                    .ToDictionary(x => x.Item1, x => x.Item2));

                var fontsDictionaryRef = WriteObject(fontsDictionary, memory, objectLocations, ref number);

                var page = new DictionaryToken(new Dictionary<IToken, IToken>
                {
                    { NameToken.Type, NameToken.Page },
                    {
                        NameToken.Resources,
                        new DictionaryToken(new Dictionary<IToken, IToken>
                        {
                            { NameToken.ProcSet, new ArrayToken(new []{ NameToken.Create("PDF"), NameToken.Create("Text") }) },
                            { NameToken.Font, new IndirectReferenceToken(fontsDictionaryRef.Number) }
                        })
                    }
                });

                var pageRef = WriteObject(page, memory, objectLocations, ref number);

                var pagesDictionary = new DictionaryToken(new Dictionary<IToken, IToken>
                {
                    { NameToken.Type, NameToken.Pages },
                    { NameToken.Kids, new ArrayToken(new [] { new IndirectReferenceToken(pageRef.Number) }) },
                    { NameToken.Count, new NumericToken(1) }
                });

                var pagesRef = WriteObject(pagesDictionary, memory, objectLocations, ref number);

                var catalog = new DictionaryToken(new Dictionary<IToken, IToken>
                {
                    { NameToken.Type, NameToken.Catalog },
                    { NameToken.Pages, new IndirectReferenceToken(pagesRef.Number) }
                });

                WriteObject(catalog, memory, objectLocations, ref number);

                return memory.ToArray();
            }
        }

        private static ObjectToken WriteObject(IToken content, MemoryStream stream, Dictionary<IndirectReference, long> objectOffsets, ref int number)
        {
            var reference = new IndirectReference(number++, 0);
            var obj = new ObjectToken(stream.Position, reference, content);
            objectOffsets.Add(reference, obj.Position);
            // TODO: write
            stream.Write(new byte[50], 0, 50);
            stream.WriteByte(Break);
            return obj;
        }

        private static void WriteString(string text, MemoryStream stream, bool appendBreak = true)
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes(text);
            stream.Write(bytes, 0, bytes.Length);
            if (appendBreak)
            {
                stream.WriteByte(Break);
            }
        }

        internal class FontStored
        {
            public AddedFont FontKey { get; }

            public TrueTypeFontProgram FontProgram { get; }

            public FontStored(AddedFont fontKey, TrueTypeFontProgram fontProgram)
            {
                FontKey = fontKey;
                FontProgram = fontProgram;
            }
        }

        public class AddedFont
        {
            public Guid Id { get; }

            public NameToken Name { get; }

            internal AddedFont(Guid id, NameToken name)
            {
                Id = id;
                Name = name ?? throw new ArgumentNullException(nameof(name));
            }
        }
    }

}
