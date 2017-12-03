namespace UglyToad.Pdf.Fonts
{
    using System;
    using System.Collections.Generic;
    using ContentStream;
    using Cos;
    using Parser.Handlers;
    using Parser.Parts;
    using Pdf.Parser;

    internal class FontFactory
    {
        private static readonly IReadOnlyDictionary<CosName, IFontHandler> Handlers;

        static FontFactory()
        {
            Handlers = new Dictionary<CosName, IFontHandler>
            {
                {CosName.TYPE0, new Type0FontHandler(new CidFontFactory(new FontDescriptorFactory()))}
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

            if (Handlers.TryGetValue(subtype, out var handler))
            {
                return handler.Generate(dictionary, arguments);
            }

            throw new NotImplementedException($"Parsing not implemented for fonts of type: {subtype}, please submit a pull request.");
        }
    }

}
