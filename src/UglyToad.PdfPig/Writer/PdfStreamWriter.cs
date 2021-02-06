namespace UglyToad.PdfPig.Writer
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using Core;
    using Graphics.Operations;
    using Tokens;

    /// <summary>
    /// This class would lazily flush all token. Allowing us to make changes to references without need to rewrite the whole stream
    /// </summary>
    internal class PdfStreamWriter : IPdfStreamWriter
    {
        private Dictionary<IndirectReference, long> offsets = new Dictionary<IndirectReference, long>();
        private const decimal DefaultVersion = 1.2m;
        private bool Initialized { get; set; }
        private int CurrentNumber { get; set; } = 1;

        public Stream Stream { get; set; }

        private bool DisposeStream { get; set; }

        public PdfStreamWriter(Stream baseStream, bool disposeStream = true)
        {
            Stream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
            if (!baseStream.CanWrite)
            {
                throw new ArgumentException("Output stream must be writable");
            }

            DisposeStream = disposeStream;
        }

        public void InitializePdf(decimal version)
        {
            WriteString($"%PDF-{version.ToString("0.0", CultureInfo.InvariantCulture)}", Stream);

            Stream.WriteText("%");
            Stream.WriteByte(169);
            Stream.WriteByte(205);
            Stream.WriteByte(196);
            Stream.WriteByte(210);
            Stream.WriteNewLine();
        }

        public void Dispose()
        {
            if (!DisposeStream)
            {
                Stream = null;
                return;
            }

            Stream?.Dispose();
            Stream = null;
        }


        private static void WriteString(string text, Stream stream)
        {
            var bytes = OtherEncodings.StringAsLatin1Bytes(text);
            stream.Write(bytes, 0, bytes.Length);
            stream.WriteNewLine();
        }

        public IndirectReferenceToken WriteToken(IToken token)
        {
            if (!Initialized)
            {
                InitializePdf(DefaultVersion);
            }

            var ir = ReserveObjectNumber();
            offsets.Add(ir.Data, Stream.Position);
            var obj = new ObjectToken(Stream.Position, ir.Data, token);
            TokenWriter.WriteToken(obj, Stream);
            return ir;
        }

        public IndirectReferenceToken WriteToken(IToken token, IndirectReferenceToken indirectReference)
        {
            if (!Initialized)
            {
                InitializePdf(DefaultVersion);
            }

            offsets.Add(indirectReference.Data, Stream.Position);
            var obj = new ObjectToken(Stream.Position, indirectReference.Data, token);
            TokenWriter.WriteToken(obj, Stream);
            return indirectReference;
        }

        public IndirectReferenceToken ReserveObjectNumber()
        {
            return new IndirectReferenceToken(new IndirectReference(CurrentNumber++, 0));
        }


        public void CompletePdf(IndirectReferenceToken catalogReference, IndirectReferenceToken documentInformationReference=null)
        {
            TokenWriter.WriteCrossReferenceTable(offsets, catalogReference.Data, Stream, documentInformationReference?.Data);
        }
    }
}
