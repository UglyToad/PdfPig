namespace UglyToad.PdfPig.Tests.Tokenization.Scanner
{
    using System.Text;
    using PdfPig.Core;
    using PdfPig.Encryption;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Tokens;

    public class PdfTokenScannerTests
    {
        [Fact]
        public void ReadsSimpleObject()
        {
            const string s = @"294 0 obj
/WDKAAR+CMBX12 
endobj";

            var pdfScanner = GetScanner(s); 

            pdfScanner.MoveNext();

            var objectToken = Assert.IsType<ObjectToken>(pdfScanner.CurrentToken);

            var name = Assert.IsType<NameToken>(objectToken.Data);

            Assert.Equal(294, objectToken.Number.ObjectNumber);
            Assert.Equal(0, objectToken.Number.Generation);

            Assert.Equal("WDKAAR+CMBX12", name.Data);

            Assert.StartsWith("294 0 obj", s.Substring((int)objectToken.Position));
        }

        [Fact]
        public void ReadsIndirectReferenceInObject()
        {
            const string s = @"
15 0 obj
12 7 R
endobj";

            var scanner = GetScanner(s);

            var token = ReadToEnd(scanner)[0];

            var reference = Assert.IsType<IndirectReferenceToken>(token.Data);

            Assert.Equal(new IndirectReference(12, 7), reference.Data);
        }

        [Fact]
        public void ReadsObjectWithUndefinedIndirectReference()
        {
            const string s = @"
5 0 obj
<<
/XObject <<
/Pic1 7 0 R
>>
/ProcSet [/PDF /Text /ImageC ]
/Font <<
/F0 8 0 R
/F1 9 0 R
/F2 10 0 R
/F3 0 0 R
>>
>>
endobj";

            var scanner = GetScanner(s);

            ReadToEnd(scanner);

            var token = scanner.Get(new IndirectReference(5, 0));
            Assert.NotNull(token);

            token = scanner.Get(new IndirectReference(0, 0));
            Assert.Null(token);
        }

        [Fact]
        public void ReadsNumericObjectWithComment()
        {
            const string s = @"%PDF-1.2

% I commented here too, tee hee
10383384 2 obj
%and here, I just love comments

45

endobj

%%EOF";

            var pdfScanner = GetScanner(s);

            pdfScanner.MoveNext();

            var obj = Assert.IsType<ObjectToken>(pdfScanner.CurrentToken);

            var num = Assert.IsType<NumericToken>(obj.Data);

            Assert.Equal(45, num.Int);

            Assert.Equal(10383384, obj.Number.ObjectNumber);
            Assert.Equal(2, obj.Number.Generation);

            Assert.StartsWith("10383384 2 obj", s.Substring((int)obj.Position));

            Assert.False(pdfScanner.MoveNext());
        }

        [Fact]
        public void ReadsArrayObject()
        {
            const string s = @"
endobj

295 0 obj
[ 
676 938 875 787 750 880 813 875 813 875 813 656 625 625 938 938 313 
344 563 563 563 563 563 850 500 574 813 875 563 1019 1144 875 313
]
endobj";

            var pdfScanner = GetScanner(s);

            pdfScanner.MoveNext();

            var obj = Assert.IsType<ObjectToken>(pdfScanner.CurrentToken);

            var array = Assert.IsType<ArrayToken>(obj.Data);

            Assert.Equal(676, ((NumericToken)array.Data[0]).Int);

            Assert.Equal(33, array.Data.Count);

            Assert.Equal(295, obj.Number.ObjectNumber);
            Assert.Equal(0, obj.Number.Generation);

            Assert.StartsWith("295 0 obj", s.Substring((int)obj.Position));

            Assert.False(pdfScanner.MoveNext());
        }

        [Fact]
        public void ReadsDictionaryObjectThenNameThenDictionary()
        {
            const string s = @"

274 0 obj
<< 
/Type /Pages 
/Count 2 
/Parent 275 0 R 
/Kids [ 121 0 R 125 0 R ] 
>> 
endobj

%Other parts...

310 0 obj
/WPXNWT+CMR9 
endobj 311 0 obj
<< 
/Type /Font 
/Subtype /Type1 
/FirstChar 0 
/LastChar 127 
/Widths 313 0 R 
/BaseFont 310 0 R /FontDescriptor 312 0 R 
>> 
endobj";

            var scanner = GetScanner(s);

            var tokens = ReadToEnd(scanner);
            
            var dictionary = Assert.IsType<DictionaryToken>(tokens[0].Data);

            Assert.Equal(4, dictionary.Data.Count);
            Assert.Equal(274, tokens[0].Number.ObjectNumber);
            Assert.StartsWith("274 0 obj", s.Substring((int)tokens[0].Position));

            var nameObject = Assert.IsType<NameToken>(tokens[1].Data);

            Assert.Equal("WPXNWT+CMR9", nameObject.Data);
            Assert.Equal(310, tokens[1].Number.ObjectNumber);
            Assert.StartsWith("310 0 obj", s.Substring((int)tokens[1].Position));

            dictionary = Assert.IsType<DictionaryToken>(tokens[2].Data);

            Assert.Equal(7, dictionary.Data.Count);
            Assert.Equal(311, tokens[2].Number.ObjectNumber);
            Assert.StartsWith("311 0 obj", s.Substring((int)tokens[2].Position));
        }

        [Fact]
        public void ReadsStringObject()
        {
            const string s = @"

58949797283757 0 obj    (An object begins with obj and ends with endobj...) endobj
";

            var scanner = GetScanner(s);

            var token = ReadToEnd(scanner)[0];

            Assert.Equal(58949797283757L, token.Number.ObjectNumber);
            Assert.Equal("An object begins with obj and ends with endobj...", Assert.IsType<StringToken>(token.Data).Data);

            Assert.StartsWith("58949797283757 0 obj", s.Substring((int)token.Position));
        }

        [Fact]
        public void ReadsStreamObject()
        {
            const string s = @"
352 0 obj
<< /S 1273 /Filter /FlateDecode /Length 353 0 R >> 
stream
H‰œUkLSgþÚh¹IÝÅlK(%[ÈÅ©+ƒåê©ŠèæÇtnZ)Z¹¨Oå~9ŠÊµo”[éiK)÷B¹´
É² ©¸˜ n±º×dKöcÏ÷ãœç{ßï}¾÷ÍÉs   Ô;€
À»—ÀF`ÇF@ƒ4˜ï	@¥T¨³fY: žwÌµ;’’Îq®]cƒÿdp¨ÛI3F#G©#œ)TÇqW£NÚÑ¬gOKbü‡µ#á¡£Þaîtƒƒ›ß–
¾“S>}µuÕõ5M±¢ª†»øÞû•q÷îÜ~¬PòžÞ~•¬ëÉƒGÅ-Ñ­ím·°gêêb,/,£P§õ^v¾ãÁô¿¿ŠTE]²±{šuwÔ`LG³DªìTÈ
A¡¬àð‰É©ˆ°‘¼›‚%¥×s³®í»š}%§X{{tøNåÝž¶ö¢ÖÞ¾–~´¼¬°À“Éððr¥8»P£ØêÁi½®Û(éhŽ‘ú;x#dÃÄ$m
+)
)†…±n
9ùyŽA·n\ï»t!=3£½¡:®­µåâ¹Ô³ø¼ËiûSÎsë;•Dt—ö$WÉ4U‘¢ºÚšñá1íÐèÔó‚svõ(/(+D²#mZÏ6êüÝ7x‡—†”‡E„²‚|ê«êªDµ5q°šR¦RÈ£n¾[è~“}ýƒÝ½Sêž¦'æQŽzÝ‚mæ
óF+Õ%ù‡ƒß9SË†ŒÓãšH¶~L-#T]êîÁ©ÎkbjÒp½¸$¤´(4<,""øfvÎ•< VÐ«#4'2l'Ð1ñðn?sìûãI'OŸøñçŸN5(äÊ'âÎÑ¾ÞþíðƒQmu}]Õ£‡c›©.Œòµ9zz0Ñ²‚B¢«#š-3ªà<cš¥’¡È¨qµ¦{pìÛ„Ã‡ŽŠ/íO»|áIclSCuo_Oœ\\ï!ª©«­ªƒTþ5Ó‹™Ü”óî_9|ýÍ7ø!Ñý|2Goÿ€Î¶Öö…<ðáƒGéGá½G´Ã.®TŠóî=_|þ™‡ƒééFwßà 0æîc_Óë¦³|ý|¶®æ„…†G8Òüï€l…\¦RFº:‰	VPð•S“Û¶ï V—ø/¿¾Xæ+«««ÖŽ4>ŸŸ¦Pà8®Ó…¼æ¢BaÅÐkëÊŠukÈÊÖL£­ivvv…k2=µZMØ|Úl(ŠZ­V›ÍbI>Ÿl¹œ(â±Äb­ø”Uªñeü©U*‹’“Oð,„E+¶Êà>ŽU”ÎÌõçlºFÃ_ÃÙl?¶=>>!>þC¿-×à©©©x¾€¢ŠÊåòtÃ0‹Æôz“‰ NÊ,¬‚kÀ°F‚XÛ4&“ÉfÃñÅæûæy=ÆãIðE_¾Èårår/XÞ„/·qò›m¶ìÖ|†óx8Wð¹hºÜÂÕalÎü’˜Ã0^Òòòü¼yÞ¶´´DX
                )¨ÇM8lüM…Oúý| 1Ïãk»:t<…ÂÚl¶e¾†” éKÜl6c¹¸É„› ”)‰'3¤œ\–™ËN–™ÿe^Ð² y÷ð¹f`3ëž´	¸“$d:e†)!%2ºdvË@½N¼ªŠ Ùná¹ ¼¿@€Ã.èšs ì÷ûM€2(E4_ | FÑ.@v@÷¤ÃÅ0È Pž~,€:»H¤k¾hT	Œ	€ êÇV:Ô…©@@oH¯(3T‰{""C½SñŠœþtz3€•ƒ ñf.¬SÐøzWþ*$9gj=~Ì·QD E6o¥Ûi/Â`1ígGMq,;}Ž¼sÔ×®kDü˜J{e5‚²ìÉ~Y)}fA>:˜ù–""Yò	ç¹=ù²yÛ¡¿i	aœ‘ØÏºþÇoäO ôkÆ)
                endstream
                    endobj
                353 0 obj
                1479
                endobj";

            var locationProvider = new TestObjectLocationProvider();
            // Mark location of "353 0 obj"
            locationProvider.Offsets[new IndirectReference(353, 0)] = 1643;

            var scanner = GetScanner(s, locationProvider);

            var tokens = ReadToEnd(scanner);

            Assert.Equal(2, tokens.Count);

            var stream = Assert.IsType<StreamToken>(tokens[0].Data);

            var str = Encoding.UTF8.GetString(stream.Data.ToArray());

            Assert.StartsWith("H‰œUkLSgþÚh¹IÝÅl", str);
            Assert.EndsWith("oäO ôkÆ)", str);

            Assert.Equal(2, locationProvider.Offsets[new IndirectReference(352, 0)]);
        }

        [Fact]
        public void ReadsStreamObjectWithInvalidLength()
        {
            string invalidLengthStream = "ABCD" + new string('e', 3996);

            string s = $@"
352 0 obj
<< /S 1273 /Filter /FlateDecode /Length 353 0 R >> 
stream
{invalidLengthStream}
endstream
endobj
353 0 obj
1479
endobj";

            var locationProvider = new TestObjectLocationProvider();
            // Mark location of "353 0 obj"
            locationProvider.Offsets[new IndirectReference(353, 0)] = 1643;

            var scanner = GetScanner(s, locationProvider);

            var tokens = ReadToEnd(scanner);

            Assert.Equal(2, tokens.Count);

            var stream = Assert.IsType<StreamToken>(tokens[0].Data);

            var data = stream.Data.ToArray();

            var str = Encoding.UTF8.GetString(data);

            Assert.Equal(data.Length, invalidLengthStream.Length);
            Assert.Equal(invalidLengthStream, str);

            Assert.Equal(2, locationProvider.Offsets[new IndirectReference(352, 0)]);
        }

        [Fact]
        public void ReadsSimpleStreamObject()
        {
            const string s =
                """
                574387 0    obj
                << /Length 45 >>
                stream
                À“Éððr¥8»P£ØêÁi½®Û(éhŽ‘ú
                endstream
                endobj
                """;
            
            var scanner = GetScanner(s);

            var tokens = ReadToEnd(scanner);

            var str = GetStreamDataString(tokens);

            Assert.Equal("À“Éððr¥8»P£ØêÁi½®Û(éhŽ‘ú", str);
        }

        [Fact]
        public void ReadsSimpleStreamContent()
        {
            const string s =
                """
                1 0 obj
                << /Name /Bob >>
                stream
                123456
                endstream
                endobj
                """;

            var scanner = GetScanner(s);

            var tokens = ReadToEnd(scanner);

            var token = Assert.Single(tokens);

            var stream = Assert.IsType<StreamToken>(token.Data);

            var bytes = stream.Data.ToArray();
            Assert.Equal(6, bytes.Length);

            var outputString = Encoding.ASCII.GetString(bytes);

            Assert.Equal("123456", outputString);
        }

        [Fact]
        public void ReadsStreamContentWithNoLinebreak()
        {
            const string s =
                """
                1 0 obj
                << /Name /Bob >>
                stream
                123456endstream
                endobj
                """;

            var scanner = GetScanner(s);

            var tokens = ReadToEnd(scanner);

            var token = Assert.Single(tokens);

            var stream = Assert.IsType<StreamToken>(token.Data);

            var bytes = stream.Data.ToArray();
            Assert.Equal(6, bytes.Length);

            var outputString = Encoding.ASCII.GetString(bytes);

            Assert.Equal("123456", outputString);
        }

        [Fact]
        public void ReadsStreamWithIndirectLength()
        {
            const string s =
                """
                5 0 obj
                52
                 endobj

                12 0 obj

                << /Length 5 0 R /S 1245 >>

                stream
                %¥×³®í»š}%§X{{tøNåÝž¶ö¢ÖÞ¾–~´¼
                endstream
                endobj
                """;

            var locationProvider = new TestObjectLocationProvider
            {
                Offsets =
                {
                    [new IndirectReference(5, 0)] = 0
                }
            };

            var scanner = GetScanner(s, locationProvider);

            var token = ReadToEnd(scanner)[1];

            var stream = Assert.IsType<StreamToken>(token.Data);

            var bytes = stream.Data.ToArray();
            Assert.Equal(52, bytes.Length);

            var outputString = Encoding.UTF8.GetString(bytes);

            Assert.Equal("%¥×³®í»š}%§X{{tøNåÝž¶ö¢ÖÞ¾–~´¼", outputString);
        }

        [Fact]
        public void ReadsStreamWithMissingLength()
        {
            const string s =
                """
                12655 0 obj
                << /S 1245 >>
                stream
                %¥×³®í»š}%§X{{tøNåendÝž¶ö¢ÖÞgrehtyyy$&%&£$££(*¾–~´¼
                endstream
                endobj
                """;

            var scanner = GetScanner(s);

            var tokens = ReadToEnd(scanner);

            var str = GetStreamDataString(tokens);

            Assert.Equal("%¥×³®í»š}%§X{{tøNåendÝž¶ö¢ÖÞgrehtyyy$&%&£$££(*¾–~´¼", str);
        }

        [Fact]
        public void ReadsStreamWithoutBreakBeforeEndstream()
        {
            const string s =
                """
                7 0 obj
                << /Filter 0 >>
                stream
                ABCendcow233endendstream
                endobj
                """;

            var scanner = GetScanner(s);

            var tokens = ReadToEnd(scanner);

            var str = GetStreamDataString(tokens);

            Assert.Equal("ABCendcow233end", str);
        }

        [Fact]
        public void ReadsStreamWithDoubleEndstreamSimple()
        {
            const string s =
                """
                250 0 obj
                << /Filter /FlateDecode >>
                stream
                012endstream
                endstream
                endobj
                """;

            var scanner = GetScanner(s);

            var tokens = ReadToEnd(scanner);

            var dataStr = GetStreamDataString(tokens);

            Assert.Equal("012", dataStr);
        }

        [Fact]
        public void ReadsStreamWithDoubleEndstream()
        {
            const string s =
                """
                1974 0 obj
                <<
                /Filter /FlateDecode
                >>
                stream
                ABC123endstream33093872end337772A
                
                3093AAendstream
                endstream
                endobj
                """;

            var scanner = GetScanner(s);

            var tokens = ReadToEnd(scanner);

            var str = GetStreamDataString(tokens);

            Assert.Equal(
                """
                ABC123endstream33093872end337772A
                
                3093AA
                """,
                str);
        }

        private string GetStreamDataString(IReadOnlyList<ObjectToken> tokens)
        {
            var token = Assert.Single(tokens);

            var stream = Assert.IsType<StreamToken>(token.Data);

            return Encoding.UTF8.GetString(stream.Data.ToArray());
        }

        [Fact]
        public void ReadsStringsWithMissingEndBracket()
        {
            const string input = @"5 0 obj
<<
/Kids [4 0 R 12 0 R 17 0 R 20 0 R 25 0 R 28 0 R ]
/Count 6
/Type /Pages
/MediaBox [ 0 0 612 792 ]
>>
endobj
1 0 obj
<<
/Creator (Corel WordPerfect - [D:\Wpdocs\WEBSITE\PROC&POL.WP6 (unmodified)
/CreationDate (D:19980224130723)
/Title (Proc&Pol.pdf)
/Author (J. L. Swezey)
/Producer (Acrobat PDFWriter 3.03 for Windows NT)
/Keywords (Budapest Treaty; Patent deposits; IDA)
/Subject (Patent Collection Procedures and Policies)
>>
endobj
3 0 obj
<<
/Pages 5 0 R
/Type /Catalog
>>
endobj";

            var scanner = GetScanner(input);

            var tokens = ReadToEnd(scanner);

            Assert.Equal(3, tokens.Count);

            var first = tokens[0];
            Assert.Equal(5, first.Number.ObjectNumber);

            var second = tokens[1];
            Assert.Equal(1, second.Number.ObjectNumber);

            var third = tokens[2];
            Assert.Equal(3, third.Number.ObjectNumber);
        }

        [Fact]
        public void ReadsDictionaryContainingNull()
        {
            const string input = @"14224 0 obj
<</Type /XRef
/Root 8 0 R
/Prev 116
/Length 84
/Size 35
/W [1 3 2]
/Index [0 1 6 1 8 2 25 10]
/ID [ (ù¸7�ãA×�žòÜ4��Š•)]
/Info 6 0 R
/Encrypt null>>
endobj";

            var scanner = GetScanner(input);

            var tokens = ReadToEnd(scanner);

            var dictionaryToken = tokens[0].Data as DictionaryToken;

            Assert.NotNull(dictionaryToken);

            var encryptValue = dictionaryToken.Data["Encrypt"];

            Assert.IsType<NullToken>(encryptValue);
        }

        [Fact]
        public void ReadMultipleNestedDictionary()
        {
            const string input =
                @"
                4 0 obj
                << /Type /Font /Subtype /Type1 /Name /AF1F040+Arial /BaseFont /Arial /FirstChar 32 /LastChar 255
                /Encoding
                <<
                /Type /Encoding /BaseEncoding /WinAnsiEncoding
                /Differences [128 /Euro 130 /quotesinglbase /florin /quotedblbase /ellipsis /dagger /daggerdbl /circumflex /perthousand /Scaron /guilsinglleft /OE 142 /Zcaron 145
                /quoteleft /quoteright /quotedblleft /quotedblright /bullet /endash /emdash /tilde /trademark /scaron /guilsinglright /oe 158 /zcaron /Ydieresis /space /exclamdown
                /cent /sterling /currency /yen /brokenbar /section /dieresis /copyright /ordfeminine /guillemotleft /logicalnot /hyphen /registered /macron /degree /plusminus
                /twosuperior /threesuperior /acute /mu /paragraph /periodcentered /cedilla /onesuperior /ordmasculine /guillemotright /onequarter /onehalf /threequarters
                /questiondown /Agrave /Aacute /Acircumflex /Atilde /Adieresis /Aring /AE /Ccedilla /Egrave /Eacute /Ecircumflex /Edieresis /Igrave /Iacute /Icircumflex /Idieresis
                /Eth /Ntilde /Ograve /Oacute /Ocircumflex /Otilde /Odieresis /multiply /Oslash /Ugrave /Uacute /Ucircumflex /Udieresis /Yacute /Thorn /germandbls /agrave /aacute
                /acircumflex /atilde /adieresis /aring /ae /ccedilla /egrave /eacute /ecircumflex /edieresis /igrave /iacute /icircumflex /idieresis /eth /ntilde /ograve /oacute
                /ocircumflex /otilde /odieresis /divide /oslash /ugrave /uacute /ucircumflex /udieresis /yacute /thorn /ydieresis ]
                >>
                /Widths [278 278 355 556 556 889 667 191 333 333 389 584 278 333 278 278 
                556 556 556 556 556 556 556 556 556 556 278 278 584 584 584 556 
                1015 667 667 722 722 667 611 778 722 278 500 667 556 833 722 778 
                667 778 722 667 611 722 667 944 667 667 611 278 278 278 469 556 
                333 556 556 500 556 556 278 556 556 222 222 500 222 833 556 556 
                556 556 333 500 278 556 500 722 500 500 500 334 260 334 584 750 
                556 750 222 556 333 1000 556 556 333 1000 667 333 1000 750 611 750 
                750 222 222 333 333 350 556 1000 333 1000 500 333 944 750 500 667 
                278 333 556 556 556 556 260 556 333 737 370 556 584 333 737 552 
                400 549 333 333 333 576 537 278 333 333 365 556 834 834 834 611 
                667 667 667 667 667 667 1000 722 667 667 667 667 278 278 278 278 
                722 722 778 778 778 778 778 584 778 722 722 722 722 667 667 611 
                556 556 556 556 556 556 889 500 556 556 556 556 278 278 278 278 
                556 556 556 556 556 556 556 549 611 556 556 556 556 500 556 500 
                ]
                >>
                 >>
                endobj
                ";

            var scanner = GetScanner(input);

            var tokens = ReadToEnd(scanner);

            var dictionaryToken = tokens[0].Data as DictionaryToken;

            Assert.NotNull(dictionaryToken);
        }

        [Fact]
        public void ReadsDictionaryWithoutEndObjBeforeNextObject()
        {
            const string input = @"1 0 obj
<</Type /XRef>>
2 0 obj
<</Length 15>>
endobj";

            var scanner = GetScanner(input);

            var tokens = ReadToEnd(scanner);

            Assert.Equal(2, tokens.Count);

            var dictionaryToken = Assert.IsType<DictionaryToken>(tokens[0].Data);
            var typeValue = dictionaryToken.Data["Type"];
            Assert.IsType<NameToken>(typeValue);

            dictionaryToken = tokens[1].Data as DictionaryToken;
            Assert.NotNull(dictionaryToken);
            typeValue = dictionaryToken.Data["Length"];
            Assert.IsType<NumericToken>(typeValue);
        }

        [Fact]
        public void ReadsStreamWithoutEndObjBeforeNextObject()
        {
            const string input = @"1 0 obj
<</Length 4>>
stream
aaaa
endstream
2 0 obj
<</Length 15>>
endobj";

            var scanner = GetScanner(input);

            var tokens = ReadToEnd(scanner);

            Assert.Equal(2, tokens.Count);

            Assert.IsType<StreamToken>(tokens[0].Data);

            var dictionaryToken = Assert.IsType<DictionaryToken>(tokens[1].Data);
            var typeValue = dictionaryToken.Data["Length"];
            Assert.IsType<NumericToken>(typeValue);
        }

        [Theory]
        [InlineData("startxref")]
        [InlineData("xref")]
        public void ReadsStreamWithoutEndObjBeforeToken(string token)
        {
            string input = @$"1 0 obj
<</Length 4>>
stream
aaaa
endstream
{token}";

            var scanner = GetScanner(input);

            var tokens = ReadToEnd(scanner);

            Assert.Single(tokens);

            Assert.IsType<StreamToken>(tokens[0].Data);
        }

        [Theory]
        [InlineData("startxref")]
        [InlineData("xref")]
        public void ReadsDictionaryWithoutEndObjBeforeToken(string token)
        {
            string input = @$"1 0 obj
<</Type /XRef>>
{token}";

            var scanner = GetScanner(input);

            var tokens = ReadToEnd(scanner);

            Assert.Single(tokens);

            var dictionaryToken = Assert.IsType<DictionaryToken>(tokens[0].Data);
            var typeValue = dictionaryToken.Data["Type"];
            Assert.IsType<NameToken>(typeValue);
        }

        [Fact]
        public void ReadsStreamWithoutEndStreamBeforeEndObj()
        {
            const string input = @"1 0 obj
<</Length 4>>
stream
aaaa
endobj
2 0 obj
<</Length 15>>
endobj";

            var scanner = GetScanner(input);

            var tokens = ReadToEnd(scanner);

            Assert.Equal(2, tokens.Count);

            Assert.IsType<StreamToken>(tokens[0].Data);

            var dictionaryToken = Assert.IsType<DictionaryToken>(tokens[1].Data);
            var lengthValue = dictionaryToken.Data["Length"];
            Assert.IsType<NumericToken>(lengthValue);
        }

        [Theory]
        [InlineData(">>")]
        [InlineData("randomstring")]
        public void ReadsIndirectObjectsDictionaryWithContentBeforeEndObj(string addedContent)
        {
            string input = @$"1 0 obj
<</Type /XRef>>
{addedContent}endobj
2 0 obj
<</Length 15>>
endobj";

            var strictScanner = GetScanner(input);
            
            var tokens = ReadToEnd(strictScanner);
            Assert.Empty(tokens);

            var lenientScanner = GetScanner(input, useLenientParsing: true);
            tokens = ReadToEnd(lenientScanner);

            Assert.Equal(2, tokens.Count);

            var dictionaryToken = Assert.IsType<DictionaryToken>(tokens[0].Data);
            var typeValue = dictionaryToken.Data["Type"];
            Assert.IsType<NameToken>(typeValue);

            dictionaryToken = Assert.IsType<DictionaryToken>(tokens[1].Data);
            var lengthValue = dictionaryToken.Data["Length"];
            Assert.IsType<NumericToken>(lengthValue);
        }

        [Theory]
        [InlineData(">>")]
        [InlineData("randomstring")]
        public void ReadsIndirectObjectsStreamWithAddedContentBeforeStream(string addedContent)
        {
            string input = @$"1 0 obj
<</length 4>>
{addedContent}stream
aaaa
endstream
endobj
2 0 obj
<</Length 15>>
endobj";

            var strictScanner = GetScanner(input);
            
            var tokens = ReadToEnd(strictScanner);
            Assert.Equal(2, tokens.Count);
            // this is linked to the parsing choosing the last token parsed in obj.
            // It can probably be challenged against taking the first one.
            var operatorToken = Assert.IsType<OperatorToken>(tokens[0].Data);
            Assert.Equal("endstream", operatorToken.Data);

            var dictionaryToken = Assert.IsType<DictionaryToken>(tokens[1].Data);
            var lengthValue = dictionaryToken.Data["Length"];
            Assert.IsType<NumericToken>(lengthValue);

            var lenientScanner = GetScanner(input, useLenientParsing:true);
            tokens = ReadToEnd(lenientScanner);

            Assert.Equal(2, tokens.Count);

            Assert.IsType<StreamToken>(tokens[0].Data);

            dictionaryToken = Assert.IsType<DictionaryToken>(tokens[1].Data);
            lengthValue = dictionaryToken.Data["Length"];
            Assert.IsType<NumericToken>(lengthValue);
        }

        private static PdfTokenScanner GetScanner(string s, TestObjectLocationProvider locationProvider = null, bool useLenientParsing = false)
        {
            var input = StringBytesTestConverter.Convert(s, false);

            return new PdfTokenScanner(input.Bytes, locationProvider ?? new TestObjectLocationProvider(),
                new TestFilterProvider(), NoOpEncryptionHandler.Instance, useLenientParsing ? new ParsingOptions() : ParsingOptions.LenientParsingOff);
        }

        private static IReadOnlyList<ObjectToken> ReadToEnd(PdfTokenScanner scanner)
        {
            var result = new List<ObjectToken>();

            while (scanner.MoveNext())
            {
                if (scanner.CurrentToken is ObjectToken obj)
                {
                    result.Add(obj);
                }
                else
                {
                    throw new InvalidOperationException($"Pdf token scanner produced token which was not an object token: {scanner.CurrentToken}.");
                }
            }

            return result;
        }
    }
}
