namespace UglyToad.PdfPig.ContentStream
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    internal class PdfBoolean : IEquatable<PdfBoolean>
    {
        /// <summary>
        /// The bytes representing a true value in the PDF content.
        /// </summary>
        /// <remarks>Equivalent to the text "true".</remarks>
        private static readonly byte[] TrueBytes = {116, 114, 117, 101};

        /// <summary>
        /// The bytes representing a false value in the PDF content.
        /// </summary>
        /// <remarks>Equivalent to the text "false".</remarks>
        private static readonly byte[] FalseBytes = {102, 97, 108, 115, 101};
        
        /// <summary>
        /// The PDF boolean representing <see langword="true"/>.
        /// </summary>
        public static PdfBoolean True { get; } = new PdfBoolean(true);

        /// <summary>
        /// The PDF boolean representing <see langword="false"/>.
        /// </summary>
        public static PdfBoolean False { get; } = new PdfBoolean(false);

        /// <summary>
        /// The boolean value.
        /// </summary>
        public bool Value { get; }

        private PdfBoolean(bool value)
        {
            Value = value;
        }

        /// <summary>
        /// Supports casting from <see cref="bool"/> to <see cref="PdfBoolean"/>.
        /// </summary>
        public static explicit operator PdfBoolean(bool value)
        {
            return value ? True : False;
        }

        /// <summary>
        /// Supports treatment of <see cref="PdfBoolean"/> as a <see cref="bool"/>.
        /// </summary>
        public static implicit operator bool(PdfBoolean value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return value.Value;
        }

        public static bool operator ==(PdfBoolean boolean1, PdfBoolean boolean2)
        {
            return EqualityComparer<PdfBoolean>.Default.Equals(boolean1, boolean2);
        }

        public static bool operator !=(PdfBoolean boolean1, PdfBoolean boolean2)
        {
            return !(boolean1 == boolean2);
        }

        public override string ToString()
        {
            return Value ? "true" : "false";
        }

        public void WriteToPdfStream(BinaryWriter output)
        {
            if (output == null)
            {
                throw new ArgumentNullException(nameof(output));
            }

            if (Value)
            {
                output.Write(TrueBytes);
            }
            else
            {
                output.Write(FalseBytes);
            }
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as PdfBoolean);
        }

        public bool Equals(PdfBoolean other)
        {
            return other != null &&
                   Value == other.Value;
        }

        public override int GetHashCode()
        {
            return -1937169414 + Value.GetHashCode();
        }
    }
}
