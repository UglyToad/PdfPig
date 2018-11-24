namespace UglyToad.PdfPig.Tests.Tokenization.Scanner
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using PdfPig.ContentStream;
    using PdfPig.IO;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Tokens;
    using PdfPig.Util;
    using Xunit;

    public class PdfTokenScannerTests
    {
        [Fact]
        public void ReadsSimpleObject()
        {
            const string s = @"294 0 obj/WDKAAR+CMBX12 endobj";

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
            const string s = @"endobj295 0 obj[ 676 938 875 787 750 880 813 875 813 875 813 656 625 625 938 938 313 344 563 563 563 563 563 850 500 574 813 875 563 1019 1144 875 313]endobj";

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

274 0 obj<< /Type /Pages /Count 2 /Parent 275 0 R /Kids [ 121 0 R 125 0 R ] >> endobj
%Other parts...310 0 obj/WPXNWT+CMR9 endobj 311 0 obj<< /Type /Font /Subtype /Type1 /FirstChar 0 /LastChar 127 /Widths 313 0 R /BaseFont 310 0 R /FontDescriptor 312 0 R >> endobj";

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
352 0 obj<< /S 1273 /Filter /FlateDecode /Length 353 0 R >> stream
H‰œUkLSgþÚh¹IÝÅlK(%[ÈÅ©+ƒåê©ŠèæÇtnZ)Z¹¨Oå~9ŠÊµo”[éiK)÷B¹´
É² ©¸˜ n±º×dKöcÏ÷ãœç{ßï}¾÷ÍÉs   Ô;€
À»—ÀF`ÇF@ƒ4˜ï	@¥T¨³fY: žwÌµ;’’Îq®]cƒÿdp¨ÛI3F#G©#œ)TÇqW£NÚÑ¬gOKbü‡µ#á¡£Þaîtƒƒ›ß–¾“S>}µuÕõ5M±¢ª†»øÞû•q÷îÜ~¬PòžÞ~•¬ëÉƒGÅ-Ñ­ím·°gêêb,/,£P§õ^v¾ãÁô¿¿ŠTE]²±{šuwÔ`LG³DªìTÈA¡¬àð‰É©ˆ°‘¼›‚%¥×s³®í»š}%§X{{tøNåÝž¶ö¢ÖÞ¾–~´¼¬°À“Éððr¥8»P£ØêÁi½®Û(éhŽ‘ú;x#dÃÄ$m
+))†…±n
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

            Assert.Equal(2, locationProvider.Offsets[new IndirectReference(352, 0)]);
        }

        [Fact]
        public void ReadsSimpleStreamObject()
        {
            // Length of the bytes as found by Encoding.UTF8.GetBytes is 45
            const string s = @"
574387 0    obj
<< /Length 45 >>
stream
À“Éððr¥8»P£ØêÁi½®Û(éhŽ‘ú
endstream
endobj";
            
            var scanner = GetScanner(s);

            var token = ReadToEnd(scanner)[0];

            var stream = Assert.IsType<StreamToken>(token.Data);

            var bytes = stream.Data.ToArray();
            Assert.Equal(45, bytes.Length);

            var outputString = Encoding.UTF8.GetString(bytes);

            Assert.Equal("À“Éððr¥8»P£ØêÁi½®Û(éhŽ‘ú", outputString);
        }

        [Fact]
        public void ReadsStreamWithIndirectLength()
        {
            const string s = @"5 0 obj 52 endobj



12 0 obj

<< /Length 5 0 R /S 1245 >>

stream
%¥×³®í»š}%§X{{tøNåÝž¶ö¢ÖÞ¾–~´¼
endstream
endobj";
            var locationProvider = new TestObjectLocationProvider();

            locationProvider.Offsets[new IndirectReference(5, 0)] = 0;

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
            const string s = @"
12655 0 obj

<< /S 1245 >>

stream
%¥×³®í»š}%§X{{tøNåÝž¶ö¢ÖÞgrehtyyy$&%&£$££(*¾–~´¼
endstream
endobj";

            var scanner = GetScanner(s);

            var token = ReadToEnd(scanner)[0];

            Assert.Equal(12655, token.Number.ObjectNumber);

            var stream = Assert.IsType<StreamToken>(token.Data);

            Assert.Equal("1245", stream.StreamDictionary.Data["S"].ToString());

            Assert.Equal("%¥×³®í»š}%§X{{tøNåÝž¶ö¢ÖÞgrehtyyy$&%&£$££(*¾–~´¼", Encoding.UTF8.GetString(stream.Data.ToArray()));
        }

        [Fact]
        public void ReadsStreamWithoutBreakBeforeEndstream()
        {
            const string s = @"
1 0 obj
12
endobj

7 0 obj
<< /Length 288
   /Filter /FlateDecode >>
stream
xœ]‘ËjÃ0E÷ÿÃ,ÓEð#NÒ€1¤N^ôA~€-]A-YYøï+Ï4¡t#qfîFWQY*­Dïv5:è”–§ñjB‹½Òa¤ •p7¤K	ƒÈûëyr8Tº!ÏÃ  úð‚ÉÙVG9¶ø@Å7+Ñ*ÝÃê³¬¹T_ùÆµƒ8Š$vËÌ—Æ¼6BDöu%½B¹yí$—Ù ¤\Hx71JœL#Ð6ºÇ0Èã¸€ü|.Â µüßõÏ""WÛ‰¯Æ.êÄ«ã8;¤iL°!Ø %Ã‰`K°ßì¸ÃöÜáÜ)	[‚#CFðÄ°#(yƒg^ÿ¶æò
ÿž“¸Zë#¢?¢h–P”Æû?šÑï÷ø¯‰Šendstream
endobj

9 0 obj
16
endobj";
            var inputBytes = new ByteArrayInputBytes(OtherEncodings.StringAsLatin1Bytes(s));

            var scanner = new PdfTokenScanner(inputBytes, new TestObjectLocationProvider(), new TestFilterProvider());

            var token = ReadToEnd(scanner)[1];

            Assert.Equal(7, token.Number.ObjectNumber);

        }

        private PdfTokenScanner GetScanner(string s, TestObjectLocationProvider locationProvider = null)
        {
            var input = StringBytesTestConverter.Convert(s, false);

            return new PdfTokenScanner(input.Bytes, locationProvider ?? new TestObjectLocationProvider(),
                new TestFilterProvider());
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
