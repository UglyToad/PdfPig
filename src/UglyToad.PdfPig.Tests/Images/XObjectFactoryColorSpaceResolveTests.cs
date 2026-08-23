namespace UglyToad.PdfPig.Tests.Images
{
    using System.Collections.Generic;
    using PdfPig.Content;
    using PdfPig.Core;
    using PdfPig.Graphics;
    using PdfPig.Graphics.Colors;
    using PdfPig.Graphics.Core;
    using PdfPig.PdfFonts;
    using PdfPig.Tokens;
    using PdfPig.XObjects;
    using PdfPig.Tests.Tokens;
    using Xunit;

    /// <summary>
    /// Reading an image resolves its stream dictionary so that the entries it needs are direct objects.
    /// The /ColorSpace entry is the one place where that is the wrong thing to do: for an /ICCBased space
    /// the array points at the profile stream and for an /Indexed one at the colour table, and resolving
    /// either materialises a whole object that <see cref="ColorSpaceDetails"/> is about to fetch through
    /// the scanner itself.
    /// </summary>
    public class XObjectFactoryColorSpaceResolveTests
    {
        private sealed class NoOpFontFactory : IFontFactory
        {
            public IFont Get(DictionaryToken dictionary) => null!;
        }

        private static ResourceStore Store(TestPdfTokenScanner scanner)
            => new ResourceStore(scanner,
                new NoOpFontFactory(),
                new TestFilterProvider(),
                new ParsingOptions { UseLenientParsing = true, SkipMissingFonts = true });

        private static void Register(TestPdfTokenScanner scanner, int number, IToken token)
            => scanner.Objects[new IndirectReference(number, 0)] =
                new ObjectToken(XrefLocation.File(0), new IndirectReference(number, 0), token);

        /// <summary>
        /// A one pixel image whose other entries are all indirect, so that whether Resolve ran is observable.
        /// </summary>
        private static XObjectImage ReadImage(TestPdfTokenScanner scanner, IToken colorSpace)
        {
            Register(scanner, 20, new NumericToken(1));
            Register(scanner, 21, new NumericToken(8));

            var imageDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.Subtype, NameToken.Image },
                { NameToken.Width, new IndirectReferenceToken(new IndirectReference(20, 0)) },
                { NameToken.Height, new IndirectReferenceToken(new IndirectReference(20, 0)) },
                { NameToken.BitsPerComponent, new IndirectReferenceToken(new IndirectReference(21, 0)) },
                { NameToken.ColorSpace, colorSpace }
            });

            var record = new XObjectContentRecord(XObjectType.Image,
                new StreamToken(imageDictionary, new byte[] { 0, 0, 0 }),
                TransformationMatrix.Identity,
                RenderingIntent.RelativeColorimetric,
                null);

            return XObjectFactory.ReadImage(record, scanner, new TestFilterProvider(), Store(scanner));
        }

        [Fact]
        public void AnIccBasedColorSpaceKeepsItsProfileStreamUnresolved()
        {
            var scanner = new TestPdfTokenScanner();

            var profileDictionary = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.N, new NumericToken(3) }
            });

            Register(scanner, 10, new StreamToken(profileDictionary, new byte[512]));

            var image = ReadImage(scanner,
                new ArrayToken([NameToken.Iccbased, new IndirectReferenceToken(new IndirectReference(10, 0))]));

            var colorSpace = Assert.IsType<ArrayToken>(image.ImageDictionary.Data[NameToken.ColorSpace.Data]);

            // The profile is still a reference: resolving it would have inlined the whole stream.
            Assert.IsType<IndirectReferenceToken>(colorSpace.Data[1]);

            // The rest of the dictionary was resolved as it always was.
            Assert.IsType<NumericToken>(image.ImageDictionary.Data[NameToken.Width.Data]);
            Assert.Equal(1, image.WidthInSamples);

            // And the colour space itself still parses, through the scanner rather than the dictionary.
            Assert.IsType<ICCBasedColorSpaceDetails>(image.ColorSpaceDetails);
        }

        [Fact]
        public void AnIndexedColorSpaceKeepsItsColorTableUnresolved()
        {
            var scanner = new TestPdfTokenScanner();

            Register(scanner, 11, new StreamToken(
                new DictionaryToken(new Dictionary<NameToken, IToken>()),
                new byte[] { 255, 0, 0, 0, 255, 0 }));

            var image = ReadImage(scanner, new ArrayToken([
                NameToken.Indexed,
                NameToken.Devicergb,
                new NumericToken(1),
                new IndirectReferenceToken(new IndirectReference(11, 0))
            ]));

            var colorSpace = Assert.IsType<ArrayToken>(image.ImageDictionary.Data[NameToken.ColorSpace.Data]);

            Assert.IsType<IndirectReferenceToken>(colorSpace.Data[3]);

            var indexed = Assert.IsType<IndexedColorSpaceDetails>(image.ColorSpaceDetails);
            Assert.Equal(1, indexed.HiVal);
        }

        [Fact]
        public void AColorSpaceNamedRatherThanWrittenOutIsStillResolved()
        {
            var scanner = new TestPdfTokenScanner();

            Register(scanner, 12, NameToken.Devicergb);

            var image = ReadImage(scanner, new IndirectReferenceToken(new IndirectReference(12, 0)));

            // Only an array whose first element is a name is held back; anything else resolves as before.
            Assert.IsType<NameToken>(image.ImageDictionary.Data[NameToken.ColorSpace.Data]);
            Assert.IsType<DeviceRgbColorSpaceDetails>(image.ColorSpaceDetails);
        }
    }
}
