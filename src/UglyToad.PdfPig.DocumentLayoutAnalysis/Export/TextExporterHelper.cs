namespace UglyToad.PdfPig.DocumentLayoutAnalysis.Export
{
    using System;
    using System.Text;
    using System.Xml;

    internal static class TextExporterHelper
    {
        public static Func<string, string> GetXmlInvalidCharHandler(InvalidCharStrategy invalidCharacterStrategy)
        {
            switch (invalidCharacterStrategy)
            {
                case InvalidCharStrategy.DoNotCheck:
                    return new Func<string, string>(s => s);

                case InvalidCharStrategy.Remove:
                    return new Func<string, string>(s =>
                    {
                        // https://stackoverflow.com/a/17735649
                        if (string.IsNullOrEmpty(s))
                        {
                            return s;
                        }

                        int length = s.Length;
                        StringBuilder stringBuilder = new StringBuilder(length);
                        for (int i = 0; i < length; ++i)
                        {
                            if (XmlConvert.IsXmlChar(s[i]))
                            {
                                stringBuilder.Append(s[i]);
                            }
                        }

                        return stringBuilder.ToString();
                    });

                case InvalidCharStrategy.ConvertToHexadecimal:
                    return new Func<string, string>(s =>
                    {
                        // Adapted from https://stackoverflow.com/a/17735649
                        if (string.IsNullOrEmpty(s))
                        {
                            return s;
                        }

                        int length = s.Length;
                        StringBuilder stringBuilder = new StringBuilder(length);
                        for (int i = 0; i < length; ++i)
                        {
                            if (XmlConvert.IsXmlChar(s[i]))
                            {
                                stringBuilder.Append(s[i]);
                            }
                            else
                            {
                                byte[] bytes = Encoding.UTF8.GetBytes(s[i].ToString());
                                string hexString = BitConverter.ToString(bytes);
                                stringBuilder.Append("0x").Append(hexString);
                            }
                        }

                        return stringBuilder.ToString();
                    });

                default:
                    throw new NotImplementedException("TODO");
            }
        }
    }
}
