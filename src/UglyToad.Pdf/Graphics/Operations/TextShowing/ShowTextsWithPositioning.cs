namespace UglyToad.Pdf.Graphics.Operations.TextShowing
{
    using System;
    using System.Collections.Generic;
    using Content;
    using Tokenization.Tokens;

    internal class ShowTextsWithPositioning : IGraphicsStateOperation
    {
        public const string Symbol = "TJ";

        public string Operator => Symbol;

        public IReadOnlyList<IToken> Array { get; }

        public ShowTextsWithPositioning(IReadOnlyList<IToken> array)
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            foreach (var token in array)
            {
                if (!(token is NumericToken) && !(token is HexToken)
                    && !(token is StringToken))
                {
                    throw new ArgumentException($"Found invalid token for showing texts with position: {token}");
                }
            }

            Array = array;
        }

        public void Run(IOperationContext operationContext, IResourceStore resourceStore)
        {
            operationContext.ShowPositionedText(Array);
        }
    }
}