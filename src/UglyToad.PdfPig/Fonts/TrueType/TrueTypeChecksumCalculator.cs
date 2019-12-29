namespace UglyToad.PdfPig.Fonts.TrueType
{
    using System;
    using System.Collections.Generic;
    using IO;

    internal static class TrueTypeChecksumCalculator
    {
        private const string HeaderTableTag = "head";

        // Preceded by 2 32-fixed fraction values.
        private const int ChecksumAdjustmentPosition = 8;
        
        public static uint CalculateWholeFontChecksum(IInputBytes bytes, TrueTypeHeaderTable headerTable)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (!IsHeadTable(headerTable))
            {
                throw new ArgumentException($"Can only calculate checksum for the whole font when the head table is provided. Got: {headerTable}.");
            }

            bytes.Seek(0);

            return Calculate(ToChecksumSkippedEnumerable(bytes, headerTable));
        }

        public static uint Calculate(IInputBytes bytes, TrueTypeHeaderTable table)
        {
            bytes.Seek(table.Offset);

            if (IsHeadTable(table))
            {
                // To calculate the checkSum for the 'head' table which itself includes the 
                // checkSumAdjustment entry for the entire font, do the following:
                // Set the checkSumAdjustment to 0.
                // Calculate the checksum as normal.
                var fullTableBytes = new byte[table.Length];
                var read = bytes.Read(fullTableBytes);
                if (read != table.Length)
                {
                    throw new InvalidOperationException();
                }

                // Zero out the checksum adjustment
                fullTableBytes[ChecksumAdjustmentPosition] = 0;
                fullTableBytes[ChecksumAdjustmentPosition + 1] = 0;
                fullTableBytes[ChecksumAdjustmentPosition + 2] = 0;
                fullTableBytes[ChecksumAdjustmentPosition + 3] = 0;

                return Calculate(fullTableBytes);
            }

            var result = 0u;

            unchecked
            {
                while (TryReadUInt(bytes, table.Offset + table.Length, out var next))
                {
                    result += next;
                }
            }

            return result;
        }

        /// <summary>
        /// Calculate the TrueType checksum for the provided bytes.
        /// </summary>
        public static uint Calculate(IEnumerable<byte> bytes)
        {
            var result = 0u;

            unchecked
            {
                using (var enumerator = bytes.GetEnumerator())
                {
                    while (TryReadUInt(enumerator, out var next))
                    {
                        result += next;
                    }
                }
            }

            return result;
        }

        private static bool IsHeadTable(TrueTypeHeaderTable table) => string.Equals(HeaderTableTag, table.Tag, StringComparison.OrdinalIgnoreCase);

        private static bool TryReadUInt(IEnumerator<byte> enumerator, out uint result)
        {
            result = 0;

            if (!enumerator.MoveNext())
            {
                return false;
            }

            var top = enumerator.Current;
            var three = enumerator.MoveNext() ? enumerator.Current : 0;
            var two = enumerator.MoveNext() ? enumerator.Current : 0;
            var one = enumerator.MoveNext() ? enumerator.Current : 0;

            result = (uint)(((long)top << 24)
                   + ((long)three << 16)
                   + (two << 8)
                   + (one << 0));

            return true;
        }

        private static bool TryReadUInt(IInputBytes input, long endAt, out uint result)
        {
            result = 0;

            byte ReadNext()
            {
                if (input.CurrentOffset == endAt || !input.MoveNext())
                {
                    return 0;
                }

                return input.CurrentByte;
            }

            if (input.CurrentOffset >= endAt)
            {
                return false;
            }

            var top = ReadNext();
            var three = ReadNext();
            var two = ReadNext();
            var one = ReadNext();

            result = (uint)(((long)top << 24)
                            + ((long)three << 16)
                            + (two << 8)
                            + (one << 0));

            return true;
        }

        private static IEnumerable<byte> ToChecksumSkippedEnumerable(IInputBytes bytes, TrueTypeHeaderTable table)
        {
            while (bytes.MoveNext())
            {
                // Skip checksum adjustment
                if (bytes.CurrentOffset > table.Offset + ChecksumAdjustmentPosition && bytes.CurrentOffset <= table.Offset + ChecksumAdjustmentPosition + 4)
                {
                    continue;
                }

                yield return bytes.CurrentByte;
            }
        }
    }
}
