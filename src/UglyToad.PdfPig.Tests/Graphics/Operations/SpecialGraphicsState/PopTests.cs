namespace UglyToad.PdfPig.Tests.Graphics.Operations.SpecialGraphicsState
{
    using System;
    using PdfPig.Graphics;
    using PdfPig.Graphics.Operations.SpecialGraphicsState;
    using Xunit;

    public class PopTests
    {
        private readonly TestOperationContext context = new TestOperationContext();

        [Fact]
        public void PopSymbolCorrect()
        {
            Assert.Equal("Q", Pop.Symbol);
            Assert.Equal("Q", Pop.Value.Operator);
        }

        [Fact]
        public void CannotPopWithSingleFrame()
        {
            Action action = () => Pop.Value.Run(context);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void CannotPopWithNoFrames()
        {
            context.StateStack.Pop();

            Action action = () => Pop.Value.Run(context);

            Assert.Throws<InvalidOperationException>(action);
        }

        [Fact]
        public void PopsTopFrame()
        {
            context.StateStack.Push(new CurrentGraphicsState
            {
                LineWidth = 23
            });

            Pop.Value.Run(context);

            Assert.Equal(1, context.StackSize);
            Assert.Equal(1, context.GetCurrentState().LineWidth);
        }
    }
}
