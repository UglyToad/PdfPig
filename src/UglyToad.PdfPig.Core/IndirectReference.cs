namespace UglyToad.PdfPig.Core
{
    using System;
    using System.Diagnostics;

    // https://github.com/apache/pdfbox/blob/trunk/pdfbox/src/main/java/org/apache/pdfbox/cos/COSObjectKey.java#L25

    /// <summary>
    /// Used to uniquely identify and refer to objects in the PDF file.
    /// </summary>
    public readonly struct IndirectReference : IEquatable<IndirectReference>
    {
        private const int NUMBER_OFFSET = sizeof(ushort) * 8;
        private static readonly long GENERATION_MASK = (long)Math.Pow(2, NUMBER_OFFSET) - 1;
        private static readonly long MAX_OBJECT_NUMBER = (long)(Math.Pow(2, sizeof(long) * 8 - NUMBER_OFFSET) - 1) / 2;

        // combined number and generation
        // The lowest 16 bits hold the generation 0-65535
        // The rest is used for the number (even though 34 bit are sufficient for 10 digits)
        private readonly long numberAndGeneration;

        /// <summary>
        /// A positive integer object number.
        /// </summary>
        // Below is different from PdfBox as we keep the sign of the offset number (use >> instead of >>> (unsigned right shift))
        public long ObjectNumber => numberAndGeneration >> NUMBER_OFFSET;

        /// <summary>
        /// A non-negative integer generation number which starts as 0 and increases if the file is updated incrementally.
        /// <para>The maximum generation number is 65,535.</para>
        /// </summary>
        public int Generation => (int)(numberAndGeneration & GENERATION_MASK);

        /// <summary>
        /// Create a new <see cref="IndirectReference"/>
        /// </summary>
        /// <param name="objectNumber">The object number.</param>
        /// <param name="generation">The generation number.</param>
        [DebuggerStepThrough]
        public IndirectReference(long objectNumber, int generation)
        {
            if (generation < 0)
            {
                // Note: We do not check generation for max value and let it overflow
                throw new ArgumentOutOfRangeException(nameof(generation), "Generation number must not be a negative value.");
            }

            if (objectNumber < -MAX_OBJECT_NUMBER || objectNumber > MAX_OBJECT_NUMBER)
            {
                throw new ArgumentOutOfRangeException(nameof(objectNumber), $"Object number must be between -{MAX_OBJECT_NUMBER:##,###} and {MAX_OBJECT_NUMBER:##,###}.");
            }

            numberAndGeneration = ComputeInternalHash(objectNumber, generation);
        }

        /// <summary>
        /// Calculate the internal hash value for the given object number and generation number.
        /// </summary>
        /// <param name="num">The object number.</param>
        /// <param name="gen">The generation number.</param>
        /// <returns>The internal hash for the given values.</returns>
        private static long ComputeInternalHash(long num, int gen)
        {
            return num << NUMBER_OFFSET | (gen & GENERATION_MASK);
        }

        /// <inheritdoc />
        public bool Equals(IndirectReference other)
        {
            return other.numberAndGeneration == numberAndGeneration;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is IndirectReference other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return numberAndGeneration.GetHashCode();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{ObjectNumber} {Generation}";
        }
        
        /// <inheritdoc/>
        public static bool operator ==(IndirectReference left, IndirectReference right)
        {
            return left.Equals(right);
        }

        /// <inheritdoc/>
        public static bool operator !=(IndirectReference left, IndirectReference right)
        {
            return !(left == right);
        }
    }
}