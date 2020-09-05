namespace UglyToad.PdfPig.Writer.Copier
{
    using System;
    using System.IO;
    using Core;
    using Tokens;

    internal static class TokenHelper
    {
        public static T GetTokenAs<T>(IToken token, Func<IndirectReferenceToken, IToken> lookupFunc) where T : IToken
        {
            var original = token;

            while (true)
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
                    default:
                        throw new InvalidOperationException($"Unable to extract a {typeof(T)} token from {original}");
                }
            }
        }
    }
}
