namespace UglyToad.PdfPig.Images.Jpg.Parts.Exif
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using UglyToad.PdfPig.Images.Jpg.Parts.ExifTypes;

    internal class ExifImageProperties
    {
        public string ImageDescription { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }

        public Orientation Orientation { get; set; }
        public int XResolution { get; set; }
        public int YResolution { get; set; }
        public ResolutionUnit ResolutionUnit { get; set; }
        public string Software { get; set; }
        public DateTime DateTime { get; set; }
        public int WhitePoint { get; set; }
        public WhiteBalance WhiteBalance { get; set; }
        public int PrimaryChromaticities { get; set; }
        public int YCbCrCoefficients { get; set; }
        public YCbCrPositioning YCbCrPositioning { get; set; }
        public int ReferenceBlackWhite { get; set; }
        public string Copyright { get; set; }
        public long ExifOffset { get; set; }
        public float ExposureTime { get; set; }
        public ExposureMode ExposureMode { get; set; }
        public float FNumber { get; set; }
        public ExposureProgram ExposureProgram { get; set; }
        public short ISOSpeedRatings { get; set; }
        public string ExifVersion { get; set; }
        public DateTime DateTimeOriginal { get; set; }
        public DateTime DateTimeDigitized { get; set; }
        public string ComponentConfiguration { get; set; }
        public int CompressedBitsPerPixel { get; set; }
        public float ShutterSpeedValue { get; set; }
        public int ApertureValue { get; set; }
        public int BrightnessValue { get; set; }
        public float ExposureBiasValue { get; set; }
        public float MaxApertureValue { get; set; }
        public int SubjectDistance { get; set; }
        public MeteringMode MeteringMode { get; set; }
        public LightSource LightSource { get; set; }
        public Flash Flash { get; set; }
        public float FocalLength { get; set; }
        public string MakerNote { get; set; }
        public string UserComment { get; set; }
        public string FlashPixVersion { get; set; }
        public ColorSpace ColorSpace { get; set; }
        public short ExifImageWidth { get; set; }
        public short ExifImageHeight { get; set; }
        public string RelatedSoundFile { get; set; }
        public long ExifInteroperabilityOffset { get; set; }
        public int FocalPlaneXResolution { get; set; }
        public int FocalPlaneYResolution { get; set; }
        public FocalPlaneResolutionUnit FocalPlaneResolutionUnit { get; set; }
        public SensingMethod SensingMethod { get; set; }
        public FileSource FileSource { get; set; }
        public string SceneType { get; set; }
        public short ImageWidth { get; set; }
        public short ImageLength { get; set; }
        public short BitsPerSample { get; set; }
        public Compression Compression { get; set; }
        public PhotometricInterpretation PhotometricInterpretation { get; set; }
        public short StripOffsets { get; set; }
        public short SamplesPerPixel { get; set; }
        public short RowsPerStrip { get; set; }
        public short StripByteConunts { get; set; }
        public PlanarConfiguration PlanarConfiguration { get; set; }
        public long JpegIFOffset { get; set; }
        public long JpegIFByteCount { get; set; }
        public short YCbCrSubSampling { get; set; }
        public long NewSubfileType { get; set; }
        public short SubfileType { get; set; }
        public short TransferFunction { get; set; }
        public string Artist { get; set; }
        public Predictor Predictor { get; set; }
        public short TileWidth { get; set; }
        public short TileLength { get; set; }
        public long TileOffsets { get; set; }
        public short TileByteCounts { get; set; }
        public long SubIFDs { get; set; }
        public string JPEGTables { get; set; }
        public short CFARepeatPatternDim { get; set; }
        public byte[] CFAPattern { get; set; }
        public int BatteryLevel { get; set; }
        public long IPTCNAA { get; set; }
        public string InterColorProfile { get; set; }
        public string SpectralSensitivity { get; set; }
        public GPSInfo GPSInfo { get; set; }
        public string OECF { get; set; }
        public short Interlace { get; set; }
        public short TimeZoneOffset { get; set; }
        public short SelfTimerMode { get; set; }
        public int FlashEnergy { get; set; }
        public string SpatialFrequencyResponse { get; set; }
        public string Noise { get; set; }
        public long ImageNumber { get; set; }
        public SecurityClassification SecurityClassification { get; set; }
        public string ImageHistory { get; set; }
        public short SubjectLocation { get; set; }
        public int ExposureIndex { get; set; }
        public byte[] TIFFEPStandardID { get; set; }
        public string SubSecTime { get; set; }
        public string SubSecTimeOriginal { get; set; }
        public string SubSecTimeDigitized { get; set; }
        public long SpecialMode { get; set; }
        public short JpegQual { get; set; }
        public short Macro { get; set; }
        public short Unknown { get; set; }
        public int DigiZoom { get; set; }
        public string SoftwareRelease { get; set; }
        public string PictInfo { get; set; }
        public string CameraID { get; set; }
        public long DataDump { get; set; }
        public int DigitalZoomRatio { get; set; }
        public short FocalLengthIn35mmFormat { get; set; }
        public SceneCaptureType SceneCaptureType { get; set; }
        public GainControl GainControl { get; set; }
        public Contrast Contrast { get; set; }
        public Saturation Saturation { get; set; }
        public Sharpness Sharpness { get; set; }
        public SubjectDistanceRange SubjectDistanceRange { get; set; }
        public InteropIndex InteropIndex { get; set; }
        public string LensInfo { get; set; }
        public string LensMake { get; set; }
        public string LensModel { get; set; }
        public string LensSerialNumber { get; set; }
    }
}
 