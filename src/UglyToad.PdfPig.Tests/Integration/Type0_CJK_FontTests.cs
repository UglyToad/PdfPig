namespace UglyToad.PdfPig.Tests.Integration
{
    using System.IO;
    using System.Linq;
    using Content;
    using Xunit;

    public class Type0_CJK_FontTests
    {
        private static string GetFilename()
        {
            return IntegrationHelpers.GetDocumentPath("Type0_CJK_Font");
        }

        [Fact]
        public void HasCorrectNumberOfPages()
        {
            var file = GetFilename();

            using (var document = PdfDocument.Open(File.ReadAllBytes(file)))
            {
                Assert.Equal(95, document.NumberOfPages);
            }
        }
 

        [Fact]
        public void HasCorrectChineseCharacters()
        {
            using (var document = PdfDocument.Open(GetFilename()))
            {
                var page = document.GetPage(1);

                var text = string.Join(string.Empty, page.Letters.Select(x => x.Value));

                Assert.True(text?.Contains("中航动力控制股份有限公司"));
                Assert.True(text?.Contains("年半年度报告"));
                Assert.True(text?.Contains("中航动力控制股份有限公司董事会"));
                Assert.True(text?.Contains("2010年8月17日"));

                //charcode, cid, unicode, char,0xc1a6,0x09ef,\u529b,力
                //charcode, cid, unicode, char,0xbfd8,0x0965,\u63a7,控
                //charcode, cid, unicode, char,0xd6c6,0x11c5,\u5236,制
                //charcode, cid, unicode, char,0xb9c9,0x0722,\u80a1,股
                //charcode, cid, unicode, char,0xb7dd,0x067a,\u4efd,份
                //charcode, cid, unicode, char,0xd3d0,0x10b5,\u6709,有
                //charcode, cid, unicode, char,0xcfde,0x0f4b,\u9650,限
                //charcode, cid, unicode, char,0xb9ab,0x0704,\u516c,公
                //charcode, cid, unicode, char,0xcbbe,0x0db3,\u53f8,司
                //charcode, cid, unicode, char,0xb6ad,0x05ec,\u8463,董
                //charcode, cid, unicode, char,0xcac2,0x0d59,\u4e8b,事
                //charcode, cid, unicode, char,0xbbe1,0x07f6,\u4f1a,会
                //charcode, cid, unicode, char,0xc4ea,0x0b4d,\u5e74,年
                //charcode, cid, unicode, char,0xd4c2,0x1105,\u6708,月
                //charcode, cid, unicode, char,0xc8d5,0x0cb0,\u65e5,日


                /*
                 Font Dictionary of Page 1
                 Name	References
                TT2  	321
                TT4  	322
                TT6  	327
                TT7  	329
                TT9  	330
                TT10 	322

                Font Details
                TT2 {[BaseFont, {/TimesNewRoman}]} {[Encoding, {/WinAnsiEncoding}]}
                TT4 {[BaseFont, {/TimesNewRoman,Bold}]} {[Encoding, {/WinAnsiEncoding}]}
                TT6 {[BaseFont, {/KaiTi_GB2312}]} {[Encoding, {/GBK-EUC-H}]}
	                DescendndFont 	{[BaseFont, {/KaiTi_GB2312}]}
			                {[Subtype, {/CIDFontType2}]}
			                {[CIDSystemInfo, {<Registry, (Adobe)>, <Ordering, (GB1)>, <Supplement, 2>}]}
	
                TT7 {[BaseFont, {/KaiTi_GB2312+2}]} 
	                {[Subtype, {/TrueType}]}
	                {[Encoding, {/WinAnsiEncoding}]}
	                {[BaseFont, {/KaiTi_GB2312+2}]}


                TT9 {[BaseFont, {/SimHei}]} 
	                {[Subtype, {/TrueType}]}
	                {[Encoding, {/WinAnsiEncoding}]}

                TT10 {[BaseFont, {/SimHei+2}]}
	                {[Encoding, {/GBK-EUC-H}]}
	                {[Subtype, {/Type0}]}
	                DescendndFont	
		                {[Subtype, {/CIDFontType2}]}
		                {[CIDSystemInfo, {<Registry, (Adobe)>, <Ordering, (GB1)>, <Supplement, 2>}]}	
		                {[BaseFont, {/SimHei+2}]}
                 */
            }
        }

         
    }
}