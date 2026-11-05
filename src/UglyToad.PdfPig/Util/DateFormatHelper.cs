namespace UglyToad.PdfPig.Util
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Helper class for dates.
    /// </summary>
    public static class DateFormatHelper
    {
        // The lowest and highest values of month, day, hour, minute and second.
        private static readonly int[] FieldMinimums = [1, 1, 0, 0, 0];
        private static readonly int[] FieldMaximums = [12, 31, 23, 59, 59];

        /// <summary>
        /// Try parsing a pdf formatted date string into a <see cref="DateTimeOffset"/>.
        /// <para>Date values used in a PDF shall conform to a standard date format, which closely 
        /// follows that of the international standard ASN.1, defined in ISO/IEC 8824. A date shall be a text string 
        /// of the form (D:YYYYMMDDHHmmSSOHH'mm).</para>
        /// <para>The form of the PDF 1.7 reference, with an apostrophe after the minutes of the offset, is also accepted.</para>
        /// </summary>
        /// <param name="s">The pdf formated date string, e.g. D:199812231952-08'00.</param>
        /// <param name="offset">The parsed date.</param>
        /// <returns>True if parsed.</returns>
        public static bool TryParseDateTimeOffset(string s, out DateTimeOffset offset)
        {
            offset = DateTimeOffset.MinValue;

            bool HasRemainingCharacters(int pos, int len)
            {
                return pos + len <= s.Length;
            }

            bool IsAtEnd(int pos)
            {
                return pos == s.Length;
            }

            bool IsWithinRange(int val, int min, int max)
            {
                return val >= min && val <= max;
            }

            bool TryParseNumber(int pos, int len, out int val)
            {
                return int.TryParse(s.AsSpanOrSubstring(pos, len), NumberStyles.Integer, CultureInfo.InvariantCulture, out val);
            }

            bool IsOffsetStart(int pos)
            {
                return s[pos] == '+' || s[pos] == '-' || s[pos] == 'Z';
            }

            if (s is null || s.Length < 4)
            {
                return false;
            }

            try
            {
                var location = 0;
                if (s[0] == 'D' && s[1] == ':')
                {
                    location = 2;
                }

                if (!HasRemainingCharacters(location, 4) || !TryParseNumber(location, 4, out var year))
                {
                    return false;
                }

                location += 4;

                // Month, day, hour, minute and second, each only present if the ones before it are. Month and day default to
                // 01, the others to zero. The offset from UT may follow any of them, as in the example D:199812231952-08'00
                // of ISO 32000-1, 7.9.4.
                int[] fields = [1, 1, 0, 0, 0];
                for (var i = 0; i < fields.Length && !IsAtEnd(location) && !(i > 0 && IsOffsetStart(location)); i++)
                {
                    if (!HasRemainingCharacters(location, 2)
                        || !TryParseNumber(location, 2, out var value)
                        || !IsWithinRange(value, FieldMinimums[i], FieldMaximums[i]))
                    {
                        return false;
                    }

                    fields[i] = value;
                    location += 2;
                }

                var date = new DateTime(year, fields[0], fields[1], fields[2], fields[3], fields[4]);

                if (IsAtEnd(location))
                {
                    offset = new DateTimeOffset(date, TimeSpan.Zero);

                    return true;
                }

                if (!IsOffsetStart(location))
                {
                    return false;
                }

                var sign = s[location] == '-' ? -1 :
                    s[location] == '+' ? 1 : 0;

                location++;

                // The offset is HH, then an apostrophe and mm, each only present if the ones before it are. As in the PDF 1.7
                // reference, an apostrophe may also follow mm.
                var hoursOffset = 0;
                var minutesOffset = 0;
                if (!IsAtEnd(location))
                {
                    if (!HasRemainingCharacters(location, 2)
                        || !TryParseNumber(location, 2, out hoursOffset)
                        || !IsWithinRange(hoursOffset, 0, 23))
                    {
                        return false;
                    }

                    location += 2;
                }

                if (!IsAtEnd(location))
                {
                    if (s[location] != '\'')
                    {
                        return false;
                    }

                    location++;
                }

                if (!IsAtEnd(location))
                {
                    if (!HasRemainingCharacters(location, 2)
                        || !TryParseNumber(location, 2, out minutesOffset)
                        || !IsWithinRange(minutesOffset, 0, 59))
                    {
                        return false;
                    }

                    location += 2;
                }

                if (!IsAtEnd(location))
                {
                    if (s[location] != '\'')
                    {
                        return false;
                    }

                    location++;
                }

                if (!IsAtEnd(location))
                {
                    return false;
                }

                offset = new DateTimeOffset(date, new TimeSpan(hoursOffset * sign, minutesOffset * sign, 0));

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
