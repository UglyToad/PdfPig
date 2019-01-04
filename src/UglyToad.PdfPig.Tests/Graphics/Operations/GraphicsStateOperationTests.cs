namespace UglyToad.PdfPig.Tests.Graphics.Operations
{
    using System.Linq;
    using System.Reflection;
    using PdfPig.Graphics.Operations;
    using Xunit;

    public class GraphicsStateOperationTests
    {
        [Fact]
        public void AllOperationsArePublic()
        {
            var assembly = Assembly.GetAssembly(typeof(IGraphicsStateOperation));

            var operationTypes = assembly.GetTypes().Where(x => typeof(IGraphicsStateOperation).IsAssignableFrom(x));

            foreach (var operationType in operationTypes)
            {
                Assert.True(operationType.IsPublic, $"{operationType.Name} should be public.");
            }
        }
    }
}
