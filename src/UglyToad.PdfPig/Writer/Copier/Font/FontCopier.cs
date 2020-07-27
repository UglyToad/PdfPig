namespace UglyToad.PdfPig.Writer.Copier.Font
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Tokens;

    /// <inheritdoc/>
    public class FontCopier : IObjectCopier
    {
        private readonly List<FontObject> writtenFonts;
        private ObjectCopier copier;

        /// <inheritdoc/>
        public FontCopier(ObjectCopier mainCopier)
        {
            writtenFonts = new List<FontObject>();
            copier = mainCopier;
        }

        /// <inheritdoc/>
        public IToken CopyObject(IToken sourceToken, Func<IndirectReferenceToken, IToken> tokenScanner)
        {
            if (!(sourceToken is IndirectReferenceToken sourceReferenceToken))
            {
                return null;
            }

            var token = tokenScanner(sourceReferenceToken);
            if (!(token is DictionaryToken dictionaryToken))
            {
                return null;
            }

            if (!dictionaryToken.TryGet(NameToken.Type, out var nameTypeToken) || !nameTypeToken.Equals(NameToken.Font))
            {
                return null;
            }

            var newFontObject = FontObject.CreateFrom(tokenScanner, sourceReferenceToken);
            if (!newFontObject.Embedded)
            {
                // TODO: Make an option
                return null;
            }

            var newFontChars = newFontObject.CharCodes;
            if (newFontChars == null)
            {
                // Let the parent copier handle it, since we won't be using the object
                return null;
            }

            var writtenFont = FindSimilarFont(newFontObject.FontName, newFontObject.Type);
            if (writtenFont == null)
            {
                newFontObject.DestinationReferenceToken = copier.WriteToken(copier.CopyObject(dictionaryToken, tokenScanner));
                writtenFonts.Add(newFontObject);
                return newFontObject.DestinationReferenceToken;
            }

            var writtenFontChars = writtenFont.CharCodes;
            if (writtenFontChars.SequenceEqual(newFontChars))
            {
                return writtenFont.DestinationReferenceToken;
            }

            // If we are in this scope in means that the chars did not match and we have the two font
            // TODO: Font Merging
            return null;
        }

        private FontObject FindSimilarFont(string fontName, NameToken fontType)
        {
            return writtenFonts.FirstOrDefault(writtenFont => writtenFont.FontName == fontName && writtenFont.Type == fontType);
        }


        /// <inheritdoc/>
        public void ClearReference()
        {
            // TODO: Invalidate reference in FontObjects
        }
    }
}
