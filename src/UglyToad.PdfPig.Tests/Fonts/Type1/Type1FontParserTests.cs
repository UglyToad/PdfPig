namespace UglyToad.PdfPig.Tests.Fonts.Type1
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using PdfPig.Fonts.Type1.Parser;
    using PdfPig.IO;
    using PdfPig.Util;
    using Xunit;

    public class Type1FontParserTests
    {
        private readonly Type1FontParser parser = new Type1FontParser(new Type1EncryptedPortionParser());

        [Fact]
        public void CanReadHexEncryptedPortion()
        {
            var bytes = GetFileBytes("AdobeUtopia.pfa");
            
            parser.Parse(new ByteArrayInputBytes(bytes));
        }

        [Fact]
        public void CanReadBinaryEncryptedPortion()
        {
            // TODO: support reading in these pfb files
            //var bytes = GetFileBytes("cmbx8.pfb");
            
            //parser.Parse(new ByteArrayInputBytes(bytes));
        }

        [Fact]
        public void CanReadAsciiPart()
        {
            var bytes = StringBytesTestConverter.Convert(Cmbx12, false);

            parser.Parse(bytes.Bytes);
        }

        private const string Cmbx12 = @"%!PS-AdobeFont-1.1: CMBX12 1.0
%%CreationDate: 1991 Aug 20 16:34:54
% Copyright (C) 1997 American Mathematical Society. All Rights Reserved.
11 dict begin
/FontInfo 7 dict dup begin
/version (1.0) readonly def
/Notice (Copyright (C) 1997 American Mathematical Society. All Rights Reserved) readonly def
/FullName (CMBX12) readonly def
/FamilyName (Computer Modern) readonly def
/Weight (Bold) readonly def
/ItalicAngle 0 def
/isFixedPitch false def
end readonly def
/FontName /WDKAAR+CMBX12 def
/PaintType 0 def
/FontType 1 def
/FontMatrix [0.001 0 0 0.001 0 0] readonly def
/Encoding 256 array
0 1 255 {1 index exch /.notdef put} for
dup 12 /fi put
dup 46 /period put
dup 49 /one put
dup 50 /two put
dup 51 /three put
dup 52 /four put
dup 53 /five put
dup 65 /A put
dup 66 /B put
dup 67 /C put
dup 69 /E put
dup 73 /I put
dup 77 /M put
dup 78 /N put
dup 80 /P put
dup 82 /R put
dup 83 /S put
dup 84 /T put
dup 97 /a put
dup 98 /b put
dup 99 /c put
dup 100 /d put
dup 101 /e put
dup 102 /f put
dup 103 /g put
dup 104 /h put
dup 105 /i put
dup 107 /k put
dup 108 /l put
dup 109 /m put
dup 110 /n put
dup 111 /o put
dup 112 /p put
dup 114 /r put
dup 115 /s put
dup 116 /t put
dup 117 /u put
dup 118 /v put
dup 120 /x put
dup 121 /y put
readonly def
/FontBBox{-53 -251 1139 750}readonly def
/UniqueID 5000769 def
currentdict end
currentfile eexec
ÙÖoc;„j—¶†©~E£ÐªÙÖoc;„j—¶†©~E£Ð7Ô×1¼Iu`“ÂõÎ>ä‘9Á?î\ºlüýÄ6Ag_Â_–²ÂGÄ´/³0¨;2j~þªÙÖoc;„j—¶†©~E£ÐªÙÖoc;„j—¶†©~E£ÐªÙÖoc;„j—¶†
©~E£ÐªÙÖoc;„j—¶†©~E£ÐªÙÖoc;„j—¶†©~E£ÐªÙÖoc;„j—¶†©~E£Ðª7Ô×1¼Iu`“ÂõÎ>ä‘9Á?î\ºlüýÄ6Ag_Â_–²ÂGÄ´/³0¨;2j~þ
ÙÖoc;„j—¶†©~E£ÐªÙÖoc;„j—¶†©~E£ÐªÙÖoc;„j—¶†©~E£ÐªÙÖoc;„j—¶†©~E£ÐªÙÖoc;„j—¶†©~E£ÐªÙÖoc;„j—¶†©~E£Ðª7Ô×1¼Iu`“ÂõÎ>ä‘9Á?î\ºlüýÄ6Ag_Â_–²ÂGÄ´/³0¨;2j~þ
7Ô×1¼Iu`“ÂõÎ>ä‘9Á?î\ºlüýÄ6Ag_Â_–²ÂGÄ´/³0¨;2j~þv7Ô×1¼Iu`“ÂõÎ>ä‘9Á?î\ºlüýÄ6Ag_Â_–²ÂGÄ´/³0¨;2j~þ000000000000000000000000000000000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000
cleartomark";

        private static byte[] GetFileBytes(string name)
        {
            var manifestFiles = typeof(Type1FontParserTests).Assembly.GetManifestResourceNames();

            var match = manifestFiles.Single(x => x.IndexOf(name, StringComparison.InvariantCultureIgnoreCase) >= 0);

            using (var memoryStream = new MemoryStream())
            using (var stream = typeof(Type1FontParserTests).Assembly.GetManifestResourceStream(match))
            {
                stream.CopyTo(memoryStream);

                return memoryStream.ToArray();
            }
        }
    }
}
