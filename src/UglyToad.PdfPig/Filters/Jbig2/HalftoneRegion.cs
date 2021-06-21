namespace UglyToad.PdfPig.Filters.Jbig2
{
    using System;
    using System.Collections.Generic;
    using UglyToad.PdfPig.Util;

    /// <summary>
    /// This class represents the data of segment type "Halftone region". Parsing is described in 7.4.5, page 67. Decoding
    /// procedure in 6.6.5 and 7.4.5.2.
    /// </summary>
    internal class HalftoneRegion : IRegion
    {
        private SubInputStream subInputStream;
        private SegmentHeader segmentHeader;
        private long dataHeaderOffset = 0;
        private long dataHeaderLength;
        private long dataOffset;
        private long dataLength;

        // Decoded data
        private Bitmap halftoneRegionBitmap;

        // Previously decoded data from other regions or dictionaries, stored to use as patterns in this region.
        private List<Bitmap> patterns;

        // Region segment information field, 7.4.1
        public RegionSegmentInformation RegionInfo { get; private set; }

        // Halftone segment information field, 7.4.5.1.1
        public byte HDefaultPixel { get; private set; }
        public CombinationOperator HCombinationOperator { get; private set; }
        public bool HSkipEnabled { get; private set; }
        public byte HTemplate { get; private set; }
        public bool IsMMREncoded { get; private set; }

        // Halftone grid position and size, 7.4.5.1.2
        // Width of the gray-scale image, 7.4.5.1.2.1
        public int HGridWidth { get; private set; }
        // Height of the gray-scale image, 7.4.5.1.2.2
        public int HGridHeight { get; private set; }
        // Horizontal offset of the grid, 7.4.5.1.2.3
        public int HGridX { get; private set; }
        // Vertical offset of the grid, 7.4.5.1.2.4
        public int HGridY { get; private set; }

        // Halftone grid vector, 7.4.5.1.3
        // Horizontal coordinate of the halftone grid vector, 7.4.5.1.3.1
        public int HRegionX { get; private set; }
        // Vertical coordinate of the halftone grod vector, 7.4.5.1.3.2
        public int HRegionY { get; private set; }

        public HalftoneRegion()
        {
        }

        public HalftoneRegion(SubInputStream subInputStream)
        {
            this.subInputStream = subInputStream;
            this.RegionInfo = new RegionSegmentInformation(subInputStream);
        }

        public HalftoneRegion(SubInputStream subInputStream, SegmentHeader segmentHeader)
        {
            this.subInputStream = subInputStream;
            this.segmentHeader = segmentHeader;
            this.RegionInfo = new RegionSegmentInformation(subInputStream);
        }

        private void ParseHeader()
        {
            RegionInfo.ParseHeader();

            // Bit 7
            HDefaultPixel = (byte)subInputStream.ReadBit();

            // Bit 4-6
            HCombinationOperator = CombinationOperators
                    .TranslateOperatorCodeToEnum((short)(subInputStream.ReadBits(3) & 0xf));

            // Bit 3
            if (subInputStream.ReadBit() == 1)
            {
                HSkipEnabled = true;
            }

            // Bit 1-2
            HTemplate = (byte)(subInputStream.ReadBits(2) & 0xf);

            // Bit 0
            if (subInputStream.ReadBit() == 1)
            {
                IsMMREncoded = true;
            }

            HGridWidth = (int)(subInputStream.ReadBits(32) & 0xffffffff);
            HGridHeight = (int)(subInputStream.ReadBits(32) & 0xffffffff);

            HGridX = (int)subInputStream.ReadBits(32);
            HGridY = (int)subInputStream.ReadBits(32);

            HRegionX = (int)subInputStream.ReadBits(16) & 0xffff;
            HRegionY = (int)subInputStream.ReadBits(16) & 0xffff;

            ComputeSegmentDataStructure();
        }

        private void ComputeSegmentDataStructure()
        {
            dataOffset = subInputStream.Position;
            dataHeaderLength = dataOffset - dataHeaderOffset;
            dataLength = subInputStream.Length - dataHeaderLength;
        }

        /// <summary>
        /// The procedure is described in JBIG2 ISO standard, 6.6.5.
        /// </summary>
        /// <returns>The decoded <see cref="Bitmap"/></returns>
        public Bitmap GetRegionBitmap()
        {
            if (null == halftoneRegionBitmap)
            {
                // 6.6.5, page 40
                // 1)
                halftoneRegionBitmap = new Bitmap(RegionInfo.BitmapWidth,
                        RegionInfo.BitmapHeight);

                if (patterns == null)
                {
                    patterns = GetPatterns();
                }

                if (HDefaultPixel == 1)
                {
                    ArrayHelper.Fill(halftoneRegionBitmap.GetByteArray(), (byte)0xff);
                }

                // 2)
                // 6.6.5.1 Computing hSkip - At the moment SKIP is not used... we are not able to test it.
                // Bitmap hSkip;
                // if (hSkipEnabled) {
                // int hPatternHeight = (int) hPats.get(0).getHeight();
                // int hPatternWidth = (int) hPats.get(0).getWidth();
                // Implementation could be achieved like this: Set or get pattern width and height from
                // referred pattern segments. The method is called like this:
                // hSkip = computeHSkip(hPatternHeight, hPatternWidth);
                // }

                // 3)
                int bitsPerValue = (int)Math.Ceiling(Math.Log(patterns.Count) / Math.Log(2));

                // 4)
                int[][] grayScaleValues = GrayScaleDecoding(bitsPerValue);

                // 5), rendering the pattern, described in 6.6.5.2
                RenderPattern(grayScaleValues);
            }
            // 6)
            return halftoneRegionBitmap;
        }

        /// <summary>
        /// This method draws the pattern into the region bitmap({ @code htReg}), as described in 6.6.5.2, page 42
        /// </summary>
        private void RenderPattern(int[][] grayScaleValues)
        {
            int x, y;

            // 1)
            for (int m = 0; m < HGridHeight; m++)
            {
                // a)
                for (int n = 0; n < HGridWidth; n++)
                {
                    // i)
                    x = ComputeX(m, n);
                    y = ComputeY(m, n);

                    // ii)
                    Bitmap patternBitmap = patterns[grayScaleValues[m][n]];
                    Bitmaps.Blit(patternBitmap, halftoneRegionBitmap, (x + HGridX), (y + HGridY),
                            HCombinationOperator);
                }
            }
        }

        private List<Bitmap> GetPatterns()
        {
            var patterns = new List<Bitmap>();

            foreach (SegmentHeader s in segmentHeader.GetRtSegments())
            {
                PatternDictionary patternDictionary = (PatternDictionary)s.GetSegmentData();
                patterns.AddRange(patternDictionary.GetDictionary());
            }

            return patterns;
        }


        /// <summary>
        /// Gray-scale image decoding procedure is special for halftone region decoding
        /// and is described in Annex C.5 on page 98.
        /// </summary>
        private int[][] GrayScaleDecoding(int bitsPerValue)
        {
            short[] gbAtX = null;
            short[] gbAtY = null;

            if (!IsMMREncoded)
            {
                gbAtX = new short[4];
                gbAtY = new short[4];
                // Set AT pixel values
                if (HTemplate <= 1)
                {
                    gbAtX[0] = 3;
                }
                else if (HTemplate >= 2)
                {
                    gbAtX[0] = 2;
                }

                gbAtY[0] = -1;
                gbAtX[1] = -3;
                gbAtY[1] = -1;
                gbAtX[2] = 2;
                gbAtY[2] = -2;
                gbAtX[3] = -2;
                gbAtY[3] = -2;
            }

            var grayScalePlanes = new Bitmap[bitsPerValue];

            // 1)
            var genericRegion = new GenericRegion(subInputStream);
            genericRegion.SetParameters(IsMMREncoded, dataOffset, dataLength, HGridHeight, HGridWidth,
                    HTemplate, false, HSkipEnabled, gbAtX, gbAtY);

            // 2)
            int j = bitsPerValue - 1;

            grayScalePlanes[j] = genericRegion.GetRegionBitmap();

            while (j > 0)
            {
                j--;
                genericRegion.ResetBitmap();
                // 3) a)
                grayScalePlanes[j] = genericRegion.GetRegionBitmap();
                // 3) b)
                grayScalePlanes = CombineGrayScalePlanes(grayScalePlanes, j);
            }

            // 4)
            return ComputeGrayScaleValues(grayScalePlanes, bitsPerValue);
        }

        private Bitmap[] CombineGrayScalePlanes(Bitmap[] grayScalePlanes, int j)
        {
            int byteIndex = 0;
            for (int y = 0; y < grayScalePlanes[j].Height; y++)
            {

                for (int x = 0; x < grayScalePlanes[j].Width; x += 8)
                {
                    byte newValue = grayScalePlanes[j + 1].GetByte(byteIndex);
                    byte oldValue = grayScalePlanes[j].GetByte(byteIndex);

                    grayScalePlanes[j].SetByte(byteIndex++,
                            Bitmaps.CombineBytes(oldValue, newValue, CombinationOperator.XOR));
                }
            }
            return grayScalePlanes;
        }

        private int[][] ComputeGrayScaleValues(Bitmap[] grayScalePlanes, int bitsPerValue)
        {
            // Gray-scale decoding procedure, page 98
            int[][] grayScaleValues = new int[HGridHeight][];
            for (int i = 0; i < grayScaleValues.Length; i++)
            {
                grayScaleValues[i] = new int[HGridWidth];
            }

            // 4)
            for (int y = 0; y < HGridHeight; y++)
            {
                for (int x = 0; x < HGridWidth; x += 8)
                {
                    int minorWidth = HGridWidth - x > 8 ? 8 : HGridWidth - x;
                    int byteIndex = grayScalePlanes[0].GetByteIndex(x, y);

                    for (int minorX = 0; minorX < minorWidth; minorX++)
                    {
                        int i = minorX + x;
                        grayScaleValues[y][i] = 0;

                        for (int j = 0; j < bitsPerValue; j++)
                        {
                            grayScaleValues[y][i] += ((grayScalePlanes[j]
                                    .GetByte(byteIndex) >> (7 - i & 7)) & 1) * (1 << j);
                        }
                    }
                }
            }
            return grayScaleValues;
        }

        private int ComputeX(int m, int n)
        {
            return ShiftAndFill((HGridX + m * HRegionY + n * HRegionX));
        }

        private int ComputeY(int m, int n)
        {
            return ShiftAndFill((HGridY + m * HRegionX - n * HRegionY));
        }

        private int ShiftAndFill(int value)
        {
            // shift value by 8 and let the leftmost 8 bits be 0
            value >>= 8;

            if (value < 0)
            {
                // fill the leftmost 8 bits with 1
                int bitPosition = (int)(Math.Log(value.HighestOneBit()) / Math.Log(2));

                for (int i = 1; i < 31 - bitPosition; i++)
                {
                    // bit flip
                    value |= 1 << (31 - i);
                }
            }

            return value;
        }

        public void Init(SegmentHeader header, SubInputStream sis)
        {
            segmentHeader = header;
            subInputStream = sis;
            RegionInfo = new RegionSegmentInformation(subInputStream);
            ParseHeader();
        }
    }
}
