namespace UglyToad.PdfPig.Filters
{
    using System;
    using System.IO;
    using IO;
    using Util;

    /// <summary>
    /// CCITT Modified Huffman RLE, Group 3 (T4) and Group 4 (T6) fax compression.
    ///
    /// Ported from https://github.com/apache/pdfbox/blob/e644c29279e276bde14ce7a33bdeef0cb1001b3e/pdfbox/src/main/java/org/apache/pdfbox/filter/CCITTFaxDecoderStream.java
    /// </summary>
    internal class CcittFaxDecoderStream : StreamWrapper
    {
        // See TIFF 6.0 Specification, Section 10: "Modified Huffman Compression", page 43.

        private readonly int columns;
        private readonly byte[] decodedRow;

        private readonly bool optionByteAligned;
        
        private readonly CcittFaxCompressionType type;

        private int decodedLength;
        private int decodedPos;

        private int[] changesReferenceRow;
        private int[] changesCurrentRow;
        private int changesReferenceRowCount;
        private int changesCurrentRowCount;

        private int lastChangingElement;

        private int buffer = -1;
        private int bufferPos = -1;

        /// <summary>
        /// Creates a CCITTFaxDecoderStream.
        /// This constructor may be used for CCITT streams embedded in PDF files,
        /// which use EncodedByteAlign.
        /// </summary>
        public CcittFaxDecoderStream(Stream stream, int columns, CcittFaxCompressionType type, bool byteAligned)
            : base(stream)
        {
            this.columns = columns;
            this.type = type;

            // We know this is only used for b/w (1 bit)
            decodedRow = new byte[(columns + 7) / 8];
            changesReferenceRow = new int[columns + 2];
            changesCurrentRow = new int[columns + 2];

            optionByteAligned = byteAligned;
        }

        private void Fetch()
        {
            if (decodedPos >= decodedLength)
            {
                decodedLength = 0;

                try
                {
                    DecodeRow();
                }
                catch (InvalidOperationException)
                {
                    if (decodedLength != 0)
                    {
                        throw;
                    }

                    // ..otherwise, just let client code try to read past the
                    // end of stream
                    decodedLength = -1;
                }

                decodedPos = 0;
            }
        }

        private void Decode1D()
        {
            var index = 0;
            var white = true;
            changesCurrentRowCount = 0;

            do
            {
                var completeRun = white ? DecodeRun(WhiteRunTree) : DecodeRun(BlackRunTree);
                index += completeRun;
                changesCurrentRow[changesCurrentRowCount++] = index;

                // Flip color for next run
                white = !white;
            } while (index < columns);
        }

        private void Decode2D()
        {
            changesReferenceRowCount = changesCurrentRowCount;
            var tmp = changesCurrentRow;
            changesCurrentRow = changesReferenceRow;
            changesReferenceRow = tmp;

            var white = true;
            var index = 0;
            changesCurrentRowCount = 0;

        mode: while (index < columns)
            {
                var node = CodeTree.Root;

                while (true)
                {
                    node = node.Walk(ReadBit());

                    if (node is null)
                    {
                        goto mode;
                    }
                    else if (node.IsLeaf)
                    {
                        switch (node.Value)
                        {
                            case VALUE_HMODE:
                                var runLength = DecodeRun(white ? WhiteRunTree : BlackRunTree);
                                index += runLength;
                                changesCurrentRow[changesCurrentRowCount++] = index;

                                runLength = DecodeRun(white ? BlackRunTree : WhiteRunTree);
                                index += runLength;
                                changesCurrentRow[changesCurrentRowCount++] = index;
                                break;

                            case VALUE_PASSMODE:
                                var pChangingElement = GetNextChangingElement(index, white) + 1;

                                if (pChangingElement >= changesReferenceRowCount)
                                {
                                    index = columns;
                                }
                                else
                                {
                                    index = changesReferenceRow[pChangingElement];
                                }

                                break;

                            default:
                                // Vertical mode (-3 to 3)
                                var vChangingElement = GetNextChangingElement(index, white);

                                if (vChangingElement >= changesReferenceRowCount || vChangingElement == -1)
                                {
                                    index = columns + node.Value;
                                }
                                else
                                {
                                    index = changesReferenceRow[vChangingElement] + node.Value;
                                }

                                changesCurrentRow[changesCurrentRowCount] = index;
                                changesCurrentRowCount++;
                                white = !white;

                                break;
                        }

                        goto mode;
                    }
                }
            }
        }

