namespace UglyToad.PdfPig.Tests.Functions.Type4
{
    public class ParserTests
    {
        /// <summary>
        /// Test the very basics.
        /// </summary>
        [Fact]
        public void ParserBasics()
        {
            Type4Tester.Create("3 4 add 2 sub").Pop(5).IsEmpty();
        }

        /// <summary>
        /// Test nested blocks.
        /// </summary>
        [Fact]
        public void Nested()
        {
            Type4Tester.Create("true { 2 1 add } { 2 1 sub } ifelse")
                .Pop(3).IsEmpty();

            Type4Tester.Create("{ true }").Pop(true).IsEmpty();
        }

        /// <summary>
        /// Tests problematic functions from PDFBOX-804.
        /// </summary>
        [Fact]
        public void Jira804()
        {
            //This is an example of a tint to CMYK function
            //Problems here were:
            //1. no whitespace between "mul" and "}" (token was detected as "mul}")
            //2. line breaks cause endless loops
            Type4Tester.Create("1 {dup dup .72 mul exch 0 exch .38 mul}\n")
                .Pop(0.38f).Pop(0f).Pop(0.72f).Pop(1.0f).IsEmpty();
        }
    }
}
