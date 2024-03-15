namespace UglyToad.PdfPig.Tests.Graphics.Operations.General
{
    using PdfPig.Graphics.Operations.General;

    public class SetMiterLimitTests
    {
        private readonly TestOperationContext context = new TestOperationContext();

        [Fact]
        public void RunSetsMiterLimitOfCurrentState()
        {
            var limit = new SetMiterLimit(25);
            
            limit.Run(context);

            Assert.Equal(25, context.GetCurrentState().MiterLimit);
        }

        [Fact]
        public void MiterLimitSymbolCorrect()
        {
            Assert.Equal("M", SetMiterLimit.Symbol);
        }
    }
}