        private int GetNextChangingElement(int a0, bool white)
        {
            var start = (int)(lastChangingElement & 0xFFFF_FFFE) + (white ? 0 : 1);
            if (start > 2)
            {
                start -= 2;
            }

            if (a0 == 0)
            {
                return start;
            }

            for (var i = start; i < changesReferenceRowCount; i += 2)
            {
                if (a0 < changesReferenceRow[i])
                {
                    lastChangingElement = i;
                    return i;
                }
            }

            return -1;
        }

        private void DecodeRowType2()
        {
            if (optionByteAligned)
            {
                ResetBuffer();
            }

            Decode1D();
        }

        private void DecodeRowType4()
        {
            if (optionByteAligned)
            {
                ResetBuffer();
            }

        eof: while (true)
            {
                // read till next EOL code
                var node = EolOnlyTree.Root;

                while (true)
                {
                    node = node.Walk(ReadBit());

                    if (node is null)
                    {
                        goto eof;
                    }

                    if (node.IsLeaf)
                    {
                        goto done;
                    }
                }
            }

        done:
            if (type == CcittFaxCompressionType.Group3_1D || ReadBit())
            {
                Decode1D();
            }
            else
            {
                Decode2D();
            }
        }

        private void DecodeRowType6()
        {
            if (optionByteAligned)
            {
                ResetBuffer();
            }

            Decode2D();
        }

        private void DecodeRow()
        {
            switch (type)
            {
                case CcittFaxCompressionType.ModifiedHuffman:
                    DecodeRowType2();
                    break;
                case CcittFaxCompressionType.Group3_1D:
                case CcittFaxCompressionType.Group3_2D:
                    DecodeRowType4();
                    break;
                case CcittFaxCompressionType.Group4_2D:
                    DecodeRowType6();
                    break;
                default:
                    throw new InvalidOperationException(type + " is not a supported compression type.");
            }

            var index = 0;
            var white = true;

            lastChangingElement = 0;
            for (var i = 0; i <= changesCurrentRowCount; i++)
            {
                var nextChange = columns;

                if (i != changesCurrentRowCount)
                {
                    nextChange = changesCurrentRow[i];
                }

                if (nextChange > columns)
                {
                    nextChange = columns;
                }

                var byteIndex = index / 8;

                while (index % 8 != 0 && (nextChange - index) > 0)
                {
                    decodedRow[byteIndex] |= (byte)(white ? 0 : 1 << (7 - ((index) % 8)));
                    index++;
                }

                if (index % 8 == 0)
                {
                    byteIndex = index / 8;
                    var value = (byte)(white ? 0x00 : 0xff);

                    while ((nextChange - index) > 7)
                    {
                        decodedRow[byteIndex] = value;
                        index += 8;
                        ++byteIndex;
                    }
                }

                while ((nextChange - index) > 0)
                {
                    if (index % 8 == 0)
                    {
                        decodedRow[byteIndex] = 0;
                    }

                    decodedRow[byteIndex] |= (byte)(white ? 0 : 1 << (7 - ((index) % 8)));
                    index++;
                }

                white = !white;
            }

            if (index != columns)
            {
                throw new InvalidOperationException("Sum of run-lengths does not equal scan line width: " + index + " > " + columns);
            }

            decodedLength = (index + 7) / 8;
        }

        private int DecodeRun(Tree tree)
        {
            var total = 0;

            var node = tree.Root;

            while (true)
            {
                var bit = ReadBit();
                node = node.Walk(bit);

                if (node is null)
                {
                    throw new InvalidOperationException("Unknown code in Huffman RLE stream");
                }

                if (node.IsLeaf)
                {
                    total += node.Value;
                    if (node.Value >= 64)
                    {
                        node = tree.Root;
                    }
                    else if (node.Value >= 0)
                    {
                        return total;
                    }
                    else
                    {
                        return columns;
                    }
                }
            }
        }

