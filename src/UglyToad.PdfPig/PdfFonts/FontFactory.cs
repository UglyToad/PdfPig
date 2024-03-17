namespace UglyToad.PdfPig.PdfFonts
{
    using System;
    using System.Collections.Generic;
    using Logging;
    using Parser.Handlers;
    using Tokens;
    using Util;

    internal class FontFactory : IFontFactory
    {
        private readonly ILog log;
        private readonly IReadOnlyDictionary<NameToken, IFontHandler> handlers;

        public FontFactory(ILog log, Type0FontHandler type0FontHandler, TrueTypeFontHandler trueTypeFontHandler,
            Type1FontHandler type1FontHandler, Type3FontHandler type3FontHandler)
        {
            this.log = log;
            handlers = new Dictionary<NameToken, IFontHandler>
            {
                {NameToken.Type0, type0FontHandler},
                {NameToken.TrueType,  trueTypeFontHandler},
                {NameToken.Type1, type1FontHandler},
                {NameToken.MmType1, type1FontHandler},
                {NameToken.Type3, type3FontHandler}
            };
        }

        public IFont Get(DictionaryToken dictionary)
        {
            var type = dictionary.GetNameOrDefault(NameToken.Type);

            if (type != null && !type.Equals(NameToken.Font))
            {
                var message = "The font dictionary did not have type 'Font'. " + dictionary;

                log?.Error(message);
            }

            var subtype = dictionary.GetNameOrDefault(NameToken.Subtype);

            if (subtype != null && handlers.TryGetValue(subtype, out var handler))
            {
                return handler.Generate(dictionary);
            }

            throw new NotImplementedException($"Parsing not implemented for fonts of type: {subtype}, please submit a pull request or an issue.");
        }
    }

}

