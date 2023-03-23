namespace UglyToad.PdfPig.Images.Jpg.Parts
{

    //JPEG ISO/IEC 10918-1 : 1993(E)  Table B.1 Page 36 https://www.w3.org/Graphics/JPEG/itu-t81.pdf
    // https://github.com/corkami/formats/blob/master/image/jpeg.md
    // https://github.com/BitMiracle/libjpeg.net/blob/f4b1a86d3ddb8bd45ef47ac993746c85d79401a2/LibJpeg/Classic/JPEG_MARKER.cs
    // https://github.com/BitMiracle/libjpeg.net
    internal enum JpegMarker : byte
    {
        /// <summary>
        /// Internal use - marker is not yet known.
        /// </summary>
        Unknown = 0x00,

        /// <summary>
        /// (TEM) For temporary private use in arithmetic coding    
        /// </summary>
        TemporaryPrivateUseInArithmeticCoding = 0x01,

        

        #region JPEG 2000
        /// <summary>
        /// (SOC) Start of codestream        
        /// </summary>
        StartOfCodeStream = 0x4F,
        /// <summary>
        /// (SOT) Start of tile
        /// </summary>
        StartOfTile = 0x90,
        /// <summary>
        /// (SOD) Start of d....(?)
        /// </summary>
        StartOfD = 0x93,

        /// (EOC) End of codestream (overlaps EOI) 0xD9
         
        /// <summary>
        /// (SIZ) Image and tile size
        /// </summary>
        SizeofTile = 0x51,

        #region Functional Segments
        /// <summary>
        /// (COD) Coding Style Default
        /// </summary>
        CodingStyleDefault = 0x52,
        /// <summary>
        /// (COC) Coding Style Component
        /// </summary>
        CodingStyleComponent = 0x53,
        /// <summary>
        /// (QCD) Quantization Default
        /// </summary>
        QuantizationDefault = 0x5C,
        /// <summary>
        /// (QCC) Quantization Component
        /// </summary>
        QuantizationComponent = 0x5D,
        /// <summary>
        /// (RGN) Region of interest
        /// </summary>
        RegionOfInterest = 0x5E,
        /// <summary>
        /// (POC) Progression Order Change
        /// </summary>
        ProgressionOrderChange = 0x5F,
        #endregion

        #region pointer segments
        /// <summary>
        /// (TLM) Tile-part Lengths
        /// </summary>
        TilePartLenths = 0x55,
        /// <summary>
        /// (PLM) Packet Length (main header)
        /// </summary>
        PacketLengthMainHeader = 0x57,
        /// <summary>
        /// (PLT) Packet Length (tile-part header)
        /// </summary>
        PacketLengthTilePartHeader = 0x58,
        /// <summary>
        /// (PPM) packed packet headers (main header)
        /// </summary>
        PacketedPacketHeadersMainHeader = 0x60,
        /// <summary>
        /// (PPT) packed packet headers (tile-part header)
        /// </summary>
        PacketedPacketHeadersTilePartHeader = 0x61,
        #endregion
        #region bitstream internal markers and segments
        /// <summary>
        /// (SOP) start of packet
        /// </summary>
        StartOfPacket = 0x91,
        /// <summary>
        /// (EOP) end of packet header
        /// </summary>
        EndOfPacketHeader = 0x92,
        #endregion
        #region informational segments
        /// <summary>
        /// (CRG) component registration
        /// </summary>
        ComponentRegistration = 0x63,
        /// <summary>
        /// (COM) comment
        /// </summary>
        CommentJpeg2000 = 0x64,
        /// <summary>
        /// (CBD) Component bit depth definition
        /// </summary>
        ComponentBitDepthDefinition = 0x78,
        /// <summary>
        /// (MCT) Multiple Component Transform
        /// </summary>
        MultipleComponentTransform = 0x74,
        /// <summary>
        /// (MCC) Multiple Component Collection
        /// </summary>
        MultipleComponentCollection = 0x75,
        /// <summary>
        /// (MCO) Multiple component transformation ordering
        /// </summary>
        MultipleComponentTransformationOrdering = 0x77,
        #endregion
        #region Part 8: Secure JPEG 2000
        /// <summary>
        /// (SEC) SEcured Codestream
        /// </summary>
        SecuredCodestream = 0x65,
        /// <summary>
        ///  (INSEC) INSEcured Codestream
        /// </summary>
        InsecuredCodestream = 0x94,
        #endregion
        #region Part 11: JPEG 2000 for Wireless
        /// <summary>
        ///  (EPC) Error Protection Capability
        /// </summary>
        ErrorProtectionCapability = 0x68,
        /// <summary>
        ///  (EPB) Error Protection Block
        /// </summary>
        ErrorProtectionBlock = 0x66,
        /// <summary>
        ///  (ESD) Error Sensitivity Descriptor
        /// </summary>
        ErrorSensitivityDescriptor = 0x67,
        /// <summary>
        ///  (RED) Residual Error Descriptor
        /// </summary>
        ResidualErrorDescriptor = 0x69,
        #endregion
        #endregion


        #region Start Of Frame (SOF)      
        #region Non-differential Huffman
        /// <summary>
        /// (SOF0) Start Of Frame markers, non-differential, Huffman coding : Baseline DCT
        /// Indicates that this is a baseline DCT-based JPEG, and specifies the width, height, number of components, and component subsampling.
        /// </summary>
        StartOfFrame0BaselineDctFrame = 0xC0,
        /// <summary>
        /// (SOF1) Start Of Frame markers, non-differential, Huffman coding : Extended sequential DCT
        /// </summary>
        StartOfFame1BaselineDctExtendedSequentialFrame = 0xC1,
        /// <summary>
        /// (SOF2) Start Of Frame markers, non-differential, Huffman coding : Progressive DCT
        /// </summary>
        StartOfFrame2ProgressiveDctFrame = 0xC2,
        /// <summary>
        /// (SOF3) Start Of Frame markers, non-differential, Huffman coding : Lossless (sequential)
        /// </summary>
        StartOfFrame3LosslessSequential = 0xC3,
        #endregion

        // Note there is no SOF4 - for C4 see DefineHuffmanTable

        #region Differential Huffman coding
        /// <summary>
        /// (SOF5) Start Of Frame markers, differential, Huffman coding : Differential sequential DCT        
        /// </summary>
        StartOfFrame5DifferentialSequentialDct = 0xC5,
        /// <summary>
        /// (SOF6) Start Of Frame markers, differential, Huffman coding : Differential progressive DCT
        /// </summary>
        StartOfFrame6DifferentialProgressiveDct = 0xC6,
        /// <summary>
        /// (SOF7) Start Of Frame markers, differential, Huffman coding : Differential lossless (sequential)
        /// </summary>
        StartOfFrame7DifferentialLosslessSequential = 0xC7,
        #endregion

        #region Non-differential, arithmetic coding
        /// <summary>
        /// (JPG) Start Of Frame markers, non-differential, arithmetic coding: Reserved for JPEG extensions
        /// </summary>
        StartOfFrameJpgArithmeticLosslessSequential = 0xC8,
      
        /// <summary>
        /// (SOF9) Start Of Frame markers, non-differential, arithmetic coding: Extended sequential DCT
        /// </summary>
        StartOfFrame9ArithmeticDctExtendedSequentialFrame = 0xC9,
      
        /// <summary>
        /// (SOF10) Start Of Frame markers, non-differential, arithmetic coding: Progressive DCT
        /// </summary>
        StartOfFrame10ArithmeticProgressiveDctFrame = 0xCA,
        /// <summary>
        /// (SOF11) Start Of Frame markers, non-differential, arithmetic coding: Progressive DCT
        /// </summary>
        StartOfFrame11ArithmeticLosslessSequential = 0xCB,
        #endregion

        /// <summary>
        /// (DHT) Huffman table specification :  Define Huffman table(s)
        /// Specifies one or more Huffman tables.
        /// </summary>
        DefineHuffmanTable = 0xC4,
        #endregion

        /// <summary>
        /// (DAC) Arithmetic coding conditioning specification : Define arithmetic coding conditioning(s)
        /// </summary>
        ArithmeticCodingConditioningSpecificationrithmeticCodingConditionings = 0xCC,

        #region Reset internval termination
        /// <summary>
        /// (RST0) Restart with modulo 8 count “m”
        /// Inserted every r macroblocks.
        /// </summary> 
        Restart0 = 0xD0,
        /// <summary>
        /// (RST1) Restart with modulo 8 count “m”
        /// Inserted every r macroblocks.
        /// </summary>        
        Restart1 = 0xD1,
        /// <summary>
        /// (RST2) Restart with modulo 8 count “m”
        /// Inserted every r macroblocks.
        /// </summary>
        Restart2 = 0xD2,
        /// <summary>
        /// (RST3) Restart with modulo 8 count “m”
        /// Inserted every r macroblocks.
        /// </summary>
        Restart3 = 0xD3,
        /// <summary>
        /// (RST4) Restart with modulo 8 count “m”
        /// Inserted every r macroblocks.
        /// </summary>
        Restart4 = 0xD4,
        /// <summary>
        /// (RST5) Restart with modulo 8 count “m”
        /// Inserted every r macroblocks.
        /// </summary>
        Restart5 = 0xD5,
        /// <summary>
        /// (RST6) Restart with modulo 8 count “m”
        /// Inserted every r macroblocks.
        /// </summary>
        Restart6 = 0xD6,
        /// <summary>
        /// (RST7) Restart with modulo 8 count “m”
        /// Inserted every r macroblocks.
        /// </summary>
        Restart7 = 0xD7,
        #endregion

        #region Other markers
        /// <summary>
        /// (SOI) Marks the start of a JPEG image file.
        /// </summary>
        StartOfImage = 0xD8,
        /// <summary>
        /// (EOI) Marks the end of a JPEG image file.
        /// </summary>
        EndOfImage = 0xD9,
        /// <summary>
        /// (SOS) Start of scan
        /// Begins a top-to-bottom scan of the image. In baseline images, there is generally a single scan.
        /// Progressive images usually contain multiple scans. 
        /// </summary>
        StartOfScan = 0xDA,
        /// <summary> 
        /// (DQT) Define quantization table(s)
        /// Specifies one or more quantization tables. 
        /// </summary>
        DefineQuantizationTable = 0xDB,
        /// <summary>
        /// (DNL) Define number of lines                
        /// </summary>
        DefineNumberOfLines = 0xDC,
        /// <summary>
        /// (DRI) Define restart interval
        /// Specifies the interval between RSTn markers, in Minimum Coded Units (MCUs).
        /// This marker is followed by two bytes indicating the fixed size so it can be treated like any other variable size segment.
        /// </summary>
        DefineRestartInterval = 0xDD,
        /// <summary>
        /// (DHP) Define hierarchical progression        
        /// </summary>
        DefineHierarchicalProgression = 0xDE,
        /// <summary>
        /// (EXP) Expand reference component(s)      
        /// </summary>
        ExpandReferenceComponents = 0xDF,

        #region Application Specific
        /// <summary>
        /// (App0) JFIF (len >=14) / JFXX (len >= 6) / AVI MJPEG
        /// </summary>
        ApplicationSpecific0 = 0xE0,
        /// <summary>
        /// (App1) EXIF/XMP/XAP
        /// </summary>
        ApplicationSpecific1 = 0xE1,
        /// <summary>
        /// (App2) FlashPix 
        /// </summary>
        ApplicationSpecific2 = 0xE2,
        /// <summary>
        /// (App4) Kodak
        /// </summary>
        ApplicationSpecific3 = 0xE3,
        /// <summary>
        /// (App4) FlashPix
        /// </summary>
        ApplicationSpecific4 = 0xE4,
        /// <summary>
        /// (App5) Ricoh
        /// </summary>
        ApplicationSpecific5 = 0xE5,
        /// <summary>
        /// (App6) GoPro
        /// </summary>
        ApplicationSpecific6 = 0xE6,
        /// <summary>
        /// (App7) Pentax/Qualcomm
        /// </summary>
        ApplicationSpecific7 = 0xE7,
        /// <summary>
        /// (App8) Spiff
        /// </summary>
        ApplicationSpecific8 = 0xE8,
        /// <summary>
        /// (App9) MediaJukebox
        /// </summary>
        ApplicationSpecific9 = 0xE9,
        /// <summary>
        /// (App10) PhotoStudio
        /// </summary>
        ApplicationSpecific10 = 0xEA,
        /// <summary>
        /// (App11) HDR
        /// </summary>
        ApplicationSpecific11 = 0xEB,
        /// <summary>
        /// (App12) (photoshoP ducky / savE foR web
        /// </summary>
        ApplicationSpecific12 = 0xEC,
        /// <summary>
        /// (App13) photoshoP savE As
        /// </summary>
        ApplicationSpecific13 = 0xED,
        /// <summary>
        /// (App14) Application segment 14 ("adobe" (length = 12))
        /// </summary>
        ApplicationSpecific14 = 0xEE,
        ApplicationSpecific15 = 0xEF,
        #endregion
        #region Reserved for JPEG extensions
        JpgExtensions0 = 0xF0,
        JpgExtensions1 = 0xF1,
        JpgExtensions2 = 0xF2,
        JpgExtensions3 = 0xF3,
        JpgExtensions4 = 0xF4,
        JpgExtensions5 = 0xF5,
        JpgExtensions6 = 0xF6,
        JpgExtensions7 = 0xF7, // JPEG-LS  SOF48
        JpgExtensions8 = 0xF8, // JPEG-LS  LSE
        JpgExtensions9 = 0xF9,
        JpgExtensions10 = 0xFA,
        JpgExtensions11 = 0xFB,
        JpgExtensions12 = 0xFC,
        JpgExtensions13 = 0xFD,
        JpgExtensions14 = 0xFE,
        JpgExtensions15 = 0xFF,
        #endregion

        /// <summary>
        /// Marks a text comment.
        /// </summary>
        Comment = 0xFE
        #endregion

    }

  
}