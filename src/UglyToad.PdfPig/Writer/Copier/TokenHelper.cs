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

            checkToken:
            switch (token)
            {
                case T retval:
                    return retval;
                case IndirectReferenceToken tokenReference:
                    token = lookupFunc(tokenReference);
                    goto checkToken;
                case ObjectToken tokenObject:
                    token = tokenObject.Data;
                    goto checkToken;
                default:
                    throw new IOException($"Unable to extract a {typeof(T)} token from {original}");
            }
        }
    }
}
