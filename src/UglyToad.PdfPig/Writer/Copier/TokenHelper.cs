namespace UglyToad.PdfPig.Writer.Copier
{
    using System;
    using Tokens;

    internal static class TokenHelper
    {
        // This is to avoid infinite loop in production. Although, it should never happen
        const int MAX_ITERATIONS = 10;

        public static T GetTokenAs<T>(IToken token, Func<IndirectReferenceToken, IToken> lookupFunc) where T : IToken
        {
            var iterations = 0;

            var original = token;
            while (iterations++ < MAX_ITERATIONS)
            {
                switch (token)
                {
                    case T result:
                        return result;
                    case IndirectReferenceToken tokenReference:
                        token = lookupFunc(tokenReference);
                        continue;
                    case ObjectToken tokenObject:
                        token = tokenObject.Data;
                        continue;
                }
            }

            throw new InvalidOperationException($"Unable to extract a {typeof(T)} token from {original}");
        }
    }
}