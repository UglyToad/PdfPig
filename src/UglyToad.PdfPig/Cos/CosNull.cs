namespace UglyToad.PdfPig.Cos
{
    using System;
    using System.IO;
    using Core;

    internal class CosNull : CosBase, ICosStreamWriter
    {
        /// <summary>
        /// The Null Token
        /// </summary>
        public static readonly byte[] NullBytes = { (byte)'n', (byte)'u', (byte)'l', (byte)'l' };
        
        /// <summary>
        /// The one <see cref="CosNull"/> object in the system.
        /// </summary>
        public static CosNull Null => NullLazy.Value;
        private static readonly Lazy<CosNull> NullLazy = new Lazy<CosNull>(() => new CosNull());


        /// <summary>
        /// Limit creation of <see cref="CosNull"/> to one instance.
        /// </summary>
        private CosNull() { }

        public override object Accept(ICosVisitor visitor)
        {
            return visitor.VisitFromNull(this);
        }

        public void WriteToPdfStream(BinaryWriter output)
        {
            output.Write(NullBytes);
        }

        public override string ToString()
        {
            return "COSNull";
        }
    }
}
