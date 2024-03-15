namespace UglyToad.PdfPig.Util
{
    using System;

    /// <summary>
    /// Helper class for dates.
    /// </summary>
    public static class DateFormatHelper
    {
        /// <summary>
        /// Try parsing a pdf formatted date string into a <see cref="DateTimeOffset"/>.
        /// <para>Date values used in a PDF shall conform to a standard date format, which closely 
        /// follows that of the international standard ASN.1, defined in ISO/IEC 8824. A date shall be a text string 
        /// of the form (D:YYYYMMDDHHmmSSOHH'mm).</para>
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

                if (!HasRemainingCharacters(location, 4))
                {
                    return false;
                }

                if (!int.TryParse(s.Substring(location, 4), out var year))
                {
                    return false;
                }

                location += 4;

                if (!HasRemainingCharacters(location, 2))
                {
                    if (!IsAtEnd(location))
                    {
                        return false;
                    }

                    offset = new DateTimeOffset(year, 1, 1, 0, 0, 0, TimeSpan.Zero);

                    return true;
                }

                if (!int.TryParse(s.Substring(location, 2), out var month)
                    || !IsWithinRange(month, 1, 12))
                {
                    return false;
                }

                location += 2;

                if (!HasRemainingCharacters(location, 2))
                {
                    if (!IsAtEnd(location))
                    {
                        return false;
                    }

                    offset = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);

                    return true;
                }

                if (!int.TryParse(s.Substring(location, 2), out var day)
                    || !IsWithinRange(day, 1, 31))
                {
                    return false;
                }

                location += 2;

                if (!HasRemainingCharacters(location, 2))
                {
                    if (!IsAtEnd(location))
                    {
                        return false;
                    }

                    offset = new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero);

                    return true;
                }

                if (!int.TryParse(s.Substring(location, 2), out var hour)
                    || !IsWithinRange(hour, 0, 23))
                {
                    return false;
                }

                location += 2;

                if (!HasRemainingCharacters(location, 2))
                {
                    if (!IsAtEnd(location))
                    {
                        return false;
                    }

                    offset = new DateTimeOffset(year, month, day, hour, 0, 0, TimeSpan.Zero);

                    return true;
                }

                if (!int.TryParse(s.Substring(location, 2), out var minute)
                    || !IsWithinRange(minute, 0, 59))
                {
                    return false;
                }

                location += 2;

                if (!HasRemainingCharacters(location, 2))
                {
                    if (!IsAtEnd(location))
                    {
                        return false;
                    }

                    offset = new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero);

                    return true;
                }

                if (!int.TryParse(s.Substring(location, 2), out var second)
                    || !IsWithinRange(second, 0, 59))
                {
                    return false;
                }

                location += 2;

                if (!HasRemainingCharacters(location, 1))
                {
                    if (!IsAtEnd(location))
                    {
                        return false;
                    }

                    offset = new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);

                    return true;
                }

                var o = s[location++];

                if (o != '-' && o != '+' && o != 'Z')
                {
                    return false;
                }

                var sign = o == '-' ? -1 :
                    o == '+' ? 1 : 0;

                if (IsAtEnd(location))
                {
                    offset = new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.Zero);

                    return true;
                }

                if (!HasRemainingCharacters(location, 3) || !int.TryParse(s.Substring(location, 2), out var hoursOffset)
                                                         || s[location + 2] != '\''
                                                         || !IsWithinRange(hoursOffset, 0, 23))
                {
                    return false;
                }

                location += 3;

                if (IsAtEnd(location))
                {
                    offset = new DateTimeOffset(year, month, day, hour, minute, second, TimeSpan.FromHours(hoursOffset * sign));

                    return true;
                }

                if (!HasRemainingCharacters(location, 3) || !int.TryParse(s.Substring(location, 2), out var minutesOffset)
                                                         || s[location + 2] != '\''
                                                         || !IsWithinRange(minutesOffset, 0, 59))
                {
                    return false;
                }

                location += 3;

                if (IsAtEnd(location))
                {
                    offset = new DateTimeOffset(year, month, day, hour, minute, second, new TimeSpan(hoursOffset * sign, minutesOffset * sign, 0));

                    return true;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
