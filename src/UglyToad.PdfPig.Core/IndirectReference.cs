﻿namespace UglyToad.PdfPig.Core
{
    using System;
    using System.Diagnostics;

    /// <summary>
    /// Used to uniquely identify and refer to objects in the PDF file.
    /// </summary>
    public readonly struct IndirectReference
    {
        /// <summary>
        /// A positive integer object number.
        /// </summary>
        public long ObjectNumber { get; }

        /// <summary>
        /// A non-negative integer generation number which starts as 0 and increases if the file is updated incrementally.
        /// </summary>
        public int Generation { get; }

        /// <summary>
        /// Create a new <see cref="IndirectReference"/>
        /// </summary>
        /// <param name="objectNumber">The object number.</param>
        /// <param name="generation">The generation number.</param>
        [DebuggerStepThrough]
        public IndirectReference(long objectNumber, int generation)
        {
            ObjectNumber = objectNumber;
            Generation = generation;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is IndirectReference reference)
            {
                return reference.ObjectNumber == ObjectNumber
                       && reference.Generation == Generation;
            }

            return false;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return HashCode.Combine(ObjectNumber, Generation);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"{ObjectNumber} {Generation}";
        }
    }
}