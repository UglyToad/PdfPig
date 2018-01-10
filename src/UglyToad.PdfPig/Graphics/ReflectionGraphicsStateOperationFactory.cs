namespace UglyToad.PdfPig.Graphics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Cos;
    using Operations;
    using Operations.TextShowing;
    using Tokenization.Tokens;

    internal class ReflectionGraphicsStateOperationFactory : IGraphicsStateOperationFactory
    {
        private readonly IReadOnlyDictionary<string, Type> operations;

        public ReflectionGraphicsStateOperationFactory()
        {
            var assemblyTypes = Assembly.GetAssembly(typeof(ReflectionGraphicsStateOperationFactory)).GetTypes();

            var result = new Dictionary<string, Type>();

            foreach (var assemblyType in assemblyTypes)
            {
                if (!assemblyType.IsInterface && typeof(IGraphicsStateOperation).IsAssignableFrom(assemblyType))
                {
                    var symbol = assemblyType.GetField("Symbol");

                    if (symbol == null)
                    {
                        throw new InvalidOperationException("An operation type was defined without the public const Symbol being declared. Type was: " + assemblyType.FullName);
                    }

                    var value = symbol.GetValue(null).ToString();

                    result[value] = assemblyType;
                }
            }

            operations = result;
        }

        public IGraphicsStateOperation Create(OperatorToken op, IReadOnlyList<IToken> operands)
        {
            switch (op.Data)
            {
                case ShowText.Symbol:
                    if (operands.Count != 1)
                    {
                        throw new InvalidOperationException($"Attempted to create a show text operation with {operands.Count} operands.");
                    }

                    if (operands[0] is StringToken s)
                    {
                        return new ShowText(s.Data);
                    }
                    else if (operands[0] is HexToken h)
                    {
                        return new ShowText(h.Bytes.ToArray());
                    }
                    else
                    {
                        throw new InvalidOperationException($"Tried to create a show text operation with operand type: {operands[0]?.GetType().Name ?? "null"}");
                    }
                case ShowTextsWithPositioning.Symbol:
                    if (operands.Count == 0)
                    {
                        throw new InvalidOperationException("Cannot have 0 parameters for a TJ operator.");
                    }

                    if (operands.Count == 1 && operands[0] is ArrayToken arrayToken)
                    {
                        return new ShowTextsWithPositioning(arrayToken.Data);
                    }

                    var array = operands.ToArray();

                    return new ShowTextsWithPositioning(array);
            }

            if (!operations.TryGetValue(op.Data, out Type operationType))
            {
                return null;
            }

            var constructors = operationType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (constructors.Length == 0)
            {
                throw new InvalidOperationException("No constructors to invoke were found for operation type: " + operationType.FullName);
            }

            // This only works by luck...
            var constructor = constructors[0];

            if (constructor.IsPrivate)
            {
                return (IGraphicsStateOperation)operationType.GetField("Value").GetValue(null);
            }

            var parameters = constructor.GetParameters();

            var offset = 0;

            var arguments = new List<object>();

            foreach (var parameter in parameters)
            {
                if (parameter.ParameterType == typeof(decimal))
                {
                    if (operands[offset] is NumericToken numeric)
                    {
                        arguments.Add(numeric.Data);   
                    }
                    else
                    {
                        throw new InvalidOperationException($"Expected a decimal parameter for operation type {operationType.FullName}. Instead got: {operands[offset]}");
                    }

                    offset++;
                }
                else if (parameter.ParameterType == typeof(int))
                {
                    if (operands[offset] is NumericToken numeric)
                    {
                        arguments.Add(numeric.Int);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Expected an integer parameter for operation type {operationType.FullName}. Instead got: {operands[offset]}");
                    }

                    offset++;
                }
                else if (parameter.ParameterType == typeof(decimal[]))
                {
                    if (operands[offset] is ArrayToken arr)
                    {
                        arguments.Add(arr.Data.OfType<NumericToken>().Select(x => x.Data).ToArray());
                        offset++;
                        continue;
                    }

                    var array = new List<decimal>();
                    while (offset < operands.Count && operands[offset] is NumericToken numeric)
                    {
                        array.Add(numeric.Data);
                        offset++;
                    }

                    arguments.Add(array.ToArray());
                }
                else if (parameter.ParameterType == typeof(CosName))
                {
                    if (operands[offset] is NameToken name)
                    {
                        arguments.Add(name.Data);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Expected a decimal array parameter for operation type {operationType.FullName}. Instead got: {operands[offset]}");
                    }

                    offset++;
                }
                else if (parameter.ParameterType == typeof(string))
                {
                    if (operands[offset] is StringToken stringToken)
                    {
                        arguments.Add(stringToken.Data);
                    }
                    else if (operands[offset] is HexToken hexToken)
                    {
                        arguments.Add(hexToken.Data);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Expected a string parameter for operation type {operationType.FullName}. Instead got: {operands[offset]}");
                    }

                    offset++;
                }
                else
                {
                    throw new NotImplementedException($"Unsupported parameter type {parameter.ParameterType.FullName} for operation type {operationType.FullName}.");
                }
            }

            var result = constructor.Invoke(arguments.ToArray());

            return (IGraphicsStateOperation)result;
        }
    }
}