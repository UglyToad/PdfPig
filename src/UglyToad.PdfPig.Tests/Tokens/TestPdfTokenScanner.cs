// ReSharper disable ObjectCreationAsStatement
namespace UglyToad.PdfPig.Tests.Tokens
{
    using PdfPig.Core;
    using PdfPig.Tokenization;
    using PdfPig.Tokenization.Scanner;
    using PdfPig.Tokens;

    internal class TestPdfTokenScanner : IPdfTokenScanner
    {
        public Dictionary<IndirectReference, ObjectToken> Objects { get; } = new Dictionary<IndirectReference, ObjectToken>();
        public bool MoveNext()
        {
            throw new NotImplementedException();
        }

        public IToken CurrentToken { get; set; }
        public bool TryReadToken<T>(out T token) where T : class, IToken
        {
            throw new NotImplementedException();
        }

        public void Seek(long position)
        {
            throw new NotImplementedException();
        }

        public long CurrentPosition { get; set; }
        public long Length { get; } = 10;

        public void RegisterCustomTokenizer(byte firstByte, ITokenizer tokenizer)
        {
            throw new NotImplementedException();
        }

        public void DeregisterCustomTokenizer(ITokenizer tokenizer)
        {
            throw new NotImplementedException();
        }

        public ObjectToken Get(IndirectReference reference)
        {
            return Objects[reference];
        }

        public void ReplaceToken(IndirectReference reference, IToken token)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
        }
    }
}
