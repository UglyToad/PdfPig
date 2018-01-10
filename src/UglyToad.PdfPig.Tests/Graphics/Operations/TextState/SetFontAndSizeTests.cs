namespace UglyToad.PdfPig.Tests.Graphics.Operations.TextState
{
    using System;
    using PdfPig.Cos;
    using PdfPig.Graphics.Operations.TextState;
    using Xunit;

    public class SetFontAndSizeTests
    {
        private static readonly CosName Font1Name = CosName.Create("Font1");

        [Fact]
        public void HasCorrectSymbol()
        {
            var symbol = SetFontAndSize.Symbol;

            Assert.Equal("Tf", symbol);
        }

        [Fact]
        public void SetsValues()
        {
            var setFontAndSize = new SetFontAndSize(Font1Name, 12.75m);

            Assert.Equal("Font1", setFontAndSize.Font.Name);
            Assert.Equal(12.75m, setFontAndSize.Size);
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
            var setFontAndSize = new SetFontAndSize(Font1Name, 12.76m);

            Assert.Equal("/Font1 12.76 Tf", setFontAndSize.ToString());
        }

        [Fact]
        public void RunSetsFontAndFontSize()
        {
            var setFontAndSize = new SetFontAndSize(Font1Name, 69.42m);

            var context = new TestOperationContext();
            var store = new TestResourceStore();

            setFontAndSize.Run(context, store);

            var state = context.GetCurrentState();

            Assert.Equal(69.42m, state.FontState.FontSize);
            Assert.Equal(Font1Name, state.FontState.FontName);
        }
    }
}
