﻿namespace UglyToad.PdfPig.Tests.Filters
{
    using System.Text;
    using PdfPig.Filters;
    using PdfPig.Tokens;

    public class Ascii85FilterTests
    {
        private readonly Ascii85Filter filter = new Ascii85Filter();

        private readonly DictionaryToken dictionary = new DictionaryToken(new Dictionary<NameToken, IToken>());

        [Fact]
        public void DecodesWikipediaExample()
        {
            var bytes = Encoding.ASCII.GetBytes(
                @"9jqo^BlbD-BleB1DJ+*+F(f,q/0JhKF<GL>Cj@.4Gp$d7F!,L7@<6@)/0JDEF<G%<+EV:2F!,
O<DJ+*.@<*K0@<6L(Df-\0Ec5e;DffZ(EZee.Bl.9pF""AGXBPCsi + DGm >@3BB / F * &OCAfu2 / AKY
            i(DIb: @FD, *) + C]U =@3BN#EcYf8ATD3s@q?d$AftVqCh[NqF<G:8+EV:.+Cf>-FD5W8ARlolDIa
            l(DId<j@<? 3r@:F % a + D58'ATD4$Bl@l3De:,-DJs`8ARoFb/0JMK@qB4^F!,R<AKZ&-DfTqBG%G
                > uD.RTpAKYo'+CT/5+Cei#DII?(E,9)oF*2M7/c~>");

            var result = filter.Decode(bytes, dictionary, TestFilterProvider.Instance, 0);

#if !NET
            string text = Encoding.ASCII.GetString(result.ToArray());
#else
            string text = Encoding.ASCII.GetString(result.Span);
#endif

            Assert.Equal("Man is distinguished, not only by his reason, but by this singular passion from other animals, which is a lust of the mind, " +
                         "that by a perseverance of delight in the continued and indefatigable generation of knowledge, " +
                         "exceeds the short vehemence of any carnal pleasure.",
                text);
        }

        [Theory]
        [InlineData("BE", "h")]
        [InlineData("BOq", "he")]
        [InlineData("BOtu", "hel")]
        [InlineData("BOu!r", "hell")]
        [InlineData("BOu!rDZ", "hello")]
        [InlineData("BOu!rD]f", "hello ")]
        [InlineData("BOu!rD]j6", "hello w")]
        [InlineData("BOu!rD]j7B", "hello wo")]
        [InlineData("BOu!rD]j7BEW", "hello wor")]
        [InlineData("BOu!rD]j7BEbk", "hello worl")]
        [InlineData("BOu!rD]j7BEbo7", "hello world")]
        [InlineData("BOu!rD]j7BEbo80", "hello world!")]
        public void DecodesHelloWorld(string encoded, string decoded)
        {
            var result = filter.Decode(
                Encoding.ASCII.GetBytes(encoded),
                dictionary,
                TestFilterProvider.Instance,
                0);

            Assert.Equal(decoded, Encoding.ASCII.GetString(result.ToArray()));
        }

        [Theory]
        [InlineData("9jqo^zBlbD-", "Man \0\0\0\0is d")]
        [InlineData("", "")]
        [InlineData("z", "\0\0\0\0")]
        [InlineData("zz", "\0\0\0\0\0\0\0\0")]
        [InlineData("zzz", "\0\0\0\0\0\0\0\0\0\0\0\0")]
        public void ReplacesZWithEmptyBytes(string encoded, string decoded)
        {
            var bytes = Encoding.ASCII.GetBytes(encoded);

            var result = filter.Decode(bytes, dictionary, TestFilterProvider.Instance, 1);

#if !NET
            string text = Encoding.ASCII.GetString(result.ToArray());
#else
            string text = Encoding.ASCII.GetString(result.Span);
#endif

            Assert.Equal(decoded, text);
        }
        
        [Fact]
        public void ZInMiddleOf5CharacterSequenceThrows()
        {
            var bytes = Encoding.ASCII.GetBytes("qjzqo^");

            Action action = () => filter.Decode(bytes, dictionary, TestFilterProvider.Instance, 0);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Theory]
        [InlineData("@rH:%B", "cool")]
        [InlineData("A~>", "")]
        [InlineData("@rH:%A~>", "cool")]
        public void SingleCharacterLastIgnores(string encoded, string decoded)
        {
            var bytes = Encoding.ASCII.GetBytes(encoded);

            var result = filter.Decode(bytes, dictionary, TestFilterProvider.Instance, 1);

            Assert.Equal(decoded, Encoding.ASCII.GetString(result.ToArray()));
        }

        private const string PdfContent = @"1 0 obj
<< /Length 568 >>
stream
2 J
BT
/F1 12 Tf
0 Tc
0 Tw
72.5 712 TD
[(Unencoded streams can be read easily) 65 (, )] TJ
0 -14 TD
[(b) 20 (ut generally tak) 10 (e more space than \311)] TJ
T* (encoded streams.) Tj
0 -28 TD
[(Se) 25 (v) 15 (eral encoding methods are a) 20 (v) 25 (ailable in PDF) 80 (.)] TJ
0 -14 TD
(Some are used for compression and others simply) Tj
T* [(to represent binary data in an ) 55 (ASCII format.)] TJ
T* (Some of the compression encoding methods are \
suitable ) Tj
T* (for both data and images, while others are \
suitable only ) Tj
T* (for continuous-tone images.) Tj
ET
endstream
endobj";

        [Fact]
        public void DecodesEncodedPdfContent()
        {
            const string input =
                @"0d&.mDdmGg4?O`>9P&*SFD)dS2E2gC4pl@QEb/Zr$8N_r$:7]!01IZ=0eskNAdU47<+?7h+B3Ol2_m!C+?)#1+B1
`9>:<KhASu!rA7]9oF*)G6@;U'.@ps6t@V$[&ART*lARTXoCj@HP2DlU*/0HBI+B1r?0H_r%1a#ac$<nof.3LB""+=MAS+D58'ATD3qCj@.
           F@;@;70ea^uAKYi.Eb-A7E+*6f+EV:*DBN1?0ek+_+B1r?<%9""=ASu!rA7]9oF*)G6@;U'<.3MT)$8<SS1,pCU6jd-H;e7
C#1,U1&Ft""Og2'=;YEa`c,ASu!rA8,po+Dk\3BQ%F&+CT;%+CQ]A1,'h!Ft""Oh2'=;UBl%3eCh4`'DBMbD7O]H>0H_br.:""&q8d[6p/M
T()<(%'A;f?Ma+CT;%+E_a:A0>K&EZek1D/aN,F)u&6DBNA*A0>f4BOu4*+EM76E,9eK+B3(_<%9""p.!0AMEb031ATMF#F<G%,DIIR2+Cno
&@3B9%+CT.1.3LK*+=KNS6V0ilAoD^,@<=+N>p**=$</Jt-rY&$AKYo'+EV:.+Cf>,E,oN2F(oQ1+D#G#De*R""B-;&&FD,T'F!+n3AKY4b
F*22=@:F%a+=SF4C'moi+=Li?EZeh0FD)e-@<>p#@;]TuBl.9kATKCFGA(],AKYo5BOu4*+CT;%+C#7pF_Pr+@VfTuDf0B:+=SF4C'moi+=
Li?EZek1DKKT1F`2DD/TboKAKY](@:s.m/h%oBC'mC/$>""*cF*)G6@;Q?_DIdZpC&~>";

            var result = filter.Decode(Encoding.ASCII.GetBytes(input), dictionary, TestFilterProvider.Instance, 0);

#if !NET
            string text = Encoding.ASCII.GetString(result.ToArray());
#else
            string text = Encoding.ASCII.GetString(result.Span);
#endif

            Assert.Equal(PdfContent.Replace("\r\n", "\n"), text);
        }

        [Fact]
        public void DecodesEncodedPdfContentMissingEndOfDataSymbol()
        {
            const string input =
                @"0d&.mDdmGg4?O`>9P&*SFD)dS2E2gC4pl@QEb/Zr$8N_r$:7]!01IZ=0eskNAdU47<+?7h+B3Ol2_m!C+?)#1+B1
`9>:<KhASu!rA7]9oF*)G6@;U'.@ps6t@V$[&ART*lARTXoCj@HP2DlU*/0HBI+B1r?0H_r%1a#ac$<nof.3LB""+=MAS+D58'ATD3qCj@.
           F@;@;70ea^uAKYi.Eb-A7E+*6f+EV:*DBN1?0ek+_+B1r?<%9""=ASu!rA7]9oF*)G6@;U'<.3MT)$8<SS1,pCU6jd-H;e7
C#1,U1&Ft""Og2'=;YEa`c,ASu!rA8,po+Dk\3BQ%F&+CT;%+CQ]A1,'h!Ft""Oh2'=;UBl%3eCh4`'DBMbD7O]H>0H_br.:""&q8d[6p/M
T()<(%'A;f?Ma+CT;%+E_a:A0>K&EZek1D/aN,F)u&6DBNA*A0>f4BOu4*+EM76E,9eK+B3(_<%9""p.!0AMEb031ATMF#F<G%,DIIR2+Cno
&@3B9%+CT.1.3LK*+=KNS6V0ilAoD^,@<=+N>p**=$</Jt-rY&$AKYo'+EV:.+Cf>,E,oN2F(oQ1+D#G#De*R""B-;&&FD,T'F!+n3AKY4b
F*22=@:F%a+=SF4C'moi+=Li?EZeh0FD)e-@<>p#@;]TuBl.9kATKCFGA(],AKYo5BOu4*+CT;%+C#7pF_Pr+@VfTuDf0B:+=SF4C'moi+=
Li?EZek1DKKT1F`2DD/TboKAKY](@:s.m/h%oBC'mC/$>""*cF*)G6@;Q?_DIdZpC&";

            var result = filter.Decode(Encoding.ASCII.GetBytes(input), dictionary, TestFilterProvider.Instance, 0);

#if !NET
            string text = Encoding.ASCII.GetString(result.ToArray());
#else
            string text = Encoding.ASCII.GetString(result.Span);
#endif

            Assert.Equal(PdfContent.Replace("\r\n", "\n"), text);
        }

        [Fact]
        public void DecodeParallel()
        {
            Parallel.For(0, 100_000, i =>
            {
                if (i % 2 == 0)
                {
                    var bytes = Encoding.ASCII.GetBytes("9jqo^zBlbD-");

                    var result = filter.Decode(bytes, dictionary, TestFilterProvider.Instance, 1);

#if !NET
                    string text = Encoding.ASCII.GetString(result.ToArray());
#else
                    string text = Encoding.ASCII.GetString(result.Span);
#endif

                    Assert.Equal("Man \0\0\0\0is d", text);
                }
                else
                {
                    const string input =
                        @"0d&.mDdmGg4?O`>9P&*SFD)dS2E2gC4pl@QEb/Zr$8N_r$:7]!01IZ=0eskNAdU47<+?7h+B3Ol2_m!C+?)#1+B1
`9>:<KhASu!rA7]9oF*)G6@;U'.@ps6t@V$[&ART*lARTXoCj@HP2DlU*/0HBI+B1r?0H_r%1a#ac$<nof.3LB""+=MAS+D58'ATD3qCj@.
           F@;@;70ea^uAKYi.Eb-A7E+*6f+EV:*DBN1?0ek+_+B1r?<%9""=ASu!rA7]9oF*)G6@;U'<.3MT)$8<SS1,pCU6jd-H;e7
C#1,U1&Ft""Og2'=;YEa`c,ASu!rA8,po+Dk\3BQ%F&+CT;%+CQ]A1,'h!Ft""Oh2'=;UBl%3eCh4`'DBMbD7O]H>0H_br.:""&q8d[6p/M
T()<(%'A;f?Ma+CT;%+E_a:A0>K&EZek1D/aN,F)u&6DBNA*A0>f4BOu4*+EM76E,9eK+B3(_<%9""p.!0AMEb031ATMF#F<G%,DIIR2+Cno
&@3B9%+CT.1.3LK*+=KNS6V0ilAoD^,@<=+N>p**=$</Jt-rY&$AKYo'+EV:.+Cf>,E,oN2F(oQ1+D#G#De*R""B-;&&FD,T'F!+n3AKY4b
F*22=@:F%a+=SF4C'moi+=Li?EZeh0FD)e-@<>p#@;]TuBl.9kATKCFGA(],AKYo5BOu4*+CT;%+C#7pF_Pr+@VfTuDf0B:+=SF4C'moi+=
Li?EZek1DKKT1F`2DD/TboKAKY](@:s.m/h%oBC'mC/$>""*cF*)G6@;Q?_DIdZpC&~>";

                    var result = filter.Decode(Encoding.ASCII.GetBytes(input), dictionary, TestFilterProvider.Instance, 0);

#if !NET
                    string text = Encoding.ASCII.GetString(result.ToArray());
#else
                    string text = Encoding.ASCII.GetString(result.Span);
#endif

                    Assert.Equal(PdfContent.Replace("\r\n", "\n"), text);
                }
            });
        }
    }
}
