using System;
using System.Text;

namespace UglyToad.Pdf.Parser.Parts
{
    using System.IO;
    using Cos;
    using IO;
    using Util.JetBrains.Annotations;

    internal class CosNameParser
    {
        [NotNull]
        public CosName Parse([NotNull]IRandomAccessRead reader)
        {
            if (reader == null)
            {
                throw new ArgumentNullException(nameof(reader));
            }

            ReadHelper.ReadExpectedChar(reader, '/');

            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
                int c = reader.Read();
                while (c != -1)
                {
                    byte ch = (byte)c;
                    if (ch == '#')
                    {
                        int ch1 = reader.Read();
                        int ch2 = reader.Read();
                        // Prior to PDF v1.2, the # was not a special character.  Also,
                        // it has been observed that various PDF tools do not follow the
                        // spec with respect to the # escape, even though they report
                        // PDF versions of 1.2 or later.  The solution here is that we
                        // interpret the # as an escape only when it is followed by two
                        // valid hex digits.
                        if (ReadHelper.IsHexDigit((char)ch1) && ReadHelper.IsHexDigit((char)ch2))
                        {
                            string hex = "" + (char)ch1 + (char)ch2;
                            try
                            {
                                var byteToWrite = (byte)Convert.ToInt32(hex, 16);
                                writer.Write(byteToWrite);
                            }
                            catch (FormatException e)
                            {
                                throw new IOException("Error: expected hex digit, actual='" + hex + "'", e);
                            }
                            c = reader.Read();
                        }
                        else
                        {
                            // check for premature EOF
                            if (ch2 == -1 || ch1 == -1)
                            {
                                //LOG.error("Premature EOF in BaseParser#parseCosName");
                                c = -1;
                                break;
                            }
                            reader.Unread(ch2);
                            c = ch1;
                            writer.Write(ch);
                        }
                    }
                    else if (ReadHelper.IsEndOfName(ch))
                    {
                        break;
                    }
                    else
                    {
                        writer.Write(ch);
                        c = reader.Read();
                    }
                }
                if (c != -1)
                {
                    reader.Unread(c);
                }

                byte[] bytes = memoryStream.ToArray();
                var str = ReadHelper.IsValidUTF8(bytes) ? Encoding.UTF8.GetString(memoryStream.ToArray()) : Encoding.GetEncoding("windows-1252").GetString(memoryStream.ToArray());
                return CosName.Create(str);
            }
        }
    }
}
