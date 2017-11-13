namespace UglyToad.Pdf.Tests.Fonts.Parser
{
    using Pdf.Fonts.Parser;
    using Xunit;

    public class CMapParserTests
    {
        private const string GoogleDocToUnicodeCmap = @"/CIDInit /ProcSet findresource begin
12 dict begin
begincmap
/CIDSystemInfo
<<  /Registry (Adobe)
/Ordering (UCS)
/Supplement 0
>> def
/CMapName /Adobe-Identity-UCS def
/CMapType 2 def
1 begincodespacerange
<0000> <FFFF>
endcodespacerange
6 beginbfchar
<0003> <0020>
<0011> <002E>
<0024> <0041>
<0037> <0054>
<0044> <0061>
<005B> <0078>
endbfchar
4 beginbfrange
<0046> <0049> <0063>
<004B> <004C> <0068>
<004F> <0052> <006C>
<0055> <0058> <0072>
endbfrange
endcmap
CMapName currentdict /CMap defineresource pop
end
end";

        private readonly CMapParser cMapParser = new CMapParser(); 

        [Fact]
        public void CanParseCMap()
        {
            var input = StringBytesTestConverter.Convert(GoogleDocToUnicodeCmap, false);

            var cmap = cMapParser.Parse(input.Bytes, false);
        }
    }
}
