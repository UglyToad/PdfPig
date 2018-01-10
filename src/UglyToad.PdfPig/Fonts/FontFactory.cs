namespace UglyToad.PdfPig.Fonts
{
    using System;
    using System.Collections.Generic;
    using ContentStream;
    using Cos;
    using Exceptions;
    using IO;
    using Logging;
    using Parser.Handlers;

    internal class FontFactory : IFontFactory
    {
        private readonly ILog log;
        private readonly IReadOnlyDictionary<CosName, IFontHandler> handlers;

        public FontFactory(ILog log, Type0FontHandler type0FontHandler, TrueTypeFontHandler trueTypeFontHandler, 
            Type1FontHandler type1FontHandler, Type3FontHandler type3FontHandler)
        {
            this.log = log;
            handlers = new Dictionary<CosName, IFontHandler>
            {
                {CosName.TYPE0, type0FontHandler},
                {CosName.TRUE_TYPE,  trueTypeFontHandler},
                {CosName.TYPE1, type1FontHandler},
                {CosName.TYPE3, type3FontHandler}
            };
        }

        public IFont Get(PdfDictionary dictionary, IRandomAccessRead reader, bool isLenientParsing)
        {
            var type = dictionary.GetName(CosName.TYPE);

            if (!type.Equals(CosName.FONT))
            {
                var message = "The font dictionary did not have type 'Font'. " + dictionary;

                if (isLenientParsing)
                {
                    log?.Error(message);
                }
                else
                {
                    throw new InvalidFontFormatException(message);
                }
            }

            var subtype = dictionary.GetName(CosName.SUBTYPE);

            if (handlers.TryGetValue(subtype, out var handler))
            {
                return handler.Generate(dictionary, reader, isLenientParsing);
            }

            throw new NotImplementedException($"Parsing not implemented for fonts of type: {subtype}, please submit a pull request or an issue.");
        }
    }

}

