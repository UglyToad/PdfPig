namespace UglyToad.PdfPig.Tests.Graphics.Operations
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using PdfPig.Graphics.Operations;
    using PdfPig.Graphics.Operations.InlineImages;
    using PdfPig.Tokens;
    using Xunit;

    public class GraphicsStateOperationTests
    {
        [Fact]
        public void AllOperationsArePublic()
        {
            foreach (var operationType in GetOperationTypes())
            {
                Assert.True(operationType.IsPublic, $"{operationType.Name} should be public.");
            }
        }

        [Fact]
        public void AllOperationsCanBeWritten()
        {
            foreach (var operationType in GetOperationTypes())
            {
                IGraphicsStateOperation operation;

                var constructors = operationType.GetConstructors();

                if (constructors.Length == 0)
                {
                    var field = operationType.GetFields(BindingFlags.Public | BindingFlags.Static)
                        .FirstOrDefault(x => x.Name == "Value");

                    if (field == null)
                    {
                        throw new InvalidOperationException($"Could not find singleton for type: {operationType.Name}.");
                    }

                    operation = (IGraphicsStateOperation)field.GetValue(null);
                }
                else if (operationType == typeof(EndInlineImage))
                {
                    operation = new EndInlineImage(new List<byte>());
                }
                else if (operationType == typeof(BeginInlineImageData))
                {
                    operation = new BeginInlineImageData(new Dictionary<NameToken, IToken>());
                }
                else
                {
                    var constructor = constructors[0];

                    var parameterTypes = constructor.GetParameters();

                    var parameters = GetConstructorParameters(parameterTypes);

                    operation = (IGraphicsStateOperation)constructor.Invoke(parameters);
                }

                using (var memoryStream = new MemoryStream())
                {
                    operation.Write(memoryStream);
                }
            }
        }

        private static IEnumerable<Type> GetOperationTypes()
        {
            var assembly = Assembly.GetAssembly(typeof(IGraphicsStateOperation));

            var operationTypes = assembly.GetTypes().Where(x => typeof(IGraphicsStateOperation).IsAssignableFrom(x)
                                                                && !x.IsInterface);

            return operationTypes;
        }

        private static object[] GetConstructorParameters(ParameterInfo[] parameters)
        {
            var result = new object[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                var type = param.ParameterType;

                if (type == typeof(string))
                {
                    result[i] = "Toad";
                }
                else if (type == typeof(NameToken))
                {
                    result[i] = NameToken.Create("Hog");
                }
                else if (type == typeof(decimal))
                {
                    result[i] = 0.5m;
                }
                else if (type == typeof(int))
                {
                    result[i] = 1;
                }
                else if (type == typeof(decimal[]) || type == typeof(IReadOnlyList<decimal>))
                {
                    result[i] = new decimal[]
                    {
                        1, 0, 0, 1, 2, 5
                    };
                }
                else if (type == typeof(IReadOnlyList<IToken>))
                {
                    result[i] = new IToken[] { new StringToken("Text") };
                }
            }

            return result;
        }
    }
}
