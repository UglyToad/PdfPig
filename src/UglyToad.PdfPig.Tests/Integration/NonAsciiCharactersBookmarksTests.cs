namespace UglyToad.PdfPig.Tests.Integration
{
    using UglyToad.PdfPig.Content;
    using UglyToad.PdfPig.Outline;
    using UglyToad.PdfPig.Outline.Destinations;
    using UglyToad.PdfPig.Writer;

    public class NonAsciiCharactersBookmarksTests
    {
        [Theory]
        [MemberData(nameof(TestData.TestData_Pass), MemberType = typeof(TestData))]
        [MemberData(nameof(TestData.TestData_Failed), MemberType = typeof(TestData))]
        public void CanGetBookmarks(string words)
        {
            using var builder = new PdfDocumentBuilder();
            builder.AddPage(PageSize.A4);

            // Set bookmark items.
            var inputs = words.Split(" ", StringSplitOptions.RemoveEmptyEntries);
            builder.Bookmarks = new Bookmarks(inputs.Select(x => new DocumentBookmarkNode(x,
                0,
                new ExplicitDestination(1,
                    ExplicitDestinationType.XyzCoordinates,
                    ExplicitDestinationCoordinates.Empty),
                Array.Empty<BookmarkNode>())).ToArray());

            // Build PDF data
            var bytes = builder.Build();

            // Read PDF from bytes. And read bookmark data.
            using var doc = PdfDocument.Open(bytes);
            bool isSuccess = doc.TryGetBookmarks(out var bookmarks);

            // Assert
            Assert.True(isSuccess);
            var results = bookmarks.GetNodes().Select(x => x.Title).ToArray();
            Assert.Equivalent(inputs, results);
        }

        private static class TestData
        {
            public static TheoryData<string> TestData_Failed = new TheoryData<string>
            {
                "ШЩＨＩ차岸岩還館小少尚",
                "A Ш Z", // CYRILLIC CAPITAL LETTER
                "AШZ", // CYRILLIC CAPITAL LETTER
                "A Щ Z", // CYRILLIC CAPITAL LETTER
                "AЩZ", // CYRILLIC CAPITAL LETTER
                "Ｈ Ｉ", // FULLWIDTH LATIN CAPITAL LETTER A
                "ＨＩ", // FULLWIDTH LATIN CAPITAL LETTER A
                "차", // HANGUL
                "岸 岩", // KANJI
                "岸岩", // KANJI
                "還 館", // KANJI
                "還館", // KANJI
                "小 少 尚", // KANJI
                "小少尚", // KANJI
            };

            public static TheoryData<string> TestData_Pass = new TheoryData<string>
            {
                // FRENCH Alphabet Diacritics and ligatures
                "É À È Ù Â Ê Î Ô Û Ë Ï Ü Ç Œ Æ",
                "é à è ù â ê î ô û ë ï ü ç œ æ",

                // GREEK Alphabet
                "Α Β Γ Δ Ε Ζ Η Θ Ι Κ Λ Μ Ν Ξ Ο Π Ρ Σ Τ Υ Φ Χ Ψ Ω",
                "α β γ δ ε ζ η θ ι κ λ μ ν ξ ο π ρ σ τ υ φ χ ψ ω",

                // CYRILLIC CAPITAL LETTER
                // "А Б В Г Д Е Ж З И К Л М Н О П Р С Т У Ф Х Ц Ч Ш Щ Ы Э Ю Я", 

                // CYRILLIC SMALL LETTER
                "а б в г д е ж з и к л м н о п р с т у ф х ц ч ш щ ы э ю я",

                // HANGUL CHOSEONG
                "ㄱ ㄴ ㄷ ㄹ ㅁ ㅂ ㅅ ㅇ ㅈ ㅊ ㅋ ㅌ ㅍ ㅎ",

                // HANGUL GANADA
                //"가 나 다 라 마 바 사 아 자 차 카 타 파 하",

                // FULLWIDTH LATIN CAPITAL LETTER
                // "Ａ Ｂ Ｃ Ｄ Ｅ Ｆ Ｇ Ｈ Ｉ Ｊ Ｋ Ｌ Ｍ Ｎ Ｏ Ｐ Ｑ Ｒ Ｓ Ｔ Ｕ Ｖ Ｗ Ｘ Ｙ Ｚ", 

                // FULLWIDTH LATIN SMALL LETTER
                "ａ ｂ ｃ ｄ ｅ ｆ ｇ ｈ ｉ ｊ ｋ ｌ ｍ ｎ ｏ ｐ ｑ ｒ ｓ ｔ ｕ ｖ ｗ ｘ ｙ ｚ",

                // Halfwidth Katakana
                "ｱ ｲ ｳ ｴ ｵ ｶ ｷ ｸ ｹ ｺ ｻ ｼ ｽ ｾ ｿ ﾀ ﾁ ﾂ ﾃ ﾄ ﾅ ﾆ ﾇ ﾈ ﾉ ﾊ ﾋ ﾌ ﾍ ﾎ ﾏ ﾐ ﾑ ﾒ ﾓ ﾔ ﾕ ﾖ ﾗ ﾘ ﾙ ﾚ ﾛ ﾜ ｦ ﾝ",

                // Fullwidth Katakana
                "ア イ ウ エ オ カ キ ク ケ コ サ シ ス セ ソ タ チ ツ テ ト ナ ニ ヌ ネ ノ ハ ヒ フ ヘ ホ マ ミ ム メ モ ヤ ユ ヨ ラ リ ル レ ロ ワ ヲ ン",

                // Fullwidth Hiragana
                "あ い う え お か き く け こ さ し す せ そ た ち つ て と な に ぬ ね の は ひ ふ へ ほ ま み む め も や ゆ よ ら り る れ ろ わ を ん",

                // Kanji (Surrogate Pair)
                "𩸽 𩹉 𡵅",

                // Emoji
                "🏠 🚗 📝",

                // Emoji (with ZWJ Sequences)
                "👨‍💻 👁‍🗨 😶‍🌫️"
            };
        }
    }
}
