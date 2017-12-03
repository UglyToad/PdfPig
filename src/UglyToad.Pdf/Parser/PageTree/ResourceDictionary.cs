namespace UglyToad.Pdf.Parser.PageTree
{
    using System.Collections.Generic;
    using Content;
    using ContentStream;
    using Cos;

    /// <summary>
    /// Represents the resources specified for a <see cref="Page"/> or <see cref="Pages"/> object.
    /// </summary>
    public class ResourceDictionary
    {
        private readonly Dictionary<CosName, CosObjectKey> fonts = new Dictionary<CosName, CosObjectKey>();
        private readonly Dictionary<CosName, Font> fontObjects
            = new Dictionary<CosName, Font>();

        public void Merge(ResourceDictionary parent)
        {
            foreach (var font in parent.fonts)
            {
                if (!fonts.ContainsKey(font.Key))
                {
                    fonts[font.Key] = font.Value;
                }
            }
        }

        public void SetFonts(IReadOnlyDictionary<CosName, CosObjectKey> resourceFonts)
        {
            foreach (var font in resourceFonts)
            {
                fonts[font.Key] = font.Value;
            } 
        }

        public bool ContainsFont(CosName name)
        {
            return fonts.ContainsKey(name);
        }

        internal bool GetFont(CosName name, ParsingArguments arguments, out Font value)
        {
            if (fontObjects.TryGetValue(name, out value))
            {
                return true;
            }

            if (!fonts.TryGetValue(name, out var key))
            {
                return false;
            }

            var dictionary = arguments.Container.Get<DynamicParser>()
                .Parse(arguments, key, false) as PdfDictionary;

            if (dictionary == null)
            {
                return false;
            }

            var font = arguments.Container.Get<FontParser>()
                .Parse(dictionary, arguments);

            fontObjects[name] = font;

            // retrieve and cache
            return false;
        }
    }
}