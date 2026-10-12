namespace UglyToad.PdfPig.Filters.CcittFax
{
    /// <summary>Reports decoded rows, input consumption, termination and cumulative buffer allocations.</summary>
    /// <remarks>Rejection reports zero rows and consumption, but retains allocations for budgeting a retry.
    /// DecodedRows includes rows beyond Height; AllocatedBufferBytes includes replaced arrays.</remarks>
    internal readonly struct CcittFaxDecodeResult
    {
        /// <summary>Number of fully decoded encoded rows, including rows discarded beyond image Height.</summary>
        internal int DecodedRows { get; }

        /// <summary>Byte-rounded input boundary, excluding whole bytes loaded only for lookahead.</summary>
        /// <remarks>This is not a PDF-file offset. Use Status to distinguish a verified boundary from rejection.</remarks>
        internal int BytesConsumed { get; }

        /// <summary>Stopping condition for this attempt; NotCompleted means no usable encoded boundary.</summary>
        internal CcittFaxDecodeStatus Status { get; }

        /// <summary>Cumulative array bytes charged to this attempt, retained when a legacy retry is needed.</summary>
        internal long AllocatedBufferBytes { get; }

        internal CcittFaxDecodeResult(int decodedRows,
            int bytesConsumed,
            CcittFaxDecodeStatus status,
            long allocatedBufferBytes)
        {
            DecodedRows = decodedRows;
            BytesConsumed = bytesConsumed;
            Status = status;
            AllocatedBufferBytes = allocatedBufferBytes;
        }
    }

    /// <summary>Identifies why decoding stopped; consumption alone does not establish a valid image boundary.</summary>
    internal enum CcittFaxDecodeStatus
    {
        /// <summary>No verified image boundary is available, including after a legacy retry.</summary>
        NotCompleted,

        /// <summary>A complete Group 3 RTC or Group 4 EOFB was consumed, including marker bits.</summary>
        EndOfBlock,

        /// <summary>Input ended between complete rows with permitted zero padding or declared-Rows zero fill.</summary>
        EndOfInput,

        /// <summary>The configured row limit was reached with EndOfBlock disabled.</summary>
        RowLimit,

        /// <summary>A shortened marker and an all-zero tail were accepted after the required complete-row checks.</summary>
        IncompleteEndOfBlock
    }
}