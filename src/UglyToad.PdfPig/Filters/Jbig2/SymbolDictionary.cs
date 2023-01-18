namespace UglyToad.PdfPig.Filters.Jbig2
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;

    /// <summary>
    /// This class represents the data of segment type "Symbol dictionary". Parsing is described in
    /// 7.4.2.1.1 - 7.4.1.1.5 and decoding procedure is described in 6.5.
    /// </summary>
    internal class SymbolDictionary : IDictionary
    {
        private SubInputStream subInputStream;

        // Symbol dictionary flags, 7.4.2.1.1
        private short sdrTemplate;
        private byte sdTemplate;
        private bool isCodingContextRetained;
        private bool isCodingContextUsed;
        private short sdHuffAggInstanceSelection;
        private short sdHuffBMSizeSelection;
        private short sdHuffDecodeWidthSelection;
        private short sdHuffDecodeHeightSelection;
        private bool useRefinementAggregation;
        private bool isHuffmanEncoded;

        // Symbol dictionary AT flags, 7.4.2.1.2
        private short[] sdATX;
        private short[] sdATY;

        // Symbol dictionary refinement AT flags, 7.4.2.1.3
        private short[] sdrATX;
        private short[] sdrATY;

        // Number of exported symbols, 7.4.2.1.4
        private int amountOfExportSymbolss;

        // Number of new symbols, 7.4.2.1.5
        private int amountOfNewSymbols;

        // Further parameters
        private SegmentHeader segmentHeader;
        private int amountOfImportedSymbolss;
        private List<Bitmap> importSymbols;
        private int amountOfDecodedSymbols;
        private Bitmap[] newSymbols;

        // User-supplied tables
        private HuffmanTable dhTable;
        private HuffmanTable dwTable;
        private HuffmanTable bmSizeTable;
        private HuffmanTable aggInstTable;

        // Return value of that segment
        private List<Bitmap> exportSymbols;
        private List<Bitmap> sbSymbols;

        private ArithmeticDecoder arithmeticDecoder;
        private ArithmeticIntegerDecoder iDecoder;

        private TextRegion textRegion;
        private GenericRegion genericRegion;
        private GenericRefinementRegion genericRefinementRegion;
        private CX cx;

        private CX cxIADH;
        private CX cxIADW;
        private CX cxIAAI;
        private CX cxIAEX;
        private CX cxIARDX;
        private CX cxIARDY;
        private CX cxIADT;

        internal CX cxIAID;
        private int sbSymCodeLen;

        public SymbolDictionary()
        {
        }

        public SymbolDictionary(SubInputStream subInputStream, SegmentHeader segmentHeader)
        {
            this.subInputStream = subInputStream;
            this.segmentHeader = segmentHeader;
        }

        private void ParseHeader()
        {
            ReadRegionFlags();
            SetAtPixels();
            SetRefinementAtPixels();
            ReadAmountOfExportedSymbols();
            ReadAmountOfNewSymbols();
            SetInSyms();

            if (isCodingContextUsed)
            {
                SegmentHeader[] rtSegments = segmentHeader.GetRtSegments();

                for (int i = rtSegments.Length - 1; i >= 0; i--)
                {
                    if (rtSegments[i].SegmentType == 0)
                    {
                        SymbolDictionary symbolDictionary =
                            (SymbolDictionary)rtSegments[i].GetSegmentData();

                        if (symbolDictionary.isCodingContextRetained)
                        {
                            // 7.4.2.2 3)
                            SetRetainedCodingContexts(symbolDictionary);
                        }
                        break;
                    }
                }
            }

            CheckInput();
        }

        private void ReadRegionFlags()
        {
            // Bit 13-15
            subInputStream.ReadBits(3); // Dirty read... reserved bits must be 0

            // Bit 12
            sdrTemplate = (short)subInputStream.ReadBit();

            // Bit 10-11
            sdTemplate = (byte)(subInputStream.ReadBits(2) & 0xf);

            // Bit 9
            if (subInputStream.ReadBit() == 1)
            {
                isCodingContextRetained = true;
            }

            // Bit 8
            if (subInputStream.ReadBit() == 1)
            {
                isCodingContextUsed = true;
            }

            // Bit 7
            sdHuffAggInstanceSelection = (short)subInputStream.ReadBit();

            // Bit 6
            sdHuffBMSizeSelection = (short)subInputStream.ReadBit();

            // Bit 4-5
            sdHuffDecodeWidthSelection = (short)(subInputStream.ReadBits(2) & 0xf);

            // Bit 2-3
            sdHuffDecodeHeightSelection = (short)(subInputStream.ReadBits(2) & 0xf);

            // Bit 1
            if (subInputStream.ReadBit() == 1)
            {
                useRefinementAggregation = true;
            }

            // Bit 0
            if (subInputStream.ReadBit() == 1)
            {
                isHuffmanEncoded = true;
            }
        }

        private void SetAtPixels()
        {
            if (!isHuffmanEncoded)
            {
                if (sdTemplate == 0)
                {
                    ReadAtPixels(4);
                }
                else
                {
                    ReadAtPixels(1);
                }
            }
        }

        private void SetRefinementAtPixels()
        {
            if (useRefinementAggregation && sdrTemplate == 0)
            {
                ReadRefinementAtPixels(2);
            }
        }

        private void ReadAtPixels(int amountOfPixels)
        {
            sdATX = new short[amountOfPixels];
            sdATY = new short[amountOfPixels];

            for (int i = 0; i < amountOfPixels; i++)
            {
                sdATX[i] = (sbyte)subInputStream.ReadByte();
                sdATY[i] = (sbyte)subInputStream.ReadByte();
            }
        }

        private void ReadRefinementAtPixels(int amountOfAtPixels)
        {
            sdrATX = new short[amountOfAtPixels];
            sdrATY = new short[amountOfAtPixels];

            for (int i = 0; i < amountOfAtPixels; i++)
            {
                sdrATX[i] = (sbyte)subInputStream.ReadByte();
                sdrATY[i] = (sbyte)subInputStream.ReadByte();
            }
        }

        private void ReadAmountOfExportedSymbols()
        {
            amountOfExportSymbolss = (int)subInputStream.ReadBits(32);
        }

        private void ReadAmountOfNewSymbols()
        {
            amountOfNewSymbols = (int)subInputStream.ReadBits(32);
        }

        private void SetInSyms()
        {
            if (segmentHeader.GetRtSegments() != null)
            {
                RetrieveImportSymbols();
            }
            else
            {
                importSymbols = new List<Bitmap>();
            }
        }

        private void SetRetainedCodingContexts(SymbolDictionary sd)
        {
            arithmeticDecoder = sd.arithmeticDecoder;
            isHuffmanEncoded = sd.isHuffmanEncoded;
            useRefinementAggregation = sd.useRefinementAggregation;
            sdTemplate = sd.sdTemplate;
            sdrTemplate = sd.sdrTemplate;
            sdATX = sd.sdATX;
            sdATY = sd.sdATY;
            sdrATX = sd.sdrATX;
            sdrATY = sd.sdrATY;
            cx = sd.cx;
        }

        private void CheckInput()
        {
            if (isHuffmanEncoded)
            {
                if (sdTemplate != 0)
                {
                    sdTemplate = 0;
                }

                if (!useRefinementAggregation)
                {
                    if (isCodingContextRetained)
                    {
                        isCodingContextRetained = false;
                    }

                    if (isCodingContextUsed)
                    {
                        isCodingContextUsed = false;
                    }
                }
            }
            else
            {
                if (sdHuffBMSizeSelection != 0)
                {
                    sdHuffBMSizeSelection = 0;
                }

                if (sdHuffDecodeWidthSelection != 0)
                {
                    sdHuffDecodeWidthSelection = 0;
                }

                if (sdHuffDecodeHeightSelection != 0)
                {
                    sdHuffDecodeHeightSelection = 0;
                }
            }

            if (!useRefinementAggregation)
            {
                if (sdrTemplate != 0)
                {
                    sdrTemplate = 0;
                }
            }

            if (!isHuffmanEncoded || !useRefinementAggregation)
            {
                if (sdHuffAggInstanceSelection != 0)
                {
                    sdHuffAggInstanceSelection = 0;
                }
            }
        }

        /// <summary>
        /// 6.5.5 Decoding the symbol dictionary.
        /// </summary>
        /// <returns>List of decoded symbol bitmaps.</returns>
        public List<Bitmap> GetDictionary()
        {
            if (null == exportSymbols)
            {
                if (useRefinementAggregation)
                {
                    sbSymCodeLen = GetSbSymCodeLen();
                }

                if (!isHuffmanEncoded)
                {
                    SetCodingStatistics();
                }

                // 6.5.5 1)
                newSymbols = new Bitmap[amountOfNewSymbols];

                // 6.5.5 2)
                int[] newSymbolsWidths = null;
                if (isHuffmanEncoded && !useRefinementAggregation)
                {
                    newSymbolsWidths = new int[amountOfNewSymbols];
                }

                SetSymbolsArray();

                // 6.5.5 3)
                int heightClassHeight = 0;
                amountOfDecodedSymbols = 0;

                // 6.5.5 4 a)
                while (amountOfDecodedSymbols < amountOfNewSymbols)
                {
                    // 6.5.5 4 b)
                    heightClassHeight += (int)DecodeHeightClassDeltaHeight();
                    int symbolWidth = 0;
                    int totalWidth = 0;
                    int heightClassFirstSymbolIndex = amountOfDecodedSymbols;

                    // 6.5.5 4 c)

                    // Repeat until OOB - OOB sends a break;
                    while (true)
                    {
                        // 4 c) i)
                        long differenceWidth = DecodeDifferenceWidth();

                        // If result is OOB (out-of-band), then all the symbols in this height
                        // class has been decoded; proceed to step 4 d). Also exit, if the
                        // expected number of symbols have been decoded.
                        // The latter exit condition guards against pathological cases where
                        // a symbol's DW never contains OOB and thus never terminates.
                        if (differenceWidth == long.MaxValue
                                || amountOfDecodedSymbols >= amountOfNewSymbols)
                        {
                            break;
                        }

                        symbolWidth += (int)differenceWidth;
                        totalWidth += symbolWidth;

                        // 4 c) ii)
                        if (!isHuffmanEncoded || useRefinementAggregation)
                        {
                            if (!useRefinementAggregation)
                            {
                                // 6.5.8.1 - Direct coded
                                DecodeDirectlyThroughGenericRegion(symbolWidth, heightClassHeight);
                            }
                            else
                            {
                                // 6.5.8.2 - Refinement/Aggregate-coded
                                DecodeAggregate(symbolWidth, heightClassHeight);
                            }
                        }
                        else if (isHuffmanEncoded && !useRefinementAggregation)
                        {
                            // 4 c) iii)
                            newSymbolsWidths[amountOfDecodedSymbols] = symbolWidth;
                        }
                        amountOfDecodedSymbols++;
                    }

                    // 6.5.5 4 d)
                    if (isHuffmanEncoded && !useRefinementAggregation)
                    {
                        // 6.5.9
                        long bmSize;
                        if (sdHuffBMSizeSelection == 0)
                        {
                            bmSize = StandardTables.getTable(1).Decode(subInputStream);
                        }
                        else
                        {
                            bmSize = HuffDecodeBmSize();
                        }

                        subInputStream.SkipBits();

                        Bitmap heightClassCollectiveBitmap =
                            DecodeHeightClassCollectiveBitmap(bmSize, heightClassHeight, totalWidth);

                        subInputStream.SkipBits();
                        DecodeHeightClassBitmap(heightClassCollectiveBitmap,
                            heightClassFirstSymbolIndex, heightClassHeight, newSymbolsWidths);
                    }
                }

                // 5)
                // 6.5.10 1) - 5)

                int[] exFlags = GetToExportFlags();

                // 6.5.10 6) - 8)
                SetExportedSymbols(exFlags);
            }

            return exportSymbols;
        }

        private void SetCodingStatistics()
        {
            if (cxIADT == null)
            {
                cxIADT = new CX(512, 1);
            }

            if (cxIADH == null)
            {
                cxIADH = new CX(512, 1);
            }

            if (cxIADW == null)
            {
                cxIADW = new CX(512, 1);
            }

            if (cxIAAI == null)
            {
                cxIAAI = new CX(512, 1);
            }

            if (cxIAEX == null)
            {
                cxIAEX = new CX(512, 1);
            }

            if (useRefinementAggregation && cxIAID == null)
            {
                cxIAID = new CX(1 << sbSymCodeLen, 1);
                cxIARDX = new CX(512, 1);
                cxIARDY = new CX(512, 1);
            }

            if (cx == null)
            {
                cx = new CX(65536, 1);
            }

            if (arithmeticDecoder == null)
            {
                arithmeticDecoder = new ArithmeticDecoder(subInputStream);
            }

            if (iDecoder == null)
            {
                iDecoder = new ArithmeticIntegerDecoder(arithmeticDecoder);
            }
        }

        private void DecodeHeightClassBitmap(Bitmap heightClassCollectiveBitmap,
                int heightClassFirstSymbol, int heightClassHeight,
                int[] newSymbolsWidths)
        {
            for (int i = heightClassFirstSymbol; i < amountOfDecodedSymbols; i++)
            {
                int startColumn = 0;

                for (int j = heightClassFirstSymbol; j <= i - 1; j++)
                {
                    startColumn += newSymbolsWidths[j];
                }

                var roi = new Rectangle(startColumn, 0, newSymbolsWidths[i], heightClassHeight);
                var symbolBitmap = Bitmaps.Extract(roi, heightClassCollectiveBitmap);
                newSymbols[i] = symbolBitmap;
                sbSymbols.Add(symbolBitmap);
            }
        }

        private void DecodeAggregate(int symbolWidth, int heightClassHeight)
        {
            // 6.5.8.2 1)
            // 6.5.8.2.1 - Number of symbol instances in aggregation
            long amountOfRefinementAggregationInstances;
            if (isHuffmanEncoded)
            {
                amountOfRefinementAggregationInstances = HuffDecodeRefAggNInst();
            }
            else
            {
                amountOfRefinementAggregationInstances = iDecoder.Decode(cxIAAI);
            }

            if (amountOfRefinementAggregationInstances > 1)
            {
                // 6.5.8.2 2)
                DecodeThroughTextRegion(symbolWidth, heightClassHeight,
                        amountOfRefinementAggregationInstances);
            }
            else if (amountOfRefinementAggregationInstances == 1)
            {
                // 6.5.8.2 3) refers to 6.5.8.2.2
                DecodeRefinedSymbol(symbolWidth, heightClassHeight);
            }
        }

        private long HuffDecodeRefAggNInst()
        {
            if (sdHuffAggInstanceSelection == 0)
            {
                return StandardTables.getTable(1).Decode(subInputStream);
            }
            else if (sdHuffAggInstanceSelection == 1)
            {
                if (aggInstTable == null)
                {
                    int aggregationInstanceNumber = 0;

                    if (sdHuffDecodeHeightSelection == 3)
                    {
                        aggregationInstanceNumber++;
                    }
                    if (sdHuffDecodeWidthSelection == 3)
                    {
                        aggregationInstanceNumber++;
                    }
                    if (sdHuffBMSizeSelection == 3)
                    {
                        aggregationInstanceNumber++;
                    }

                    aggInstTable = GetUserTable(aggregationInstanceNumber);
                }
                return aggInstTable.Decode(subInputStream);
            }
            return 0;
        }

        private void DecodeThroughTextRegion(int symbolWidth, int heightClassHeight,
                long amountOfRefinementAggregationInstances)
        {
            if (textRegion == null)
            {
                textRegion = new TextRegion(subInputStream, null);

                textRegion.SetContexts(cx, // default context
                        new CX(512, 1), // IADT
                        new CX(512, 1), // IAFS
                        new CX(512, 1), // IADS
                        new CX(512, 1), // IAIT
                        cxIAID, // IAID
                        new CX(512, 1), // IARDW
                        new CX(512, 1), // IARDH
                        new CX(512, 1), // IARDX
                        new CX(512, 1) // IARDY
                );
            }

            // 6.5.8.2.4 Concatenating the array used as parameter later.
            SetSymbolsArray();

            // 6.5.8.2 2) Parameters set according to Table 17, page 36
            textRegion.SetParameters(arithmeticDecoder, iDecoder, isHuffmanEncoded, true, symbolWidth,
                    heightClassHeight, amountOfRefinementAggregationInstances, 1,
                    (amountOfImportedSymbolss + amountOfDecodedSymbols), 0, 0, 0, 1, 0, 0, 0,
                    0, 0, 0, 0, 0, 0, sdrTemplate, sdrATX, sdrATY, sbSymbols, sbSymCodeLen);

            AddSymbol(textRegion);
        }

        private void DecodeRefinedSymbol(int symbolWidth, int heightClassHeight)
        {
            int id;
            int rdx;
            int rdy;
            if (isHuffmanEncoded)
            {
                // 2) - 4)
                id = (int)subInputStream.ReadBits(sbSymCodeLen);
                rdx = (int)StandardTables.getTable(15).Decode(subInputStream);
                rdy = (int)StandardTables.getTable(15).Decode(subInputStream);

                // 5) a)
                /* symInRefSize = */
                StandardTables.getTable(1).Decode(subInputStream);

                // 5) b) - Skip over remaining bits
                subInputStream.SkipBits();
            }
            else
            {
                // 2) - 4)
                id = iDecoder.DecodeIAID(cxIAID, sbSymCodeLen);
                rdx = (int)iDecoder.Decode(cxIARDX);
                rdy = (int)iDecoder.Decode(cxIARDY);
            }

            // 6)
            SetSymbolsArray();
            Bitmap ibo = sbSymbols[id];
            DecodeNewSymbols(symbolWidth, heightClassHeight, ibo, rdx, rdy);

            // 7)
            if (isHuffmanEncoded)
            {
                subInputStream.SkipBits();
                // Make sure that the processed bytes are equal to the value read in step 5 a)
            }
        }

        private void DecodeNewSymbols(int symWidth, int hcHeight, Bitmap ibo, int rdx, int rdy)
        {
            if (genericRefinementRegion == null)
            {
                genericRefinementRegion = new GenericRefinementRegion(subInputStream);

                if (arithmeticDecoder == null)
                {
                    arithmeticDecoder = new ArithmeticDecoder(subInputStream);
                }

                if (cx == null)
                {
                    cx = new CX(65536, 1);
                }
            }

            // Parameters as shown in Table 18, page 36
            genericRefinementRegion.SetParameters(cx, arithmeticDecoder, sdrTemplate, symWidth,
                    hcHeight, ibo, rdx, rdy, false, sdrATX, sdrATY);

            AddSymbol(genericRefinementRegion);
        }

        private void DecodeDirectlyThroughGenericRegion(int symWidth, int hcHeight)
        {
            if (genericRegion == null)
            {
                genericRegion = new GenericRegion(subInputStream);
            }

            // Parameters set according to Table 16, page 35
            genericRegion.SetParameters(false, sdTemplate, false, false, sdATX, sdATY, symWidth,
                    hcHeight, cx, arithmeticDecoder);

            AddSymbol(genericRegion);
        }

        private void AddSymbol(IRegion region)
        {
            Bitmap symbol = region.GetRegionBitmap();
            newSymbols[amountOfDecodedSymbols] = symbol;
            sbSymbols.Add(symbol);
        }

        private long DecodeDifferenceWidth()
        {
            if (isHuffmanEncoded)
            {
                switch (sdHuffDecodeWidthSelection)
                {
                    case 0:
                        return StandardTables.getTable(2).Decode(subInputStream);

                    case 1:
                        return StandardTables.getTable(3).Decode(subInputStream);

                    case 3:
                        if (dwTable == null)
                        {
                            int dwNr = 0;

                            if (sdHuffDecodeHeightSelection == 3)
                            {
                                dwNr++;
                            }
                            dwTable = GetUserTable(dwNr);
                        }

                        return dwTable.Decode(subInputStream);
                }
            }
            else
            {
                return iDecoder.Decode(cxIADW);
            }
            return 0;
        }

        private long DecodeHeightClassDeltaHeight()
        {
            if (isHuffmanEncoded)
            {
                return DecodeHeightClassDeltaHeightWithHuffman();
            }
            else
            {
                return iDecoder.Decode(cxIADH);
            }
        }


        /// <summary>
        /// 6.5.6 if isHuffmanEncoded,
        /// </summary>
        /// <returns>Result of decoding HCDH</returns>
        private long DecodeHeightClassDeltaHeightWithHuffman()
        {
            switch (sdHuffDecodeHeightSelection)
            {
                case 0:
                    return StandardTables.getTable(4).Decode(subInputStream);

                case 1:
                    return StandardTables.getTable(5).Decode(subInputStream);

                case 3:
                    if (dhTable == null)
                    {
                        dhTable = GetUserTable(0);
                    }
                    return dhTable.Decode(subInputStream);
            }

            return 0;
        }

        private Bitmap DecodeHeightClassCollectiveBitmap(long bmSize,
                int heightClassHeight, int totalWidth)
        {
            if (bmSize == 0)
            {
                Bitmap heightClassCollectiveBitmap = new Bitmap(totalWidth, heightClassHeight);

                for (int i = 0; i < heightClassCollectiveBitmap.GetByteArray().Length; i++)
                {
                    heightClassCollectiveBitmap.SetByte(i, subInputStream.ReadByte());
                }

                return heightClassCollectiveBitmap;
            }
            else
            {
                if (genericRegion == null)
                {
                    genericRegion = new GenericRegion(subInputStream);
                }

                genericRegion.SetParameters(true, subInputStream.Position, bmSize,
                        heightClassHeight, totalWidth);

                return genericRegion.GetRegionBitmap();
            }
        }

        private void SetExportedSymbols(int[] toExportFlags)
        {
            exportSymbols = new List<Bitmap>(amountOfExportSymbolss);

            for (int i = 0; i < amountOfImportedSymbolss + amountOfNewSymbols; i++)
            {
                if (toExportFlags[i] == 1)
                {
                    if (i < amountOfImportedSymbolss)
                    {
                        exportSymbols.Add(importSymbols[i]);
                    }
                    else
                    {
                        exportSymbols.Add(newSymbols[i - amountOfImportedSymbolss]);
                    }
                }
            }
        }

        private int[] GetToExportFlags()
        {
            int currentExportFlag = 0;
            int[] exportFlags = new int[amountOfImportedSymbolss + amountOfNewSymbols];

            long exRunLength;
            for (int exportIndex = 0; exportIndex < amountOfImportedSymbolss
                    + amountOfNewSymbols; exportIndex += (int)exRunLength)
            {
                if (isHuffmanEncoded)
                {
                    exRunLength = StandardTables.getTable(1).Decode(subInputStream);
                }
                else
                {
                    exRunLength = iDecoder.Decode(cxIAEX);
                }

                if (exRunLength != 0)
                {
                    for (int index = exportIndex; index < exportIndex + exRunLength; index++)
                    {
                        exportFlags[index] = currentExportFlag;
                    }
                }

                currentExportFlag = (currentExportFlag == 0) ? 1 : 0;
            }

            return exportFlags;
        }

        private long HuffDecodeBmSize()
        {
            if (bmSizeTable == null)
            {
                int bmNr = 0;

                if (sdHuffDecodeHeightSelection == 3)
                {
                    bmNr++;
                }

                if (sdHuffDecodeWidthSelection == 3)
                {
                    bmNr++;
                }

                bmSizeTable = GetUserTable(bmNr);
            }
            return bmSizeTable.Decode(subInputStream);
        }

        /// <summary>
        /// 6.5.8.2.3 - Setting SBSYMCODES and SBSYMCODELEN
        /// </summary>
        /// <returns>Result of computing SBSYMCODELEN</returns>
        private int GetSbSymCodeLen()
        {
            if (isHuffmanEncoded)
            {
                return Math.Max(
                        (int)(Math.Ceiling(
                                Math.Log(amountOfImportedSymbolss + amountOfNewSymbols) / Math.Log(2))),
                        1);
            }
            else
            {
                return (int)(Math
                        .Ceiling(Math.Log(amountOfImportedSymbolss + amountOfNewSymbols) / Math.Log(2)));
            }
        }


        /// <summary>
        /// 6.5.8.2.4 - Setting SBSYMS
        /// </summary>
        private void SetSymbolsArray()
        {
            if (importSymbols == null)
            {
                RetrieveImportSymbols();
            }

            if (sbSymbols == null)
            {
                sbSymbols = new List<Bitmap>();
                sbSymbols.AddRange(importSymbols);
            }
        }

        /// <summary>
        /// Concatenates symbols from all referred-to segments.
        /// </summary>
        private void RetrieveImportSymbols()
        {
            importSymbols = new List<Bitmap>();
            foreach (SegmentHeader referredToSegmentHeader in segmentHeader.GetRtSegments())
            {
                if (referredToSegmentHeader.SegmentType == 0)
                {
                    SymbolDictionary sd = (SymbolDictionary)referredToSegmentHeader
                            .GetSegmentData();
                    importSymbols.AddRange(sd.GetDictionary());
                    amountOfImportedSymbolss += sd.amountOfExportSymbolss;
                }
            }
        }

        private HuffmanTable GetUserTable(int tablePosition)
        {
            int tableCounter = 0;

            foreach (SegmentHeader referredToSegmentHeader in segmentHeader.GetRtSegments())
            {
                if (referredToSegmentHeader.SegmentType == 53)
                {
                    if (tableCounter == tablePosition)
                    {
                        Table t = (Table)referredToSegmentHeader.GetSegmentData();
                        return new EncodedTable(t);
                    }
                    else
                    {
                        tableCounter++;
                    }
                }
            }
            return null;
        }

        public void Init(SegmentHeader header, SubInputStream sis)
        {
            subInputStream = sis;
            segmentHeader = header;
            ParseHeader();
        }
    }
}
