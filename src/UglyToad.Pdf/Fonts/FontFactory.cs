namespace UglyToad.Pdf.Fonts
{
    using System;
    using System.Collections.Generic;
    using ContentStream;
    using Cos;
    using Parser.Handlers;
    using Pdf.Parser;

    internal class FontFactory
    {
        private readonly IReadOnlyDictionary<CosName, IFontHandler> handlers;

        public FontFactory(Type0FontHandler type0FontHandler)
        {
            handlers = new Dictionary<CosName, IFontHandler>
            {
                {CosName.TYPE0, type0FontHandler}
            };
        }

        public IFont GetFont(PdfDictionary dictionary, ParsingArguments arguments)
        {
            var type = dictionary.GetName(CosName.TYPE);

            if (!type.Equals(CosName.FONT))
            {
                var message = "The font dictionary did not have type 'Font'. " + dictionary;

                if (arguments.IsLenientParsing)
                {
                    arguments.Log.Error(message);
                }
                else
                {
                    throw new InvalidOperationException(message);
                }
            }

            var subtype = dictionary.GetName(CosName.SUBTYPE);

            if (handlers.TryGetValue(subtype, out var handler))
            {
                return handler.Generate(dictionary, arguments);
            }

            throw new NotImplementedException($"Parsing not implemented for fonts of type: {subtype}, please submit a pull request or an issue.");
        }
    }

}
