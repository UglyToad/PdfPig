namespace UglyToad.PdfPig.Filters.CcittFax
{
    /// <summary>Defines storage, framing and buffer limits for one CCITT decode attempt.</summary>
    /// <remarks>DeclaredRows controls termination; MaximumStoredRows (Height) limits output;
    /// InitialRowCapacity is only a storage hint. The filter validates dimensions before construction.</remarks>
    internal readonly struct CcittFaxDecodeOptions
    {
        /// <summary>Validated positive row width in pixels.</summary>
        internal int Columns { get; }

        /// <summary>Initial bitmap capacity in complete rows; decoding may grow it within the budget.</summary>
        internal int InitialRowCapacity { get; }

        /// <summary>Resolved MH, Group 3 or Group 4 row coding and framing.</summary>
        internal CcittFaxCompressionType CompressionType { get; }

        /// <summary>Whether row boundaries discard bits up to the next byte boundary.</summary>
        internal bool EncodedByteAlign { get; }

        /// <summary>Whether black pixels, rather than white pixels, use set output bits.</summary>
        internal bool BlackIsOne { get; }

        /// <summary>Whether an EOL may precede rows whose coding family does not require one.</summary>
        internal bool AcceptOptionalEndOfLine { get; }

        /// <summary>Whether RTC or EOFB is checked before decoding the next row.</summary>
        internal bool EndOfBlock { get; }

        /// <summary>Whether Group 3/4 row-framing probes require an EOL before decoding pixels.</summary>
        internal bool RequireEndOfLine { get; }

        /// <summary>Whether reaching a fixed bitmap capacity also requires a valid stopping condition.</summary>
        internal bool VerifyEndMarker { get; }

        /// <summary>Whether the documented complete-row and zero-tail exceptions may authorize termination.</summary>
        internal bool AllowIncompleteTermination { get; }

        /// <summary>The PDF Rows value, independent of stored height; zero means unspecified.</summary>
        internal int DeclaredRows { get; }

        /// <summary>Whether EOF between complete rows with final-byte zero padding is accepted.</summary>
        internal bool AllowEndOfInput { get; }

        /// <summary>Cumulative buffer limit, including replaced arrays. Production always supplies a positive limit.</summary>
        /// <remarks>Zero is used for fixed-capacity decoding without an allocation ceiling. Growing
        /// the bitmap or decoding into a discarded-row scratch buffer requires a positive limit.</remarks>
        internal long MaximumAllocatedBufferBytes { get; }

        /// <summary>Maximum rows kept in the output bitmap; zero means no image-height limit.</summary>
        internal int MaximumStoredRows { get; }

        /// <summary>Maximum encoded rows to process when EndOfBlock is disabled; zero means no row-count stop.</summary>
        internal int MaximumDecodedRows { get; }

        /// <summary>Stores resolved parameters without validating or allocating decoder state.</summary>
        internal CcittFaxDecodeOptions(int columns,
            int initialRowCapacity,
            CcittFaxCompressionType compressionType,
            bool encodedByteAlign,
            bool blackIsOne,
            bool acceptOptionalEndOfLine = false,
            bool endOfBlock = false,
            bool requireEndOfLine = true,
            bool verifyEndMarker = false,
            bool allowIncompleteTermination = false,
            int declaredRows = 0,
            bool allowEndOfInput = false,
            long maximumAllocatedBufferBytes = 0,
            int maximumStoredRows = 0,
            int maximumDecodedRows = 0)
        {
            Columns = columns;
            InitialRowCapacity = initialRowCapacity;
            CompressionType = compressionType;
            EncodedByteAlign = encodedByteAlign;
            BlackIsOne = blackIsOne;
            AcceptOptionalEndOfLine = acceptOptionalEndOfLine;
            EndOfBlock = endOfBlock;
            RequireEndOfLine = requireEndOfLine;
            VerifyEndMarker = verifyEndMarker;
            AllowIncompleteTermination = allowIncompleteTermination;
            DeclaredRows = declaredRows;
            AllowEndOfInput = allowEndOfInput;
            MaximumAllocatedBufferBytes = maximumAllocatedBufferBytes;
            MaximumStoredRows = maximumStoredRows;
            MaximumDecodedRows = maximumDecodedRows;
        }
    }
}