namespace UglyToad.PdfPig.Cos
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Util;

    internal class CosString : CosBase
    {
        public byte[] Bytes { get; }

        /**
         * Creates a new PDF string from a byte array. This method can be used to read a string from
         * an existing PDF file, or to create a new byte string.
         *
         * @param bytes The raw bytes of the PDF text string or byte string.
         */
        public CosString(byte[] bytes)
        {
            Bytes = CloneBytes(bytes);
        }

        /**
         * Creates a new <i>text string</i> from a Java string.
         *
         * @param text The string value of the object.
         */
        public CosString(string text)
        {
            // check whether the string uses only characters available in PDFDocEncoding
            bool isOnlyPdfDocEncoding = true;
            for (int i = 0; i < text.Length; i++)
            {
                if (!PdfDocEncoding.ContainsChar(text[i]))
                {
                    isOnlyPdfDocEncoding = false;
                    break;
                }
            }

            if (isOnlyPdfDocEncoding)
            {
                // PDFDocEncoded string
                Bytes = PdfDocEncoding.GetBytes(text);
            }
            else
            {
                // UTF-16BE encoded string with a leading byte order marker
                byte[] data = Encoding.BigEndianUnicode.GetBytes(text);

                using (var outBytes = new MemoryStream(data.Length + 2))
                using (var w = new StreamWriter(outBytes))
                {
                    w.Write(0xFE); // BOM
                    w.Write(0xFF); // BOM

                    try
                    {
                        w.Write(data);
                    }
                    catch (IOException e)
                    {
                        // should never happen
                        throw new InvalidOperationException("Fatal Error", e);
                    }

                    Bytes = outBytes.ToArray();
                }
            }
        }

        private static byte[] CloneBytes(IReadOnlyList<byte> bytes)
        {
            var result = new byte[bytes.Count];

            for (int i = 0; i < bytes.Count; i++)
            {
                result[i] = bytes[i];
            }

            return result;
        }

        /// <summary>
        /// This will create a <see cref="CosString"/> from a string of hex characters.
        /// </summary>
        /// <param name="hex">A hex string.</param>
        /// <returns>A cos string with the hex characters converted to their actual bytes.</returns>
        public static CosString ParseHex(string hex)
        {
            using (var bytes = new MemoryStream())
            using (var writer = new StreamWriter(bytes))
            {
                StringBuilder hexBuffer = new StringBuilder(hex.Trim());

                // if odd number then the last hex digit is assumed to be 0
                if (hexBuffer.Length % 2 != 0)
                {
                    hexBuffer.Append('0');
                }

                int length = hexBuffer.Length;
                for (int i = 0; i < length; i += 2)
                {
                    try
                    {
                        writer.Write(Convert.ToInt32(hexBuffer.ToString().Substring(i, 2), 16));
                    }
                    catch (FormatException e)
                    {
                        throw new ArgumentException("Invalid hex string: " + hex, e);
                    }
                }

                return new CosString(bytes.ToArray());
            }
        }
        /**
         * Returns the content of this string as a PDF <i>text string</i>.
         */
        public string GetString()
        {
            // text string - BOM indicates Unicode
            if (Bytes.Length >= 2)
            {
                if ((Bytes[0] & 0xff) == 0xFE && (Bytes[1] & 0xff) == 0xFF)
                {
                    Encoding.BigEndianUnicode.GetString(Bytes, 2, Bytes.Length - 2);
                }
                else if ((Bytes[0] & 0xff) == 0xFF && (Bytes[1] & 0xff) == 0xFE)
                {
                    Encoding.Unicode.GetString(Bytes, 2, Bytes.Length - 2);
                }
            }

            // otherwise use PDFDocEncoding
            return PdfDocEncoding.ToString(Bytes);
        }

        /**
         * Returns the content of this string as a PDF <i>ASCII string</i>.
         */
        public string GetAscii()
        {
            return Encoding.ASCII.GetString(Bytes);
        }

        /// <summary>
        /// This will take this string and create a hex representation of the bytes that make the string.
        /// </summary>
        /// <returns>A hex string representing the bytes in this string.</returns>
        public string ToHexString()
        {
            return Hex.GetString(Bytes);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as CosString);
        }

        protected bool Equals(CosString other)
        {
            if (other == null)
            {
                return false;
            }

            var thisString = GetString();
            var otherString = other.GetString();

            return string.Equals(thisString, otherString);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Bytes != null ? Bytes.GetHashCode() : 0) * 397);
            }
        }

        public override object Accept(ICosVisitor visitor)
        {
            return visitor.VisitFromString(this);
        }
    }
}
