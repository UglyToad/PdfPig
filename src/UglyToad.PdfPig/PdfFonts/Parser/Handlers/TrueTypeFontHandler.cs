namespace UglyToad.PdfPig.PdfFonts.Parser.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Cmap;
    using Core;
    using Filters;
    using Fonts;
    using Fonts.AdobeFontMetrics;
    using Fonts.Encodings;
    using Fonts.Standard14Fonts;
    using Fonts.SystemFonts;
    using Fonts.TrueType;
    using Fonts.TrueType.Parser;
    using Logging;
    using PdfPig.Parser.Parts;
    using Simple;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal class TrueTypeFontHandler : IFontHandler
    {
        private readonly ILog log;
        private readonly IPdfTokenScanner pdfScanner;
        private readonly ILookupFilterProvider filterProvider;
        private readonly IEncodingReader encodingReader;
        private readonly ISystemFontFinder systemFontFinder;
        private readonly IFontHandler type1FontHandler;

        public TrueTypeFontHandler(ILog log, IPdfTokenScanner pdfScanner, ILookupFilterProvider filterProvider,
            IEncodingReader encodingReader,
            ISystemFontFinder systemFontFinder,
            IFontHandler type1FontHandler)
        {
            this.log = log;
            this.filterProvider = filterProvider;
            this.encodingReader = encodingReader;
            this.systemFontFinder = systemFontFinder;
            this.type1FontHandler = type1FontHandler;
            this.pdfScanner = pdfScanner;
        }

        public IFont Generate(DictionaryToken dictionary)
        {
            if (!dictionary.TryGetOptionalTokenDirect(NameToken.FirstChar, pdfScanner, out NumericToken firstCharacterToken)
                || !dictionary.TryGet<IToken>(NameToken.FontDescriptor, pdfScanner, out _)
                || !dictionary.TryGet(NameToken.Widths, out IToken _))
            {
                if (!dictionary.TryGetOptionalTokenDirect(NameToken.BaseFont, pdfScanner, out NameToken baseFont))
                {
                    throw new InvalidFontFormatException($"The provided TrueType font dictionary did not contain a /FirstChar or a /BaseFont entry: {dictionary}.");
                }

                // Can use the AFM descriptor despite not being Type 1!
                var standard14Font = Standard14.GetAdobeFontMetrics(baseFont.Data);

                if (standard14Font == null)
                {
                    throw new InvalidFontFormatException($"The provided TrueType font dictionary did not have a /FirstChar and did not match a Standard 14 font: {dictionary}.");
                }

                var fileSystemFont = systemFontFinder.GetTrueTypeFont(baseFont.Data);

                var thisEncoding = encodingReader.Read(dictionary);

                if (thisEncoding == null)
                {
                    thisEncoding = new AdobeFontMetricsEncoding(standard14Font);
                }

                int? firstChar = null;
                double[] widthsOverride = null;

                if (dictionary.TryGet(NameToken.FirstChar, pdfScanner, out firstCharacterToken))
                {
                    firstChar = firstCharacterToken.Int;
                }

                if (dictionary.TryGet(NameToken.Widths, pdfScanner, out ArrayToken widthsArray))
                {
                    widthsOverride = widthsArray.Data.OfType<NumericToken>()
                        .Select(x => x.Double).ToArray();
                }

                return new TrueTypeStandard14FallbackSimpleFont(baseFont, standard14Font, thisEncoding, fileSystemFont,
                    new TrueTypeStandard14FallbackSimpleFont.MetricOverrides(firstChar, widthsOverride));
            }

            var firstCharacter = firstCharacterToken.Int;

            var widths = FontDictionaryAccessHelper.GetWidths(pdfScanner, dictionary);

            var descriptor = FontDictionaryAccessHelper.GetFontDescriptor(pdfScanner, dictionary);

            var font = ParseTrueTypeFont(descriptor, out var actualHandler);

            if (font == null && actualHandler != null)
            {
                return actualHandler.Generate(dictionary);
            }

            var name = FontDictionaryAccessHelper.GetName(pdfScanner, dictionary, descriptor);

            CMap toUnicodeCMap = null;
            if (dictionary.TryGet(NameToken.ToUnicode, out var toUnicodeObj))
            {
                var toUnicode = DirectObjectFinder.Get<StreamToken>(toUnicodeObj, pdfScanner);

                var decodedUnicodeCMap = toUnicode.Decode(filterProvider, pdfScanner);

                if (decodedUnicodeCMap != null)
                {
                    toUnicodeCMap = CMapCache.Parse(new ByteArrayInputBytes(decodedUnicodeCMap));
                }
            }

            Encoding encoding = encodingReader.Read(dictionary, descriptor);
            
            if (encoding == null && font?.TableRegister?.CMapTable != null
                                 && font.TableRegister.PostScriptTable?.GlyphNames != null)
            {
                var postscript = font.TableRegister.PostScriptTable;

                // Synthesize an encoding
                var fakeEncoding = new Dictionary<int, string>();
                for (var i = 0; i < 256; i++)
                {
                    if (font.TableRegister.CMapTable.TryGetGlyphIndex(i, out var index))
                    {
                        string glyphName;
                        if (index >= 0 && index < postscript.GlyphNames.Count)
                        {
                            glyphName = postscript.GlyphNames[index];
                        }
                        else
                        {
                            glyphName = index.ToString(CultureInfo.InvariantCulture);
                        }

                        fakeEncoding[i] = glyphName;
                    }
                }

                encoding = new BuiltInEncoding(fakeEncoding);
            }

            return new TrueTypeSimpleFont(name, descriptor, toUnicodeCMap, encoding, font, firstCharacter, widths);
        }

        private TrueTypeFont ParseTrueTypeFont(FontDescriptor descriptor, out IFontHandler actualHandler)
        {
            actualHandler = null;

            if (descriptor.FontFile == null)
            {
                try
                {
                    var ttf = systemFontFinder.GetTrueTypeFont(descriptor.FontName.Data);
                    return ttf;
                }
                catch (Exception ex)
                {
                    log.Error($"Failed finding system font by name: {descriptor.FontName}.", ex);
                }

                return null;
            }

            try
            {
                var fontFileStream = DirectObjectFinder.Get<StreamToken>(descriptor.FontFile.ObjectKey, pdfScanner);

                var fontFile = fontFileStream.Decode(filterProvider, pdfScanner);

                if (descriptor.FontFile.FileType == DescriptorFontFile.FontFileType.FromSubtype)
                {
                    var shouldThrow = true;

                    if (fontFileStream.StreamDictionary.TryGet(NameToken.Subtype, pdfScanner, out NameToken subTypeName))
                    {
                        if (subTypeName == NameToken.Type1C)
                        {
                            actualHandler = type1FontHandler;
                            return null;
                        }

                        if (subTypeName == NameToken.OpenType)
                        {
                            shouldThrow = false;
                        }
                    }

                    if (shouldThrow)
                    {
                        throw new InvalidFontFormatException(
                            $"Expected a TrueType font in the TrueType font descriptor, instead it was {descriptor.FontFile.FileType}.");
                    }
                }
                
                var font = TrueTypeFontParser.Parse(new TrueTypeDataBytes(new ByteArrayInputBytes(fontFile)));

                return font;
            }
            catch (Exception ex)
            {
                log.Error("Could not parse the TrueType font.", ex);

                return null;
            }
        }
    }
}
