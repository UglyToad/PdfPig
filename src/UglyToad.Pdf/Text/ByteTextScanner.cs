namespace UglyToad.Pdf.Text
{
    using System.Collections.Generic;
    using System.Linq;
    using Operators;

    public class ByteTextScanner : ITextScanner
    {
        private static readonly ITextComponentApproach[] Approaches =
        {
            new BaseTextComponentApproach(new[] {(byte) 'B', (byte) 'T'}, TextObjectComponentType.BeginText, new TextObjectComponentType[0]),
            new BaseTextComponentApproach(new[] {(byte) 'E', (byte) 'T'}, TextObjectComponentType.EndText, new TextObjectComponentType[0]),
            new BaseTextComponentApproach(new[] {(byte) 'T', (byte) 'f'}, TextObjectComponentType.TextFont, new []{ TextObjectComponentType.Font, TextObjectComponentType.Numeric }),
            new BaseTextComponentApproach(new[] {(byte) 'T', (byte) 'm'}, TextObjectComponentType.SetTextMatrix, new []
            {
                TextObjectComponentType.Numeric, TextObjectComponentType.Numeric, TextObjectComponentType.Numeric,
                TextObjectComponentType.Numeric, TextObjectComponentType.Numeric, TextObjectComponentType.Numeric
            }),
            new BaseTextComponentApproach(new[] {(byte) 'T', (byte) 'd'}, TextObjectComponentType.MoveTextPosition, new[]{ TextObjectComponentType.Numeric, TextObjectComponentType.Numeric }),
            new BaseTextComponentApproach(new[] {(byte) 'T', (byte) 'D'}, TextObjectComponentType.MoveTextPositionAndSetLeading, new[]{ TextObjectComponentType.Numeric, TextObjectComponentType.Numeric }),
            new BaseTextComponentApproach(new[] {(byte) 'T', (byte) 'j'}, TextObjectComponentType.ShowText, new[] { TextObjectComponentType.String }),
            new BaseTextComponentApproach(new[] {(byte) 'T', (byte) 'J'}, TextObjectComponentType.ShowTextWithIndividualGlyphPositioning, new[]{ TextObjectComponentType.Array }),
            new BaseTextComponentApproach(new[] {(byte) 'T', (byte) 'L'}, TextObjectComponentType.SetTextLeading, new []{ TextObjectComponentType.Numeric }),
            new BaseTextComponentApproach(new[] {(byte) 'T', (byte) 'r'}, TextObjectComponentType.SetTextRenderingMode, new[] { TextObjectComponentType.Numeric }),
            new BaseTextComponentApproach(new[] {(byte) 'T', (byte) 's'}, TextObjectComponentType.SetTextRise, new[] { TextObjectComponentType.Numeric }),
            new BaseTextComponentApproach(new[] {(byte) 'T', (byte) 'w'}, TextObjectComponentType.SetWordSpacing, new[] { TextObjectComponentType.Numeric }),
            new BaseTextComponentApproach(new[] {(byte) 'T', (byte) 'z'}, TextObjectComponentType.SetHorizontalTextScaling, new[] { TextObjectComponentType.Numeric }),
            new BaseTextComponentApproach(new[] {(byte) 'T', (byte) '*'}, TextObjectComponentType.MoveToNextLineStart, new TextObjectComponentType[0]),
            new BaseTextComponentApproach(new[] {(byte) 'T', (byte) 'c'}, TextObjectComponentType.SetCharacterSpacing, new[] { TextObjectComponentType.Numeric }),
            new BaseTextComponentApproach(new[] {(byte) 'g'}, TextObjectComponentType.SetGrayNonStroking, new [] { TextObjectComponentType.Numeric }),
            new BaseTextComponentApproach(new[] {(byte) 'G'}, TextObjectComponentType.SetGrayStroking, new [] { TextObjectComponentType.Numeric }),
            new BaseTextComponentApproach(new[] {(byte) 'w'}, TextObjectComponentType.SetLineWidth, new [] { TextObjectComponentType.Numeric }),
            new BaseTextComponentApproach(new[] {(byte) 'W'}, TextObjectComponentType.SetClippingPathNonZeroWinding, new [] { TextObjectComponentType.Numeric }),
            new BaseTextComponentApproach(new[] {(byte) 'W', (byte) '*'}, TextObjectComponentType.SetClippingPathEvenOdd, new [] { TextObjectComponentType.Numeric }),
            new BaseTextComponentApproach(new[] {(byte) '\''}, TextObjectComponentType.MoveNextLineAndShowText, new [] { TextObjectComponentType.String }),
            new FontTextComponentApproach(),
            new NumericTextComponentApproach(), 
            new StringTextComponentApproach()
        };

        private readonly byte[] bytes;

        private int offset;

        public ByteTextScanner(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public ITextObjectComponent CurrentComponent { get; private set; }

        public bool Read()
        {
            if (offset == bytes.Length - 1)
            {
                return false;
            }

            bool isReadingCandidate = false;
            int startOffset = -1;
            var validApproaches = new List<ITextComponentApproach>();
            var buffer = new List<byte>();

            while (offset < bytes.Length)
            {
                var current = bytes[offset];

                // Whitespace clears the current operator search.
                if (BaseTextComponentApproach.IsEmpty(current))
                {
                    // TODO: consider the case of two valid operators, one of which is a single character, 'Q' and 'Qe'. For example "BT 10 Q 13 Qe ET"

                    isReadingCandidate = false;

                    validApproaches.Clear();
                    buffer.Clear();

                    offset++;
                    continue;
                }

                buffer.Add(current);

                // If we previously started reading a byte which matched some possible approaches.
                if (isReadingCandidate)
                {
                    // Remove any approaches which are no longer valid for the next byte.
                    foreach (var validApproach in new List<ITextComponentApproach>(validApproaches))
                    {
                        if (!validApproach.CanRead(current, offset - startOffset))
                        {
                            validApproaches.Remove(validApproach);
                        }
                    }

                    // There is a single valid approach which is indicative of a specific operator.
                    if (validApproaches.Count == 1)
                    {
                        CurrentComponent = validApproaches[0].Read(buffer, bytes.Skip(offset + 1), out var localOffset);

                        if (CurrentComponent != null)
                        {
                            offset += localOffset;
                            return true;
                        }

                        isReadingCandidate = false;
                    }
                    // This was a false start.
                    else if (validApproaches.Count == 0)
                    {
                        buffer.Clear();
                        isReadingCandidate = false;
                    }
                }
                // If we haven't looked at the first byte after some whitespace.
                else if (buffer.Count == 1)
                {
                    // Find any operator approaches which are valid for this first byte.
                    foreach (var approach in Approaches)
                    {
                        if (approach.CanRead(current, 0))
                        {
                            validApproaches.Add(approach);
                        }
                    }

                    switch (validApproaches.Count)
                    {
                        case 0:
                            // No valid approaches, this cannot be a operator, continue until we hit a whitespace.
                            break;
                        case 1:
                            // A single valid approach, this immediately matches an operator.
                            CurrentComponent = validApproaches[0].Read(buffer, bytes.Skip(offset + 1), out var localOffset);

                            if (CurrentComponent != null)
                            {
                                offset += localOffset;
                                return true;
                            }
                            break;
                        default:
                            // Multiple valid approaches, use the next character to refine the possible approaches.
                            startOffset = offset;
                            isReadingCandidate = true;
                            break;
                    }
                }
                
                offset++;
            }

            return false;
        }
    }
}