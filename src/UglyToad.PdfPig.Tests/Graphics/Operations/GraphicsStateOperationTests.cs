namespace UglyToad.PdfPig.Tests.Graphics.Operations
{
    using System.Reflection;
    using PdfPig.Graphics.Operations;
    using PdfPig.Graphics.Operations.InlineImages;
    using PdfPig.Tokens;
    using UglyToad.PdfPig.Graphics;

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
                    operation = new EndInlineImage([]);
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

        // Test that ReflectionGraphicsStateOperationFactory.operations contains all supported graphics operations
        [Fact]
        public void ReflectionGraphicsStateOperationFactoryKnowsAllOperations()
        {
            var operationsField = typeof(ReflectionGraphicsStateOperationFactory)
                .GetField("Operations", BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(operationsField);

            var operationDictionaryRaw = operationsField.GetValue(null) as IReadOnlyDictionary<string, Type>;

            Assert.NotNull(operationDictionaryRaw);

            var operationDictionary = new Dictionary<string, Type>(operationDictionaryRaw!.ToDictionary(x => x.Key, x => x.Value));

            var allOperations = GetOperationTypes();

            Assert.Equal(allOperations.Count, operationDictionary.Count);

            var mapped = allOperations.Select(o =>
            {
                var symbol = o.GetField("Symbol").GetValue(null)!.ToString()!;
                return new KeyValuePair<string, Type>(symbol, o);
            });

            Assert.Equivalent(operationDictionary, mapped, strict: true);
        }

        private static IReadOnlyList<Type> GetOperationTypes()
        {
            var assembly = Assembly.GetAssembly(typeof(IGraphicsStateOperation));

            var operationTypes = assembly.GetTypes().Where(x => typeof(IGraphicsStateOperation).IsAssignableFrom(x)
                                                                && !x.IsInterface);

            return operationTypes.ToList();
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
                else if (type == typeof(double))
                {
                    result[i] = 0.5;
                }
                else if (type == typeof(int))
                {
                    result[i] = 1;
                }
                else if (type == typeof(double[]) || type == typeof(IReadOnlyList<double>))
                {
                    result[i] = new double[]
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