        private void ResetBuffer()
        {
            bufferPos = -1;
        }

        private bool ReadBit()
        {
            if (bufferPos < 0 || bufferPos > 7)
            {
                buffer = Stream.ReadByte();

                if (buffer == -1)
                {
                    throw new InvalidOperationException("Unexpected end of Huffman RLE stream");
                }

                bufferPos = 0;
            }

            var isSet = ((buffer >> (7 - bufferPos)) & 1) == 1;

            bufferPos++;

            if (bufferPos > 7)
            {
                bufferPos = -1;
            }

            return isSet;
        }

        public override int ReadByte()
        {
            if (decodedLength < 0)
            {
                return 0x0;
            }

            if (decodedPos >= decodedLength)
            {
                Fetch();

                if (decodedLength < 0)
                {
                    return 0x0;
                }
            }

            return decodedRow[decodedPos++] & 0xff;
        }

        public override int Read(byte[] b, int off, int len)
        {
            if (decodedLength < 0)
            {
                ArrayHelper.Fill(b, off, off + len, (byte)0x0);
                return len;
            }

            if (decodedPos >= decodedLength)
            {
                Fetch();

                if (decodedLength < 0)
                {
                    ArrayHelper.Fill(b, off, off + len, (byte)0x0);
                    return len;
                }
            }

            var read = Math.Min(decodedLength - decodedPos, len);
            Array.Copy(decodedRow, decodedPos, b, off, read);
            decodedPos += read;

            return read;
        }

        private class Node
        {
            public Node Left { get; set; }
            public Node Right { get; set; }

            public int Value { get; set; }

            public bool CanBeFill { get; set; } 
            public bool IsLeaf { get; set; }

            public void Set(bool next, Node node)
            {
                if (!next)
                {
                    Left = node;
                }
                else
                {
                    Right = node;
                }
            }

            public Node Walk(bool next)
            {
                return next ? Right : Left;
            }

            public override string ToString()
            {
                return $"[{nameof(IsLeaf)}={IsLeaf}, {nameof(Value)}={Value}, {nameof(CanBeFill)}={CanBeFill}]";
            }
        }

        private class Tree
        {
            public Node Root { get; } = new Node();

            public void Fill(int depth, int path, int value)
            {
                var current = Root;

                for (var i = 0; i < depth; i++)
                {
                    var bitPos = depth - 1 - i;
                    var isSet = ((path >> bitPos) & 1) == 1;
                    var next = current.Walk(isSet);

                    if (next is null)
                    {
                        next = new Node();

                        if (i == depth - 1)
                        {
                            next.Value = value;
                            next.IsLeaf = true;
                        }

                        if (path == 0)
                        {
                            next.CanBeFill = true;
                        }

                        current.Set(isSet, next);
                    }
                    else if (next.IsLeaf)
                    {
                        throw new InvalidOperationException("node is leaf, no other following");
                    }

                    current = next;
                }
            }

            public void Fill(int depth, int path, Node node)
            {
                var current = Root;

                for (var i = 0; i < depth; i++)
                {
                    var bitPos = depth - 1 - i;
                    var isSet = ((path >> bitPos) & 1) == 1;
                    var next = current.Walk(isSet);

                    if (next is null)
                    {
                        if (i == depth - 1)
                        {
                            next = node;
                        }
                        else
                        {
                            next = new Node();
                        }

                        if (path == 0)
                        {
                            next.CanBeFill = true;
                        }

                        current.Set(isSet, next);
                    }
                    else if (next.IsLeaf)
                    {
                        throw new InvalidOperationException("node is leaf, no other following");
                    }

                    current = next;
                }
            }
        }

