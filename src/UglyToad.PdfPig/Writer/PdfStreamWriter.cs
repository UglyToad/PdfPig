namespace UglyToad.PdfPig.Writer
{
    using Core;
    using Graphics.Operations;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using Tokens;

    /// <summary>
    /// This class would lazily flush all token. Allowing us to make changes to references without need to rewrite the whole stream
    /// </summary>
    internal class PdfStreamWriter : IPdfStreamWriter
    {
        private readonly Action<double>? recordVersion;
        protected const double DefaultVersion = 1.2;
        protected Dictionary<IndirectReference, long> offsets = new Dictionary<IndirectReference, long>();
        protected bool DisposeStream { get; set; }
        protected bool Initialized { get; set; }
        protected int CurrentNumber { get; set; } = 1;
        protected readonly ITokenWriter TokenWriter;

        public Stream Stream { get; protected set; }

        public bool AttemptDeduplication { get; set; } = true;

        public bool WritingPageContents
        {
            get => TokenWriter.WritingPageContents;
            set => TokenWriter.WritingPageContents = value;
        }

        internal PdfStreamWriter(
            Stream baseStream,
            bool disposeStream = true,
            ITokenWriter? tokenWriter = null,
            Action<double>? recordVersion = null)
        {
            Stream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));

            if (!baseStream.CanWrite)
            {
                throw new ArgumentException("Output stream must be writable");
            }

            this.recordVersion = recordVersion;
            DisposeStream = disposeStream;
            TokenWriter = tokenWriter ?? new TokenWriter();
        }

        public virtual IndirectReferenceToken WriteToken(IToken token)
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

        public virtual IndirectReferenceToken WriteToken(IToken token, IndirectReferenceToken indirectReference)
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

        public void InitializePdf(double version)
        {
            recordVersion?.Invoke(version);

            Stream.WriteText($"%PDF-{version.ToString("0.0", CultureInfo.InvariantCulture)}");
            Stream.WriteNewLine();
            Stream.WriteText("%"u8);
            Stream.Write([169, 205, 196, 210]);
            Stream.WriteNewLine();
            Initialized = true;
        }

        public void CompletePdf(IndirectReferenceToken catalogReference, IndirectReferenceToken? documentInformationReference = null)
        {
            TokenWriter.WriteCrossReferenceTable(offsets, catalogReference.Data, Stream, documentInformationReference?.Data);
        }


        public void Dispose()
        {
            if (DisposeStream)
            {
                Stream?.Dispose();
            }
            
            Stream = null!;
        }
    }
}
