namespace UglyToad.PdfPig.Core
{
    using System.Collections.Generic;

    /// <summary>
    /// <para>
    /// PDFDocEncoding, defined in the spec for text strings in PDF objects (but not content stream contents,
    /// Type 1 font contents, etc).
    /// </para>
    /// <para>
    /// Matches ASCII for code points 32 - 126.
    /// </para>
    /// </summary>
    public static class PdfDocEncoding
    {
        private static readonly Dictionary<char, byte> UnicodeToCode = new Dictionary<char, byte>();
        private static readonly Dictionary<byte, char> CodeToUnicode = new Dictionary<byte, char>
            {
                {9, '\u0009'},
                {10, '\u000A'},
                {13, '\u000D'},
                {24, '\u02D8'},
                {25, '\u02C7'},
                {26, '\u02C6'},
                {27, '\u02D9'},
                {28, '\u02DD'},
                {29, '\u02DB'},
                {30, '\u02DA'},
                {31, '\u02DC'},
                {32, '\u0020'},
                {33, '\u0021'},
                {34, '\u0022'},
                {35, '\u0023'},
                {36, '\u0024'},
                {37, '\u0025'},
                {38, '\u0026'},
                {39, '\u0027'},
                {40, '\u0028'},
                {41, '\u0029'},
                {42, '\u002A'},
                {43, '\u002B'},
                {44, '\u002C'},
                {45, '\u002D'},
                {46, '\u002E'},
                {47, '\u002F'},
                {48, '\u0030'},
                {49, '\u0031'},
                {50, '\u0032'},
                {51, '\u0033'},
                {52, '\u0034'},
                {53, '\u0035'},
                {54, '\u0036'},
                {55, '\u0037'},
                {56, '\u0038'},
                {57, '\u0039'},
                {58, '\u003A'},
                {59, '\u003B'},
                {60, '\u003C'},
                {61, '\u003D'},
                {62, '\u003E'},
                {63, '\u003F'},
                {64, '\u0040'},
                {65, '\u0041'},
                {66, '\u0042'},
                {67, '\u0043'},
                {68, '\u0044'},
                {69, '\u0045'},
                {70, '\u0046'},
                {71, '\u0047'},
                {72, '\u0048'},
                {73, '\u0049'},
                {74, '\u004A'},
                {75, '\u004B'},
                {76, '\u004C'},
                {77, '\u004D'},
                {78, '\u004E'},
                {79, '\u004F'},
                {80, '\u0050'},
                {81, '\u0051'},
                {82, '\u0052'},
                {83, '\u0053'},
                {84, '\u0054'},
                {85, '\u0055'},
                {86, '\u0056'},
                {87, '\u0057'},
                {88, '\u0058'},
                {89, '\u0059'},
                {90, '\u005A'},
                {91, '\u005B'},
                {92, '\u005C'},
                {93, '\u005D'},
                {94, '\u005E'},
                {95, '\u005F'},
                {96, '\u0060'},
                {97, '\u0061'},
                {98, '\u0062'},
                {99, '\u0063'},
                {100, '\u0064'},
                {101, '\u0065'},
                {102, '\u0066'},
                {103, '\u0067'},
                {104, '\u0068'},
                {105, '\u0069'},
                {106, '\u006A'},
                {107, '\u006B'},
                {108, '\u006C'},
                {109, '\u006D'},
                {110, '\u006E'},
                {111, '\u006F'},
                {112, '\u0070'},
                {113, '\u0071'},
                {114, '\u0072'},
                {115, '\u0073'},
                {116, '\u0074'},
                {117, '\u0075'},
                {118, '\u0076'},
                {119, '\u0077'},
                {120, '\u0078'},
                {121, '\u0079'},
                {122, '\u007A'},
                {123, '\u007B'},
                {124, '\u007C'},
                {125, '\u007D'},
                {126, '\u007E'},
                {128, '\u2022'},
                {129, '\u2020'},
                {130, '\u2021'},
                {131, '\u2026'},
                {132, '\u2014'},
                {133, '\u2013'},
                {134, '\u0192'},
                {135, '\u2044'},
                {136, '\u2039'},
                {137, '\u203A'},
                {138, '\u2212'},
                {139, '\u2030'},
                {140, '\u201E'},
                {141, '\u201C'},
                {142, '\u201D'},
                {143, '\u2018'},
                {144, '\u2019'},
                {145, '\u201A'},
                {146, '\u2122'},
                {147, '\uFB01'},
                {148, '\uFB02'},
                {149, '\u0141'},
                {150, '\u0152'},
                {151, '\u0160'},
                {152, '\u0178'},
                {153, '\u017D'},
                {154, '\u0131'},
                {155, '\u0142'},
                {156, '\u0153'},
                {157, '\u0161'},
                {158, '\u017E'},
                {160, '\u20AC'},
                {161, '\u00A1'},
                {162, '\u00A2'},
                {163, '\u00A3'},
                {164, '\u00A4'},
                {165, '\u00A5'},
                {166, '\u00A6'},
                {167, '\u00A7'},
                {168, '\u00A8'},
                {169, '\u00A9'},
                {170, '\u00AA'},
                {171, '\u00AB'},
                {172, '\u00AC'},
                {174, '\u00AE'},
                {175, '\u00AF'},
                {176, '\u00B0'},
                {177, '\u00B1'},
                {178, '\u00B2'},
                {179, '\u00B3'},
                {180, '\u00B4'},
                {181, '\u00B5'},
                {182, '\u00B6'},
                {183, '\u00B7'},
                {184, '\u00B8'},
                {185, '\u00B9'},
                {186, '\u00BA'},
                {187, '\u00BB'},
                {188, '\u00BC'},
                {189, '\u00BD'},
                {190, '\u00BE'},
                {191, '\u00BF'},
                {192, '\u00C0'},
                {193, '\u00C1'},
                {194, '\u00C2'},
                {195, '\u00C3'},
                {196, '\u00C4'},
                {197, '\u00C5'},
                {198, '\u00C6'},
                {199, '\u00C7'},
                {200, '\u00C8'},
                {201, '\u00C9'},
                {202, '\u00CA'},
                {203, '\u00CB'},
                {204, '\u00CC'},
                {205, '\u00CD'},
                {206, '\u00CE'},
                {207, '\u00CF'},
                {208, '\u00D0'},
                {209, '\u00D1'},
                {210, '\u00D2'},
                {211, '\u00D3'},
                {212, '\u00D4'},
                {213, '\u00D5'},
                {214, '\u00D6'},
                {215, '\u00D7'},
                {216, '\u00D8'},
                {217, '\u00D9'},
                {218, '\u00DA'},
                {219, '\u00DB'},
                {220, '\u00DC'},
                {221, '\u00DD'},
                {222, '\u00DE'},
                {223, '\u00DF'},
                {224, '\u00E0'},
                {225, '\u00E1'},
                {226, '\u00E2'},
                {227, '\u00E3'},
                {228, '\u00E4'},
                {229, '\u00E5'},
                {230, '\u00E6'},
                {231, '\u00E7'},
                {232, '\u00E8'},
                {233, '\u00E9'},
                {234, '\u00EA'},
                {235, '\u00EB'},
                {236, '\u00EC'},
                {237, '\u00ED'},
                {238, '\u00EE'},
                {239, '\u00EF'},
                {240, '\u00F0'},
                {241, '\u00F1'},
                {242, '\u00F2'},
                {243, '\u00F3'},
                {244, '\u00F4'},
                {245, '\u00F5'},
                {246, '\u00F6'},
                {247, '\u00F7'},
                {248, '\u00F8'},
                {249, '\u00F9'},
                {250, '\u00FA'},
                {251, '\u00FB'},
                {252, '\u00FC'},
                {253, '\u00FD'},
                {254, '\u00FE'},
                {255, '\u00FF'}
            };