        private static readonly short[][] BLACK_CODES = new short[][] {
            new short[]{ // 2 bits
              0x2, 0x3,
              },
            new short[]{ // 3 bits
              0x2, 0x3,
              },
            new short[]{ // 4 bits
              0x2, 0x3,
              },
            new short[]{ // 5 bits
              0x3,
              },
            new short[]{ // 6 bits
              0x4, 0x5,
              },
            new short[]{ // 7 bits
              0x4, 0x5, 0x7,
              },
            new short[]{ // 8 bits
              0x4, 0x7,
              },
            new short[]{ // 9 bits
              0x18,
              },
            new short[]{ // 10 bits
              0x17, 0x18, 0x37, 0x8, 0xf,
              },
            new short[]{ // 11 bits
              0x17, 0x18, 0x28, 0x37, 0x67, 0x68, 0x6c, 0x8, 0xc, 0xd,
              },
            new short[]{ // 12 bits
              0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x1c, 0x1d, 0x1e, 0x1f, 0x24, 0x27, 0x28, 0x2b, 0x2c, 0x33,
              0x34, 0x35, 0x37, 0x38, 0x52, 0x53, 0x54, 0x55, 0x56, 0x57, 0x58, 0x59, 0x5a, 0x5b, 0x64, 0x65,
              0x66, 0x67, 0x68, 0x69, 0x6a, 0x6b, 0x6c, 0x6d, 0xc8, 0xc9, 0xca, 0xcb, 0xcc, 0xcd, 0xd2, 0xd3,
              0xd4, 0xd5, 0xd6, 0xd7, 0xda, 0xdb,
              },
            new short[]{ // 13 bits
              0x4a, 0x4b, 0x4c, 0x4d, 0x52, 0x53, 0x54, 0x55, 0x5a, 0x5b, 0x64, 0x65, 0x6c, 0x6d, 0x72, 0x73,
              0x74, 0x75, 0x76, 0x77,
              }
        };

        private static readonly short[][] BLACK_RUN_LENGTHS = new short[][]{
            new short[]{ // 2 bits
              3, 2,
              },
            new short[]{ // 3 bits
              1, 4,
              },
            new short[]{ // 4 bits
              6, 5,
              },
            new short[]{ // 5 bits
              7,
              },
            new short[]{ // 6 bits
              9, 8,
              },
            new short[]{ // 7 bits
              10, 11, 12,
              },
            new short[]{ // 8 bits
              13, 14,
              },
            new short[]{ // 9 bits
              15,
              },
            new short[]{ // 10 bits
              16, 17, 0, 18, 64,
              },
            new short[]{ // 11 bits
              24, 25, 23, 22, 19, 20, 21, 1792, 1856, 1920,
              },
            new short[]{ // 12 bits
              1984, 2048, 2112, 2176, 2240, 2304, 2368, 2432, 2496, 2560, 52, 55, 56, 59, 60, 320, 384, 448, 53,
              54, 50, 51, 44, 45, 46, 47, 57, 58, 61, 256, 48, 49, 62, 63, 30, 31, 32, 33, 40, 41, 128, 192, 26,
              27, 28, 29, 34, 35, 36, 37, 38, 39, 42, 43,
              },
            new short[]{ // 13 bits
              640, 704, 768, 832, 1280, 1344, 1408, 1472, 1536, 1600, 1664, 1728, 512, 576, 896, 960, 1024, 1088,
              1152, 1216,
              }
        };

        private static readonly short[][] WHITE_CODES = new short[][]{
            new short[]{ // 4 bits
              0x7, 0x8, 0xb, 0xc, 0xe, 0xf,
              },
            new short[]{ // 5 bits
              0x12, 0x13, 0x14, 0x1b, 0x7, 0x8,
              },
            new short[]{ // 6 bits
              0x17, 0x18, 0x2a, 0x2b, 0x3, 0x34, 0x35, 0x7, 0x8,
              },
            new short[]{ // 7 bits
              0x13, 0x17, 0x18, 0x24, 0x27, 0x28, 0x2b, 0x3, 0x37, 0x4, 0x8, 0xc,
              },
            new short[]{ // 8 bits
              0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x1a, 0x1b, 0x2, 0x24, 0x25, 0x28, 0x29, 0x2a, 0x2b, 0x2c, 0x2d,
              0x3, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x4, 0x4a, 0x4b, 0x5, 0x52, 0x53, 0x54, 0x55, 0x58, 0x59,
              0x5a, 0x5b, 0x64, 0x65, 0x67, 0x68, 0xa, 0xb,
              },
            new short[]{ // 9 bits
              0x98, 0x99, 0x9a, 0x9b, 0xcc, 0xcd, 0xd2, 0xd3, 0xd4, 0xd5, 0xd6, 0xd7, 0xd8, 0xd9, 0xda, 0xdb,
              },
            new short[]{ // 10 bits
            },
            new short[]{ // 11 bits
              0x8, 0xc, 0xd,
              },
            new short[]{ // 12 bits
              0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x1c, 0x1d, 0x1e, 0x1f,
              }
        };

