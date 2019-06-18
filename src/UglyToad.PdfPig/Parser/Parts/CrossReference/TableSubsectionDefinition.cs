namespace UglyToad.PdfPig.Parser.Parts.CrossReference
{
    using System;
    using System.Globalization;
    using IO;
    using Logging;

    /// <summary>
    /// Each subsection of the cross-reference table starts with a line defining the starting object number
    /// and the count of objects in the subsection.
    /// </summary>
    /// <example>
    /// xref
    /// 12 16
    /// ...
    /// 
    /// Defines a table subsection that starts with object 12 and has 16 entries (12-27).
    /// </example>
    internal struct TableSubsectionDefinition
    {
        private static readonly char[] Splitters = { ' ' };

        /// <summary>
        /// The first object number in the table.
        /// </summary>
        public long FirstNumber { get; }

        /// <summary>
        /// The number of consecutive objects declared in the table.
        /// </summary>
        public int Count { get; }

        /// <summary>
        /// Create a new <see cref="TableSubsectionDefinition"/> to define a range of consecutive objects in the cross-reference table.
        /// </summary>
        public TableSubsectionDefinition(long firstNumber, int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), $"Count must be 0 or positive, instead it was {count}.");
            }

            FirstNumber = firstNumber;
            Count = count;
        }

        /// <summary>
        /// Attempts to read the <see cref="TableSubsectionDefinition"/> from the current line of the source.
        /// </summary>
        public static bool TryRead(ILog log, IInputBytes bytes, out TableSubsectionDefinition definition)
        {
            definition = default(TableSubsectionDefinition);

            var line = ReadHelper.ReadLine(bytes);

            var parts = line.Split(Splitters, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                return false;
            }

            try
            {
                var firstObjectId = long.Parse(parts[0], CultureInfo.InvariantCulture);
                var objectCount = int.Parse(parts[1], CultureInfo.InvariantCulture);

                definition = new TableSubsectionDefinition(firstObjectId, objectCount);

                return true;
            }
            catch (Exception ex)
            {
                log.Error(
                    $"The format for the subsection definition was invalid, expected [long] [int], instead got '{line}'", ex);

                return false;
            }
        }

        public override string ToString()
        {
            return $"{FirstNumber} {Count}";
        }
    }
}