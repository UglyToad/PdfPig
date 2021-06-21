namespace UglyToad.PdfPig.Filters.Jbig2
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    /// <summary>
    /// The basic class for all JBIG2 segments.
    /// </summary>
    internal class SegmentHeader
    {
        private static readonly Dictionary<int, Type> SEGMENT_TYPE_MAP = new Dictionary<int, Type>
        {
            { 0, typeof(SymbolDictionary) }, { 4, typeof(TextRegion) },
            { 6, typeof(TextRegion) }, { 7, typeof(TextRegion) }, { 16, typeof(PatternDictionary) },
            { 20, typeof(HalftoneRegion) }, { 22, typeof(HalftoneRegion) },
            { 23, typeof(HalftoneRegion) }, { 36, typeof(GenericRegion) },
            { 38, typeof(GenericRegion) }, { 39, typeof(GenericRegion) },
            { 40, typeof(GenericRefinementRegion) }, { 42, typeof(GenericRefinementRegion) },
            { 43, typeof(GenericRefinementRegion) }, { 48, typeof(PageInformation) },
            { 50, typeof(EndOfStripe) }, { 52, typeof(Profiles) }, { 53, typeof(Table) }
        };

        private readonly SubInputStream subInputStream;

        private byte pageAssociationFieldSize;

        private SegmentHeader[] referredToSegments;

        private WeakReference<ISegmentData> segmentData;

        public int SegmentNumber { get; private set; }

        public int SegmentType { get; private set; }

        public int PageAssociation { get; private set; }

        public long SegmentHeaderLength { get; private set; }

        public long SegmentDataLength { get; private set; }

        public long SegmentDataStartOffset { get; set; }

        public bool IsRetained { get; private set; }

        public SegmentHeader(Jbig2Document document, SubInputStream sis, long offset, int organisationType)
        {
            subInputStream = sis;
            Parse(document, sis, offset, organisationType);
        }

        private void Parse(Jbig2Document document, IImageInputStream subInputStream, long offset, int organisationType)
        {
            subInputStream.Seek(offset);

            // 7.2.2 Segment number
            ReadSegmentNumber(subInputStream);

            // 7.2.3 Segment header flags
            ReadSegmentHeaderFlag(subInputStream);

            // 7.2.4 Amount of referred-to segments
            int countOfReferredToSegments = ReadAmountOfReferredToSegments(subInputStream);

            // 7.2.5 Referred-to segments numbers
            int[] referredToSegmentNumbers = ReadReferredToSegmentsNumbers(subInputStream, countOfReferredToSegments);

            // 7.2.6 Segment page association (Checks how big the page association field is.)
            ReadSegmentPageAssociation(document, subInputStream, countOfReferredToSegments, referredToSegmentNumbers);

            // 7.2.7 Segment data length (Contains the length of the data part (in bytes).)
            ReadSegmentDataLength(subInputStream);

            ReadDataStartOffset(subInputStream, organisationType);
            ReadSegmentHeaderLength(subInputStream, offset);
        }

        /// <summary>
        /// 7.2.2 Segment number
        /// </summary>
        private void ReadSegmentNumber(IImageInputStream subInputStream)
        {
            SegmentNumber = (int)(subInputStream.ReadBits(32) & 0xffffffff);
        }

        /// <summary>
        /// 7.2.3 Segment header flags
        /// </summary>
        /// <param name="subInputStream"></param>
        private void ReadSegmentHeaderFlag(IImageInputStream subInputStream)
        {
            // Bit 7: Retain Flag, if 1, this segment is flagged as retained;
            IsRetained = subInputStream.ReadBit() == 1;

            // Bit 6: Size of the page association field. One byte if 0, four bytes if 1;
            pageAssociationFieldSize = (byte)subInputStream.ReadBit();

            // Bit 5-0: Contains the values (between 0 and 62 with gaps) for segment types, specified in 7.3
            SegmentType = (int)(subInputStream.ReadBits(6) & 0xff);
        }

        /// <summary>
        /// 7.2.4 Amount of referred-to segments
        /// </summary>
        private int ReadAmountOfReferredToSegments(IImageInputStream subInputStream)
        {
            int countOfRTS = (int)(subInputStream.ReadBits(3) & 0xf);

            byte[] retainBit;

            if (countOfRTS <= 4)
            {
                // Short format
                retainBit = new byte[5];
                for (int i = 0; i <= 4; i++)
                {
                    retainBit[i] = (byte)subInputStream.ReadBit();
                }
            }
            else
            {
                // Long format
                countOfRTS = (int)(subInputStream.ReadBits(29) & 0xffffffff);

                int arrayLength = (countOfRTS + 8) >> 3;
                retainBit = new byte[arrayLength <<= 3];

                for (int i = 0; i < arrayLength; i++)
                {
                    retainBit[i] = (byte)subInputStream.ReadBit();
                }
            }
            return countOfRTS;
        }

        /// <summary>
        /// 7.2.5 Referred-to segments numbers
        /// Gathers all segment numbers of referred-to segments.The segments itself are stored in the
        /// <see cref="referredToSegments"/> array.
        /// </summary>
        /// <param name="subInputStream">Wrapped source data input stream.</param>
        /// <param name="countOfReferredToSegments">The number of referred - to segments.</param>
        /// <returns>An array with the segment number of all referred - to segments.</returns>
        private int[] ReadReferredToSegmentsNumbers(IImageInputStream subInputStream, int countOfReferredToSegments)
        {
            int[] result = new int[countOfReferredToSegments];

            if (countOfReferredToSegments > 0)
            {
                short rtsSize = 1;
                if (SegmentNumber > 256)
                {
                    rtsSize = 2;
                    if (SegmentNumber > 65536)
                    {
                        rtsSize = 4;
                    }
                }

                referredToSegments = new SegmentHeader[countOfReferredToSegments];

                for (int i = 0; i < countOfReferredToSegments; i++)
                {
                    result[i] = (int)(subInputStream.ReadBits(rtsSize << 3) & 0xffffffff);
                }
            }

            return result;
        }

        /// <summary>
        /// 7.2.6 Segment page association
        /// </summary>
        private void ReadSegmentPageAssociation(Jbig2Document document, IImageInputStream subInputStream,
                int countOfReferredToSegments, int[] referredToSegmentNumbers)
        {
            if (pageAssociationFieldSize == 0)
            {
                // Short format
                PageAssociation = (short)(subInputStream.ReadBits(8) & 0xff);
            }
            else
            {
                // Long format
                PageAssociation = (int)(subInputStream.ReadBits(32) & 0xffffffff);
            }

            if (countOfReferredToSegments > 0)
            {
                Jbig2Page page = document.GetPage(PageAssociation);
                for (int i = 0; i < countOfReferredToSegments; i++)
                {
                    referredToSegments[i] = (null != page ? page.GetSegment(referredToSegmentNumbers[i])
                            : document.GetGlobalSegment(referredToSegmentNumbers[i]));
                }
            }
        }

        /// <summary>
        /// 7.2.7 Segment data length. Reads the length of the data part in bytes.
        /// </summary>
        private void ReadSegmentDataLength(IImageInputStream subInputStream)
        {
            SegmentDataLength = subInputStream.ReadBits(32) & 0xffffffff;
        }

        /// <summary>
        /// Sets the offset only if organization type is SEQUENTIAL. If random, data starts after segment headers and can be
        /// determined when all segment headers are parsed and allocated.
        /// </summary>
        private void ReadDataStartOffset(IImageInputStream subInputStream, int organisationType)
        {
            if (organisationType == Jbig2Document.SEQUENTIAL)
            {
                SegmentDataStartOffset = subInputStream.Position;
            }
        }

        private void ReadSegmentHeaderLength(IImageInputStream subInputStream, long offset)
        {
            SegmentHeaderLength = subInputStream.Position - offset;
        }

        public SegmentHeader[] GetRtSegments()
        {
            return referredToSegments;
        }

        /// <summary>
        /// Creates and returns a new <see cref="SubInputStream"/> that provides the data part of this segment.
        /// It is a clipped view of the source input stream.
        /// </summary>
        /// <returns>The <see cref="SubInputStream"/> that represents the data part of the segment.</returns>
        public SubInputStream GetDataInputStream()
        {
            return new SubInputStream(subInputStream, SegmentDataStartOffset, SegmentDataLength);
        }

        /// <summary>
        /// Retrieves the segments' data part.
        /// </summary>
        public ISegmentData GetSegmentData()
        {
            ISegmentData segmentDataPart = null;

            if (null != segmentData)
            {
                segmentData.TryGetTarget(out segmentDataPart);
            }

            if (null == segmentDataPart)
            {
                try
                {
                    if (!SEGMENT_TYPE_MAP.TryGetValue(SegmentType, out var segmentClassType))
                    {
                        throw new InvalidOperationException("No segment class for type " + SegmentType);
                    }

                    segmentDataPart = (ISegmentData)Activator.CreateInstance(segmentClassType);
                    segmentDataPart.Init(this, GetDataInputStream());

                    segmentData = new WeakReference<ISegmentData>(segmentDataPart);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException("Can't instantiate segment class", e);
                }
            }

            return segmentDataPart;
        }

        public void CleanSegmentData()
        {
            if (segmentData != null)
            {
                segmentData = null;
            }
        }

        public override string ToString()
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (referredToSegments != null)
            {
                foreach (SegmentHeader s in referredToSegments)
                {
                    stringBuilder.Append(s.SegmentNumber + " ");
                }
            }
            else
            {
                stringBuilder.Append("none");
            }

            return "\n#SegmentNr: " + SegmentNumber //
                    + "\n SegmentType: " + SegmentType //
                    + "\n PageAssociation: " + PageAssociation //
                    + "\n Referred-to segments: " + stringBuilder.ToString() //
                    + "\n"; //
        }
    }
}
