namespace UglyToad.PdfPig.Tests.Util
{
    using System;
    using System.Collections.Generic;
    using PdfPig.Util;
    using Xunit;

    public class DateFormatHelperTests
    {
        public static IEnumerable<object[]> PositiveDateData = new[]
        {
            new object[] {"D:20190710205447+01'00'", new DateTimeOffset(2019, 7, 10, 20, 54, 47, TimeSpan.FromHours(1))},
            new object[] {"D:2017", new DateTimeOffset(2017, 1, 1, 0, 0, 0, TimeSpan.Zero)},
            new object[] {"2017", new DateTimeOffset(2017, 1, 1, 0, 0, 0, TimeSpan.Zero)},
            new object[] {"196712", new DateTimeOffset(1967, 12, 1, 0, 0, 0, TimeSpan.Zero)},
            new object[] {"D:196712", new DateTimeOffset(1967, 12, 1, 0, 0, 0, TimeSpan.Zero)},
            new object[] {"D:20100520", new DateTimeOffset(2010, 5, 20, 0, 0, 0, TimeSpan.Zero)},
            new object[] {"20121106", new DateTimeOffset(2012, 11, 6, 0, 0, 0, TimeSpan.Zero)},
            new object[] {"D:2012110623", new DateTimeOffset(2012, 11, 6, 23, 0, 0, TimeSpan.Zero)},
            new object[] {"D:201211061655", new DateTimeOffset(2012, 11, 6, 16, 55, 0, TimeSpan.Zero)},
            new object[] {"D:20121106005512", new DateTimeOffset(2012, 11, 6, 0, 55, 12, TimeSpan.Zero)},
            new object[] {"D:20121106165512Z", new DateTimeOffset(2012, 11, 6, 16, 55, 12, TimeSpan.Zero)},
            new object[] {"20121106165512Z", new DateTimeOffset(2012, 11, 6, 16, 55, 12, TimeSpan.Zero)},
            new object[] {"D:19970915110347-07'30'", new DateTimeOffset(1997, 9, 15, 11, 3, 47, new TimeSpan(-7, -30, 0))},
            new object[] {"D:19990209153925+11'", new DateTimeOffset(1999, 2, 9, 15, 39, 25, TimeSpan.FromHours(11))},
            new object[] {"D:19990209153925-03'", new DateTimeOffset(1999, 2, 9, 15, 39, 25, TimeSpan.FromHours(-3))},
        };

        [Theory]
        [InlineData(default(string))]
        [InlineData("")]
        [InlineData("D:")]
        [InlineData("D:FEHTR$54")]
        [InlineData("D:49454")]
        [InlineData("9454AE")]
        [InlineData("20190107121634!")]
        [InlineData("D:19990209153925+11A")]
        [InlineData("D:19990209153925+11")]
        [InlineData("D:19990209153925E11")]
        [InlineData("D:19993209")]
        [InlineData("D:19990750")]
        [InlineData("D:20100231")]
        public void TryParseDateTimeOffset_InvalidInput_False(string input)
        {
            var result = DateFormatHelper.TryParseDateTimeOffset(input, out _);

            Assert.False(result);
        }

        [Theory]
        [MemberData(nameof(PositiveDateData))]
        public void TryParseDateTimeOffset_ValidDate_True(string input, DateTimeOffset expected)
        {
            var success = DateFormatHelper.TryParseDateTimeOffset(input, out var result);

            Assert.True(success);
            Assert.Equal(expected, result);
        }
    }
}
