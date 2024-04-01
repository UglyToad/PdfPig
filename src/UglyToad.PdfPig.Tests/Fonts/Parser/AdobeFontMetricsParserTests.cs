namespace UglyToad.PdfPig.Tests.Fonts.Parser
{
    using PdfPig.Core;
    using PdfPig.Fonts.AdobeFontMetrics;

    public class AdobeFontMetricsParserTests
    {
        private const string CourierAfmSnippet = @"
StartFontMetrics 4.1

Comment Copyright (c) 1989, 1990, 1991, 1992, 1993, 1997 Adobe Systems Incorporated.  All Rights Reserved.

Comment Creation Date: Thu May  1 17:27:09 1997

Comment UniqueID 43050

Comment VMusage 39754 50779

FontName Courier

FullName Courier

FamilyName Courier

Weight Medium

ItalicAngle 0

IsFixedPitch true

CharacterSet ExtendedRoman

FontBBox -23 -250 715 805 

UnderlinePosition -100

UnderlineThickness 50

Version 003.000

Notice Copyright (c) 1989, 1990, 1991, 1992, 1993, 1997 Adobe Systems Incorporated.  All Rights Reserved.

EncodingScheme AdobeStandardEncoding

CapHeight 562

XHeight 426

Ascender 629

Descender -157

StdHW 51

StdVW 51

StartCharMetrics 6

C 32 ; WX 600 ; N space ; B 0 0 0 0 ;

C 33 ; WX 600 ; N exclam ; B 236 -15 364 572 ;

C 34 ; WX 600 ; N quotedbl ; B 187 328 413 562 ;

C 35 ; WX 600 ; N numbersign ; B 93 -32 507 639 ;

C 36 ; WX 600 ; N dollar ; B 105 -126 496 662 ;

C 37 ; WX 600 ; N percent ; B 81 -15 518 622 ;

EndCharMetrics";
        
        [Fact]
        public void CanParseAfmFile()
        {
            var input = StringBytesTestConverter.Convert(CourierAfmSnippet, false);

            var metrics = AdobeFontMetricsParser.Parse(input.Bytes, false);

            Assert.NotNull(metrics);
        }

        [Fact]
        public void CanParseHelveticaAfmFile()
        {
            var helvetica = GetResourceBytes("UglyToad.PdfPig.Fonts.Resources.AdobeFontMetrics.Helvetica.afm");

            var input = new MemoryInputBytes(helvetica);

            var metrics = AdobeFontMetricsParser.Parse(input, false);

            Assert.NotNull(metrics);
        }

        private static byte[] GetResourceBytes(string name)
        {
            using (var memoryStream = new MemoryStream())
            using (var resource = typeof(AdobeFontMetricsParser).Assembly.GetManifestResourceStream(name))
            {
                if (resource == null)
                {
                    throw new InvalidOperationException($"No assembly resource with name: {name}.");
                }

                resource.CopyTo(memoryStream);

                return memoryStream.ToArray();
            }
        }
    }
}
