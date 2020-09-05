namespace UglyToad.PdfPig.Writer.Copier.Font
{
    using System;
    using System.IO;
    using System.Linq;
    using Core;
    using Filters;
    using PdfPig;
    using PdfPig.Fonts.TrueType;
    using PdfPig.Fonts.TrueType.Parser;
    using PdfPig.Fonts.Type1;
    using PdfPig.Fonts.Type1.Parser;
    using Tokens;
    using Util.JetBrains.Annotations;

    internal class FontObject
    {
        public IndirectReferenceToken SourceReferenceToken { get; private set; }

        public IndirectReferenceToken DestinationReferenceToken { get; set; }

        // This is the font name as set in `/BaseFont`
        public string Name { get; private set; }

        public string FontName { 
            get {
                var plusIndex = Name.IndexOf('+');
                return plusIndex == -1 ? Name : Name.Substring(plusIndex + 1);
            }
        }

        public NameToken Type { get; private set; }

        /*
         * I choose to do the `>` operand because,
         * if a font is subset, the font name should be prefixed
         * with 6 random letter, plus the `+`.
         * As per spec: 9.6.4 Font Subsets
         */
        public bool Subset => Name.IndexOf('+') > 0;

        public bool Embedded => FontData != null;

        public DictionaryToken FontDescriptor { get; private set; } = null;

        public DictionaryToken FontDictionary { get; private set;  } = null;

        // This object would hold the parsed font program object
        public object FontData { get; private set; } = null;

        public byte[] FontDataBytes { get; private set; } = null;

        private int[] charCodes = null;
        public int[] CharCodes
        {
            get
            {
                if (charCodes != null)
                {
                    return charCodes;
                }

                if (Type == NameToken.TrueType)
                {
                    var trueTypeFont = FontData as TrueTypeFont;
                    var cmap = trueTypeFont.WindowsUnicodeCMap ?? trueTypeFont.WindowsSymbolCMap ?? trueTypeFont.MacRomanCMap
                        ?? throw new InvalidOperationException("Cannot subset font due to missing cmap subtables."); ;

                    charCodes = cmap.GetCharacterCodes().OrderBy(k => k).ToArray();
                }
                else if (Type == NameToken.Type1)
                {
                    var type1Font = FontData as Type1Font;
                    charCodes = type1Font.Encoding.Keys.OrderBy(k => k).ToArray();
                }
                else
                {
                    // TODO: I haven't implemented a way to get the chars code for the other type of font
                    return null;
                }

                return charCodes;
            }
        }

        private FontObject() {}

        internal static FontObject CreateFrom([NotNull] Func<IndirectReferenceToken, IToken> tokenScanner, [NotNull] IndirectReferenceToken fontReference)
        {
            var fontDictionary = TokenHelper.GetTokenAs<DictionaryToken>(fontReference, tokenScanner);

            string baseFontName;
            if (fontDictionary.TryGet(NameToken.BaseFont, out var tokenObj))
            {
                baseFontName = TokenHelper.GetTokenAs<NameToken>(tokenObj, tokenScanner).Data;
            }
            else
            {
                throw new PdfDocumentFormatException($"Unable to extract the font name of font located at {fontReference}");
            }

            if (!fontDictionary.TryGet(NameToken.Subtype, out NameToken fontType))
            {
                throw new PdfDocumentFormatException($"Unable to extract font subtype from {fontDictionary}");
            }

            if (!fontDictionary.TryGet(NameToken.FontDescriptor, out tokenObj))
            {
                return new FontObject()
                {
                    SourceReferenceToken = fontReference,
                    Name = baseFontName,
                    FontDictionary = fontDictionary
                };
            }

            var fontDescriptor = TokenHelper.GetTokenAs<DictionaryToken>(tokenObj, tokenScanner);
            var (fontData, fontDataBytes) = ExtractFontData(tokenScanner, fontDescriptor, fontType);

            return new FontObject()
            {
                SourceReferenceToken = fontReference,
                Name = baseFontName,
                Type = fontType,
                FontDictionary = fontDictionary,
                FontDescriptor = fontDescriptor,
                FontData = fontData,
                FontDataBytes = fontDataBytes
            };
        }

        private static (object, byte[]) ExtractFontData(Func<IndirectReferenceToken, IToken> lookupFunc, DictionaryToken fontDescriptor, NameToken type)
        {
            if (!fontDescriptor.TryGet(NameToken.FontFile, out var tokenObj) && !fontDescriptor.TryGet(NameToken.FontFile2, out tokenObj) && 
                !fontDescriptor.TryGet(NameToken.FontFile3, out tokenObj))
            {
                // This would mean that the font is not embedded
                return (null, null);
            }

            // TODO: Check if we always should get a StreamToken
            var fontFileStream = TokenHelper.GetTokenAs<StreamToken>(tokenObj, lookupFunc);

            var bytes = fontFileStream.Decode(DefaultFilterProvider.Instance).ToArray();

            if (type == NameToken.TrueType)
            {
                return (TrueTypeFontParser.Parse(new TrueTypeDataBytes(bytes)), bytes);
            }
            else if (type == NameToken.Type1)
            {
                var length1 = 0;
                
                if (fontFileStream.StreamDictionary.TryGet(NameToken.Length1, out tokenObj))
                {
                    length1 = TokenHelper.GetTokenAs<NumericToken>(tokenObj, lookupFunc).Int;
                }

                var length2 = 0;
                if (!fontFileStream.StreamDictionary.TryGet(NameToken.Length2, out tokenObj))
                {
                    length2 = TokenHelper.GetTokenAs<NumericToken>(tokenObj, lookupFunc).Int;
                }

                return (Type1FontParser.Parse(new ByteArrayInputBytes(bytes), length1, length2), bytes);
            }

            // We have to return something because if we return null,
            // the font would be marked as being not embedded
            return (fontFileStream, bytes);
        }
    }
}