        static PdfDocEncoding()
        {
            foreach (var c in CodeToUnicode)
            {
                UnicodeToCode.Add(c.Value, c.Key);
            }
        }


        /// <summary>
        /// Try to convert raw bytes to a PdfDocEncoding encoded string. If unsupported characters are encountered
        /// meaning we cannot safely round-trip the value to bytes this will instead return false.
        /// </summary>
        public static bool TryConvertBytesToString(byte[] bytes, out string result)
        {
            result = null;
            if (bytes.Length == 0)
            {
                result = string.Empty;
                return true;
            }

            var arr = new char[bytes.Length];

            for (var i = 0; i < bytes.Length; i++)
            {
                var b = bytes[i];

                if (!CodeToUnicode.TryGetValue(b, out var c))
                {
                    return false;
                }

                arr[i] = c;
            }

            result = new string(arr);
            return true;
        }

        /// <summary>
        /// Map from string back to bytes. This is not a reversible operation for all inputs.
        /// </summary>
        public static byte[] StringToBytes(string s)
        {
            var result = new byte[s.Length];
            for (int i = 0; i < s.Length; i++)
            {
                var c = s[i];

                if (UnicodeToCode.TryGetValue(c, out var b))
                {
                    result[i] = b;
                }
            }

            return result;
        }
    }
}
