namespace UglyToad.PdfPig.Tests.Graphics.Operations.TextState
{
    using System;
    using PdfPig.Graphics.Operations.TextState;
    using PdfPig.Tokens;
    using Xunit;

    public class SetFontAndSizeTests
    {
        private static readonly NameToken Font1Name = NameToken.Create("Font1");

        [Fact]
        public void HasCorrectSymbol()
        {
            var symbol = SetFontAndSize.Symbol;

            Assert.Equal("Tf", symbol);
        }

        [Fact]
        public void SetsValues()
        {
            var setFontAndSize = new SetFontAndSize(Font1Name, 12.75);

            Assert.Equal("Font1", setFontAndSize.Font.Data);
            Assert.Equal(12.75, setFontAndSize.Size);
        }

        [Fact]
        public void HasCorrectOperator()
        {
            var setFontAndSize = new SetFontAndSize(Font1Name, 12);

            Assert.Equal("Tf", setFontAndSize.Operator);
        }

        [Fact]
        public void NameNullThrows()
        {
            // ReSharper disable once ObjectCreationAsStatement
            Action action = () => new SetFontAndSize(null, 6);

            Assert.Throws<ArgumentNullException>(action);
        }

        [Fact]
        public void StringRepresentationIsCorrect()
        {
            var setFontAndSize = new SetFontAndSize(Font1Name, 12.76);

            Assert.Equal("/Font1 12.76 Tf", setFontAndSize.ToString());
        }

        [Fact]
        public void RunSetsFontAndFontSize()
        {
            var setFontAndSize = new SetFontAndSize(Font1Name, 69.42);

            var context = new TestOperationContext();

            setFontAndSize.Run(context);

            var state = context.GetCurrentState();

            Assert.Equal(69.42, state.FontState.FontSize);
            Assert.Equal(Font1Name, state.FontState.FontName);
        }
    }
}
