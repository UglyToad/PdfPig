using System.Collections.Generic;
using System.Text;
using System.IO;

namespace UglyToad.Pdf.Cos
{
    /// <summary>
    /// The "PDFDocEncoding" encoding. Note that this is *not* a Type 1 font encoding, it is used only within PDF "text strings".
    /// </summary>
    internal static class PdfDocEncoding
    {
        private const char ReplacementCharacter = '\uFFFD';

        private static readonly int[] CodeToUni;
        private static readonly IReadOnlyDictionary<char, int> UnicodeToCode;

        static PdfDocEncoding()
        {
            var temporaryMap = new Dictionary<char, int>(256);
            CodeToUni = new int[256];

            // initialize with basically ISO-8859-1
            for (int i = 0; i < 256; i++)
            {
                // skip entries not in Unicode column
                if (i > 0x17 && i < 0x20)
                {
                    continue;
                }
                if (i > 0x7E && i < 0xA1)
                {
                    continue;
                }
                if (i == 0xAD)
                {
                    continue;
                }


                set(i, (char)i, temporaryMap);
            }

            // then do all deviations (based on the table in ISO 32000-1:2008)
            // block 1
            set(0x18, '\u02D8', temporaryMap); // BREVE
            set(0x19, '\u02C7', temporaryMap); // CARON
            set(0x1A, '\u02C6', temporaryMap); // MODIFIER LETTER CIRCUMFLEX ACCENT
            set(0x1B, '\u02D9', temporaryMap); // DOT ABOVE
            set(0x1C, '\u02DD', temporaryMap); // DOUBLE ACUTE ACCENT
            set(0x1D, '\u02DB', temporaryMap); // OGONEK
            set(0x1E, '\u02DA', temporaryMap); // RING ABOVE
            set(0x1F, '\u02DC', temporaryMap); // SMALL TILDE
                                               // block 2
            set(0x7F, ReplacementCharacter, temporaryMap); // undefined
            set(0x80, '\u2022', temporaryMap); // BULLET
            set(0x81, '\u2020', temporaryMap); // DAGGER
            set(0x82, '\u2021', temporaryMap); // DOUBLE DAGGER
            set(0x83, '\u2026', temporaryMap); // HORIZONTAL ELLIPSIS
            set(0x84, '\u2014', temporaryMap); // EM DASH
            set(0x85, '\u2013', temporaryMap); // EN DASH
            set(0x86, '\u0192', temporaryMap); // LATIN SMALL LETTER SCRIPT F
            set(0x87, '\u2044', temporaryMap); // FRACTION SLASH (solidus)
            set(0x88, '\u2039', temporaryMap); // SINGLE LEFT-POINTING ANGLE QUOTATION MARK
            set(0x89, '\u203A', temporaryMap); // SINGLE RIGHT-POINTING ANGLE QUOTATION MARK
            set(0x8A, '\u2212', temporaryMap); // MINUS SIGN
            set(0x8B, '\u2030', temporaryMap); // PER MILLE SIGN
            set(0x8C, '\u201E', temporaryMap); // DOUBLE LOW-9 QUOTATION MARK (quotedblbase)
            set(0x8D, '\u201C', temporaryMap); // LEFT DOUBLE QUOTATION MARK (quotedblleft)
            set(0x8E, '\u201D', temporaryMap); // RIGHT DOUBLE QUOTATION MARK (quotedblright)
            set(0x8F, '\u2018', temporaryMap); // LEFT SINGLE QUOTATION MARK (quoteleft)
            set(0x90, '\u2019', temporaryMap); // RIGHT SINGLE QUOTATION MARK (quoteright)
            set(0x91, '\u201A', temporaryMap); // SINGLE LOW-9 QUOTATION MARK (quotesinglbase)
            set(0x92, '\u2122', temporaryMap); // TRADE MARK SIGN
            set(0x93, '\uFB01', temporaryMap); // LATIN SMALL LIGATURE FI
            set(0x94, '\uFB02', temporaryMap); // LATIN SMALL LIGATURE FL
            set(0x95, '\u0141', temporaryMap); // LATIN CAPITAL LETTER L WITH STROKE
            set(0x96, '\u0152', temporaryMap); // LATIN CAPITAL LIGATURE OE
            set(0x97, '\u0160', temporaryMap); // LATIN CAPITAL LETTER S WITH CARON
            set(0x98, '\u0178', temporaryMap); // LATIN CAPITAL LETTER Y WITH DIAERESIS
            set(0x99, '\u017D', temporaryMap); // LATIN CAPITAL LETTER Z WITH CARON
            set(0x9A, '\u0131', temporaryMap); // LATIN SMALL LETTER DOTLESS I
            set(0x9B, '\u0142', temporaryMap); // LATIN SMALL LETTER L WITH STROKE
            set(0x9C, '\u0153', temporaryMap); // LATIN SMALL LIGATURE OE
            set(0x9D, '\u0161', temporaryMap); // LATIN SMALL LETTER S WITH CARON
            set(0x9E, '\u017E', temporaryMap); // LATIN SMALL LETTER Z WITH CARON
            set(0x9F, ReplacementCharacter, temporaryMap); // undefined
            set(0xA0, '\u20AC', temporaryMap); // EURO SIGN
                                               // end of deviations

            UnicodeToCode = temporaryMap;
        }

        private static void set(int code, char unicode, Dictionary<char, int> unicodeToCode)
        {
            CodeToUni[code] = unicode;
            unicodeToCode.Add(unicode, code);
        }

        /**
         * Returns the string representation of the given PDFDocEncoded bytes.
         */
        public static string ToString(byte[] bytes)
        {
            if (bytes == null)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            foreach (var b in bytes)
            {
                if ((b & 0xff) >= CodeToUni.Length)
                {
                    sb.Append('?');
                }
                else
                {
                    sb.Append((char)CodeToUni[b & 0xff]);
                }
            }

            return sb.ToString();
        }


        /// <summary>
        /// Returns the given string encoded with PDFDocEncoding.
        /// </summary>
        public static byte[] GetBytes(string text)
        {
            using (var memoryStream = new MemoryStream())
            using (var write = new StreamWriter(memoryStream))
            {
                foreach (var c in text)
                {
                    if (!UnicodeToCode.TryGetValue(c, out int value))
                    {
                        write.Write(0);
                    }
                    else
                    {
                        write.Write(value);
                    }
                }

                return memoryStream.ToArray();
            }
        }


        /// <summary>
        /// Returns true if the given character is available in PDFDocEncoding.
        /// </summary>
        /// <param name="character">UTF-16 character</param>
        public static bool ContainsChar(char character)
        {
            return UnicodeToCode.ContainsKey(character);
        }
    }
}