        private static readonly short[][] WHITE_RUN_LENGTHS = new short[][]{
            new short[]{ // 4 bits
              2, 3, 4, 5, 6, 7,
              },
            new short[]{ // 5 bits
              128, 8, 9, 64, 10, 11,
              },
            new short[]{ // 6 bits
              192, 1664, 16, 17, 13, 14, 15, 1, 12,
              },
            new short[]{ // 7 bits
              26, 21, 28, 27, 18, 24, 25, 22, 256, 23, 20, 19,
              },
            new short[]{ // 8 bits
              33, 34, 35, 36, 37, 38, 31, 32, 29, 53, 54, 39, 40, 41, 42, 43, 44, 30, 61, 62, 63, 0, 320, 384, 45,
              59, 60, 46, 49, 50, 51, 52, 55, 56, 57, 58, 448, 512, 640, 576, 47, 48,
              },
            new short[]{ // 9 bits
              1472, 1536, 1600, 1728, 704, 768, 832, 896, 960, 1024, 1088, 1152, 1216, 1280, 1344, 1408,
              },
            new short[]{ // 10 bits
            },
            new short[]{ // 11 bits
              1792, 1856, 1920,
              },
            new short[]{ // 12 bits
              1984, 2048, 2112, 2176, 2240, 2304, 2368, 2432, 2496, 2560,
              }
        };

        private static readonly Node EOL;
        private static readonly Node FILL;
        private static readonly Tree BlackRunTree;
        private static readonly Tree WhiteRunTree;
        private static readonly Tree EolOnlyTree;
        private static readonly Tree CodeTree;

        const int VALUE_EOL = -2000;
        const int VALUE_FILL = -1000;
        const int VALUE_PASSMODE = -3000;
        const int VALUE_HMODE = -4000;

        static CcittFaxDecoderStream()
        {
            EOL = new Node
            {
                IsLeaf = true,
                Value = VALUE_EOL
            };
            FILL = new Node
            {
                Value = VALUE_FILL
            };
            FILL.Left = FILL;
            FILL.Right = EOL;

            EolOnlyTree = new Tree();
            EolOnlyTree.Fill(12, 0, FILL);
            EolOnlyTree.Fill(12, 1, EOL);

            BlackRunTree = new Tree();
            for (var i = 0; i < BLACK_CODES.Length; i++)
            {
                for (var j = 0; j < BLACK_CODES[i].Length; j++)
                {
                    BlackRunTree.Fill(i + 2, BLACK_CODES[i][j], BLACK_RUN_LENGTHS[i][j]);
                }
            }
            BlackRunTree.Fill(12, 0, FILL);
            BlackRunTree.Fill(12, 1, EOL);

            WhiteRunTree = new Tree();

            for (var i = 0; i < WHITE_CODES.Length; i++)
            {
                for (var j = 0; j < WHITE_CODES[i].Length; j++)
                {
                    WhiteRunTree.Fill(i + 4, WHITE_CODES[i][j], WHITE_RUN_LENGTHS[i][j]);
                }
            }

            WhiteRunTree.Fill(12, 0, FILL);
            WhiteRunTree.Fill(12, 1, EOL);

            CodeTree = new Tree();
            CodeTree.Fill(4, 1, VALUE_PASSMODE); // pass mode
            CodeTree.Fill(3, 1, VALUE_HMODE); // H mode
            CodeTree.Fill(1, 1, 0); // V(0)
            CodeTree.Fill(3, 3, 1); // V_R(1)
            CodeTree.Fill(6, 3, 2); // V_R(2)
            CodeTree.Fill(7, 3, 3); // V_R(3)
            CodeTree.Fill(3, 2, -1); // V_L(1)
            CodeTree.Fill(6, 2, -2); // V_L(2)
            CodeTree.Fill(7, 2, -3); // V_L(3)
        }
    }
}