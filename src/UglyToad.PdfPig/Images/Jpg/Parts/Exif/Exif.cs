namespace UglyToad.PdfPig.Images.Jpg.Parts.Exif
{
    using System;    
    using System.Diagnostics;
    using System.Linq;
    using ContextForApp1Segment = Helpers.Context.ContextForApp1Segment;
    using ExifTypes;
    using ExifUtils;
    using PropertyItem = Drawing.PropertyItem;

    // https://www.media.mit.edu/pia/Research/deepview/exif.html

    internal class Exif
    {
        internal static void GetExifProperties(ContextForApp1Segment context, int segmentLength, byte[] segmentData)
        {
            var dataLength = segmentLength - 6 - 2; // Length of an IFD entry

            if (dataLength < 12)
            {
                throw new Exception($"Jpg App1 segment. Expected Exif Data length >=12. Got {dataLength}.");
            }

            var data = new byte[dataLength - 6 - 2];
            Buffer.BlockCopy(segmentData, 6, data, 0, data.Length);

            (var isBigEndian, var firstOffset) = TiffHeader(data);

            uint tagNum;

            uint offset = firstOffset;

            /* Get the number of directory entries contained in this IFD */
            var numberOfTags = (uint)ToUInt16(isBigEndian, data, offset);
            if (numberOfTags == 0) return;
            offset += 2;

            // Search for ExifSubIFD offset Tag in IFD0
            while (true)
            {
                if (offset > dataLength - 12) return; // check end of data segment

                tagNum = (uint)ToUInt16(isBigEndian, data, offset);

                if (tagNum == 0x8769) break;  // ExifSubIFD offset Tag
                if (--numberOfTags == 0) return;
                offset += 12;
            }

            /* Get the ExifSubIFD offset */
            offset = ToUInt32(isBigEndian, data, offset + 8);

            if (offset > dataLength - 2) return; /* check end of data segment */

            /* Get the number of directory entries contained in this SubIFD */
            numberOfTags = (uint)ToUInt16(isBigEndian, data, offset);
            if (numberOfTags < 2) return;
            offset += 2;

            var exifProperties = new ExifImageProperties();

            context.ExifImageProperties = exifProperties;

            // Each IFD entry is 12 bytes broken down as:
            // TTTT : 2 bytes : Tag Number
            // ffff : 2 bytes : Format / Entry Type (eg. unsigned byte, short, long, rational, ascii string)
            // NNNNNNNN : 4 bytes : Number of components
            // DDDDDDDD : 4 bytes : Data or Offset
            // Using ffff obtain number of bytes for entry type multiply by
            // NNNNNNNN to get the number of bytes to store entry data.
            // If entry data length > 4 then DDDDDDDD is an offset otherwise it is the data.
            do
            {
                if (offset > dataLength - 12) return; // check if beyond end of the data buffer

                /* Get Tag number */
                tagNum = (uint)ToUInt16(isBigEndian, data, offset);

                offset += 2;
                var entryType = (Int16)ToUInt16(isBigEndian, data, offset);

                (var entryTypeByteCount, var entryTypeDescription) = GetEntryTypeDetails(entryType);

                offset += 2;
                var entryNumberOfComponents = (int)ToUInt32(isBigEndian, data, offset);
                offset += 4;

                var entryDataByteCount = entryTypeByteCount * entryNumberOfComponents;

                var entryValue = new byte[entryDataByteCount];
                if (entryDataByteCount <= 4)
                {
                    Buffer.BlockCopy(data, (int)offset, entryValue, 0, entryValue.Length);
                    if (isBigEndian)
                    {
                        entryValue = entryValue.Reverse().ToArray();
                    }
                }
                else
                {
                    var entryDataOffset = (int)ToUInt32(isBigEndian, data, offset);
                    Buffer.BlockCopy(data, (int)entryDataOffset, entryValue, 0, entryValue.Length);
                }
                offset += 4;

                var propertyItem = new PropertyItem() { Id = (int)tagNum, Len = entryNumberOfComponents, Type = entryType, Value = entryValue };

                Convert(propertyItem, exifProperties);

            } while (--numberOfTags != 0);
        }

        static (int typeByteCount, string typeDescription) GetEntryTypeDetails(Int16 entryType)
        {
            switch (entryType)
            {
                case 1: return (1, "unsigned byte");
                case 2: return (1, "ascii string");
                case 3: return (2, "unsigned short");
                case 4: return (4, "unsigned long");
                case 5: return (8, "unsigned rational");
                case 6: return (1, "signed byte");
                case 7: return (1, "undefined");
                case 8: return (2, "unsigned short");
                case 9: return (2, "signed long");
                case 10: return (8, "signed rational");
                case 11: return (4, "signed float");
                case 12: return (8, "double float");
                default: throw new Exception($"Jpg Exif Unknown entry type 0x{entryType:X}.");
            }
        }

      

        private static (bool isBigEndian, uint offsetToFirstIFD) TiffHeader(byte[] data)
        {

            // Byte align           2 bytes either  "II" (0x49 0x49) or "MM" (0x4d 0x4d)
            // TAG Mark             2 bytes must be 0x2A 0x00
            // Offset to first IFD  4 bytes usually 0x00000008 - IFD0 then following immediately after TIFF Header


            bool isBigEndian; /* Byte order */

            {
                // Byte align (byte order) : 2 bytes : Either 0x49 0X49 "II" or 0x4D 0x4D "MM" 
                // "II" for "Intel" (Little Endian) or "MM" for "Motorola" (Big Endian)

                if (data[0] == 0x49 && data[1] == 0x49) { isBigEndian = false; }

                else if (data[0] == 0x4D && data[1] == 0x4D) { isBigEndian = true; }

                else { throw new Exception($"Jpg App1 segment. Exif unknown byte order. Expected either 0x49 or 0x4D (twice). Got: 0X{data[0]:X} 0X{data[1]:X}"); }
            }

            {
                /* Check Tag Mark : 2 bytes : 0x002a  */
                var tag = ToUInt16(isBigEndian, data, 2);
                if (tag != 0x002a)
                {
                    throw new Exception($"Jpg App1 segment. Exif TIFF header tag expected to be 0x002A. Got: 0X{tag:X}");
                }
            }

            // Get first Image File Directory (IFD) offset [IFD0 (main image)] usually offset is 8.
            var firstOffset = ToUInt32(isBigEndian, data, 4);

            if (firstOffset > data.Length - 2) // check if offset beyond the end of the data buffer
            {
                throw new Exception($"Jpg Exif offset ({firstOffset})[0x{firstOffset:X}] of first IFD beyond the end of data buffer (buffer lerngth: {data.Length}.");
            }
            return (isBigEndian, firstOffset);
        }

        static UInt32 ToUInt32(bool isBigEndian, byte[] data, uint offset)
        {
            if (isBigEndian)
            {

                UInt64 value = 0;
                value += (UInt64)(data[offset++] << 24);
                value += (UInt64)(data[offset++] << 16);
                value += (UInt64)(data[offset++] << 8);
                value += (UInt64)(data[offset++]);
                return (UInt32)value;
            }

            return BitConverter.ToUInt32(data, (int)offset);
        }

        static UInt16 ToUInt16(bool isBigEndian, byte[] data, uint offset)
        {
            if (isBigEndian)
            {
                UInt32 value = 0;
                value += (UInt32)(data[offset++] << 8);
                value += (UInt32)(data[offset++]);
                return (UInt16)value;
            }

            return BitConverter.ToUInt16(data, (int)offset);
        }

        private static void Convert(PropertyItem property, ExifImageProperties exifProperties)
        {
            try
            {
                switch (property.Id)
                {
                    case 0x010e:
                        exifProperties.ImageDescription = Utils.getStringValue(property);
                        break;
                    case 0x010f:
                        exifProperties.Make = Utils.getStringValue(property);
                        break;
                    case 0x0110:
                        exifProperties.Model = Utils.getStringValue(property);
                        break;
                    case 0x0112:
                        exifProperties.Orientation = (Orientation)Enum.ToObject(typeof(Orientation), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x011a:
                        exifProperties.XResolution = Utils.getNumberValueInt32(property);
                        break;
                    case 0x011b:
                        exifProperties.YResolution = Utils.getNumberValueInt32(property);
                        break;
                    case 0x0128:
                        exifProperties.ResolutionUnit = (ResolutionUnit)Enum.ToObject(typeof(ResolutionUnit), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x0131:
                        exifProperties.Software = Utils.getStringValue(property);
                        break;
                    case 0x0132:
                        exifProperties.DateTime = Utils.convertDateTime(property, "yyyy:MM:dd HH:mm:ss\0");
                        break;
                    case 0x013e:
                        exifProperties.WhitePoint = Utils.getNumberValueInt32(property);
                        break;
                    case 0x013f:
                        exifProperties.PrimaryChromaticities = Utils.getNumberValueInt32(property);
                        break;
                    case 0x0211:
                        exifProperties.YCbCrCoefficients = Utils.getNumberValueInt32(property);
                        break;
                    case 0x0213:
                        exifProperties.YCbCrPositioning = (YCbCrPositioning)Enum.ToObject(typeof(YCbCrPositioning), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x0214:
                        exifProperties.ReferenceBlackWhite = Utils.getNumberValueInt32(property);
                        break;
                    case 0x8298:
                        exifProperties.Copyright = Utils.getStringValue(property);
                        break;
                    case 0x8769:
                        exifProperties.ExifOffset = Utils.getNumberValueInt64(property);
                        break;
                    case 0x829a:
                        exifProperties.ExposureTime = (float)BitConverter.ToInt32(property.Value, 0) / (float)BitConverter.ToInt32(property.Value, 4);
                        break;
                    case 0x829d:
                        exifProperties.FNumber = Utils.calcFnumber(property);
                        break;
                    case 0x8822:
                        exifProperties.ExposureProgram = (ExposureProgram)Enum.ToObject(typeof(ExposureProgram), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0xa403:
                        exifProperties.WhiteBalance = (WhiteBalance)Enum.ToObject(typeof(WhiteBalance), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0xa402:
                        exifProperties.ExposureMode = (ExposureMode)Enum.ToObject(typeof(ExposureMode), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x8827:
                        exifProperties.ISOSpeedRatings = Utils.getNumberValueInt16(property);
                        break;
                    case 0x9000:
                        exifProperties.ExifVersion = Utils.getStringValue(property);
                        break;
                    case 0x9003:
                        exifProperties.DateTimeOriginal = Utils.convertDateTime(property, "yyyy:MM:dd HH:mm:ss\0");
                        break;
                    case 0x9004:
                        exifProperties.DateTimeDigitized = Utils.convertDateTime(property, "yyyy:MM:dd HH:mm:ss\0");
                        break;
                    case 0x9101:
                        exifProperties.ComponentConfiguration = Utils.getStringValue(property);
                        break;
                    case 0x9102:
                        exifProperties.CompressedBitsPerPixel = Utils.getNumberValueInt32(property);
                        break;
                    case 0x9201:
                        exifProperties.ShutterSpeedValue = Utils.calcShutterSpeedValue(property);
                        break;
                    case 0x9202:
                        exifProperties.ApertureValue = Utils.getNumberValueInt32(property);
                        break;
                    case 0x9203:
                        exifProperties.BrightnessValue = Utils.getNumberValueInt32(property);
                        break;
                    case 0x9204:
                        exifProperties.ExposureBiasValue = Utils.getNumberValueFloat(property, 4);
                        break;
                    case 0x9205:
                        exifProperties.MaxApertureValue = Utils.calcFnumber(property);
                        break;
                    case 0x9206:
                        exifProperties.SubjectDistance = Utils.getNumberValueInt32(property);
                        break;
                    case 0x9207:
                        exifProperties.MeteringMode = (MeteringMode)Enum.ToObject(typeof(MeteringMode), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x9208:
                        exifProperties.LightSource = (LightSource)Enum.ToObject(typeof(LightSource), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x9209:
                        exifProperties.Flash = (Flash)Enum.ToObject(typeof(Flash), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x920a:
                        exifProperties.FocalLength = Utils.calcFnumber(property);
                        break;
                    case 0x927c:
                        exifProperties.MakerNote = Utils.getStringValue(property);
                        break;
                    case 0x9286:
                        exifProperties.UserComment = Utils.getStringValue(property);
                        break;
                    case 0xa000:
                        exifProperties.FlashPixVersion = Utils.getStringValue(property);
                        break;
                    case 0xa001:
                        UInt16 valueColorSpace = BitConverter.ToUInt16(property.Value, 0);
                        exifProperties.ColorSpace = (ColorSpace)Enum.ToObject(typeof(ColorSpace), valueColorSpace);
                        break;
                    case 0xa002:
                        exifProperties.ExifImageWidth = Utils.getNumberValueInt16(property);
                        break;
                    case 0xa003:
                        exifProperties.ExifImageHeight = Utils.getNumberValueInt16(property);
                        break;
                    case 0xa004:
                        exifProperties.RelatedSoundFile = Utils.getStringValue(property);
                        break;
                    case 0xa005:
                        exifProperties.ExifInteroperabilityOffset = Utils.getNumberValueInt64(property);
                        break;
                    case 0xa20e:
                        exifProperties.FocalPlaneXResolution = Utils.getNumberValueInt32(property);
                        break;
                    case 0xa20f:
                        exifProperties.FocalPlaneYResolution = Utils.getNumberValueInt32(property);
                        break;
                    case 0xa210:
                        exifProperties.FocalPlaneResolutionUnit = (FocalPlaneResolutionUnit)Enum.ToObject(typeof(FocalPlaneResolutionUnit), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0xa217:
                        exifProperties.SensingMethod = (SensingMethod)Enum.ToObject(typeof(SensingMethod), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0xa300:
                        exifProperties.FileSource = (FileSource)Enum.ToObject(typeof(FileSource), Int32.Parse(BitConverter.ToString(property.Value)));
                        break;
                    case 0xa301:
                        exifProperties.SceneType = Utils.getStringValue(property);
                        break;
                    case 0x0100:
                        exifProperties.ImageWidth = Utils.getNumberValueInt16(property);
                        break;
                    case 0x0101:
                        exifProperties.ImageLength = Utils.getNumberValueInt16(property);
                        break;
                    case 0x0102:
                        exifProperties.BitsPerSample = Utils.getNumberValueInt16(property);
                        break;
                    case 0x0103:
                        exifProperties.Compression = (Compression)Enum.ToObject(typeof(Compression), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x0106:
                        exifProperties.PhotometricInterpretation = (PhotometricInterpretation)Enum.ToObject(typeof(PhotometricInterpretation), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x0111:
                        exifProperties.StripOffsets = Utils.getNumberValueInt16(property);
                        break;
                    case 0x0115:
                        exifProperties.SamplesPerPixel = Utils.getNumberValueInt16(property);
                        break;
                    case 0x0116:
                        exifProperties.RowsPerStrip = Utils.getNumberValueInt16(property);
                        break;
                    case 0x0117:
                        exifProperties.StripByteConunts = Utils.getNumberValueInt16(property);
                        break;
                    case 0x011c:
                        exifProperties.PlanarConfiguration = (PlanarConfiguration)Enum.ToObject(typeof(PlanarConfiguration), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x0212:
                        exifProperties.YCbCrSubSampling = Utils.getNumberValueInt16(property);
                        break;
                    case 0x00fe:
                        exifProperties.NewSubfileType = Utils.getNumberValueInt64(property);
                        break;
                    case 0x00ff:
                        exifProperties.SubfileType = Utils.getNumberValueInt16(property);
                        break;
                    case 0x012d:
                        exifProperties.TransferFunction = Utils.getNumberValueInt16(property);
                        break;
                    case 0x013b:
                        exifProperties.Artist = Utils.getStringValue(property);
                        break;
                    case 0x013d:
                        exifProperties.Predictor = (Predictor)Enum.ToObject(typeof(Predictor), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x0142:
                        exifProperties.TileWidth = Utils.getNumberValueInt16(property);
                        break;
                    case 0x0143:
                        exifProperties.TileLength = Utils.getNumberValueInt16(property);
                        break;
                    case 0x0144:
                        exifProperties.TileOffsets = Utils.getNumberValueInt64(property);
                        break;
                    case 0x0145:
                        exifProperties.TileByteCounts = Utils.getNumberValueInt16(property);
                        break;
                    case 0x014a:
                        exifProperties.SubIFDs = Utils.getNumberValueInt64(property);
                        break;
                    case 0x015b:
                        exifProperties.JPEGTables = Utils.getStringValue(property);
                        break;
                    case 0x828d:
                        exifProperties.CFARepeatPatternDim = Utils.getNumberValueInt16(property);
                        break;
                    case 0x828e:
                        exifProperties.CFAPattern = property.Value;
                        break;
                    case 0x828f:
                        exifProperties.BatteryLevel = Utils.getNumberValueInt32(property);
                        break;
                    case 0x83bb:
                        exifProperties.IPTCNAA = Utils.getNumberValueInt64(property);
                        break;
                    case 0x8773:
                        exifProperties.InterColorProfile = Utils.getStringValue(property);
                        break;
                    case 0x8824:
                        exifProperties.SpectralSensitivity = Utils.getStringValue(property);
                        break;
                    case 0x0000:
                        exifProperties.GPSInfo.VersionID = Utils.getNumberValueInt16(property);
                        break;
                    case 0x0001:
                        exifProperties.GPSInfo.LatitudeRef = (LatitudeRef)Enum.ToObject(typeof(LatitudeRef), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x0002:
                        exifProperties.GPSInfo.Latitude = GPSInfo.ExifGpsToFloat(exifProperties.GPSInfo.LatitudeRef.ToString(), property);
                        break;
                    case 0x0003:
                        exifProperties.GPSInfo.LongitudeRef = (LongitudeRef)Enum.ToObject(typeof(LongitudeRef), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x0004:
                        exifProperties.GPSInfo.Longitude = GPSInfo.ExifGpsToFloat(exifProperties.GPSInfo.LongitudeRef.ToString(), property);
                        break;
                    case 0x0005:
                        exifProperties.GPSInfo.AltitudeRef = (AltitudeRef)Enum.ToObject(typeof(AltitudeRef), Int32.Parse(BitConverter.ToString(property.Value)));
                        break;
                    case 0x0006:
                        exifProperties.GPSInfo.Altitude = (float)BitConverter.ToInt32(property.Value, 0) / (float)BitConverter.ToInt32(property.Value, 4);
                        break;
                    case 0x0007:
                        //exifProperties.GPSInfo.TimeStamp = DateTime.ParseExact(${BitConverter.ToInt32(property.Value, 0).ToString().PadLeft(2, '0')}:{BitConverter.ToInt32(property.Value, 8).ToString().PadLeft(2, '0')}:{BitConverter.ToInt32(property.Value, 16).ToString().PadLeft(2, '0')}\0, HH:mm:ss\0, System.Globalization.CultureInfo.InvariantCulture);
                        break;
                    case 0x0008:
                        exifProperties.GPSInfo.Satellites = Utils.getStringValue(property);
                        break;
                    case 0x0009:
                        exifProperties.GPSInfo.Status = (Status)Enum.ToObject(typeof(Status), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x000a:
                        exifProperties.GPSInfo.MeasureMode = Utils.getStringValue(property);
                        break;
                    case 0x000b:
                        exifProperties.GPSInfo.DOP = Utils.getNumberValueInt32(property);
                        break;
                    case 0x000c:
                        exifProperties.GPSInfo.SpeedRef = Utils.getStringValue(property);
                        break;
                    case 0x000d:
                        exifProperties.GPSInfo.Speed = Utils.getNumberValueInt32(property);
                        break;
                    case 0x000e:
                        exifProperties.GPSInfo.TrackRef = (TrackRef)Enum.ToObject(typeof(TrackRef), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x000f:
                        exifProperties.GPSInfo.Track = Utils.getNumberValueInt32(property);
                        break;
                    case 0x0010:
                        exifProperties.GPSInfo.ImgDirectionRef = (ImgDirectionRef)Enum.ToObject(typeof(ImgDirectionRef), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x0011:
                        exifProperties.GPSInfo.ImgDirection = Utils.getNumberValueInt32(property);
                        break;
                    case 0x0012:
                        exifProperties.GPSInfo.MapDatum = Utils.getStringValue(property);
                        break;
                    case 0x0013:
                        exifProperties.GPSInfo.DestLatitudeRef = Utils.getStringValue(property);
                        break;
                    case 0x0014:
                        exifProperties.GPSInfo.DestLatitude = Utils.getNumberValueInt32(property);
                        break;
                    case 0x0015:
                        exifProperties.GPSInfo.DestLongitudeRef = (DestLongitudeRef)Enum.ToObject(typeof(DestLongitudeRef), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x0016:
                        exifProperties.GPSInfo.DestLongitude = Utils.getNumberValueInt32(property);
                        break;
                    case 0x0017:
                        exifProperties.GPSInfo.DestBearingRef = (DestBearingRef)Enum.ToObject(typeof(DestBearingRef), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x0018:
                        exifProperties.GPSInfo.DestBearing = Utils.getNumberValueInt32(property);
                        break;
                    case 0x0019:
                        exifProperties.GPSInfo.DestDistanceRef = (DestDistanceRef)Enum.ToObject(typeof(DestDistanceRef), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x001a:
                        exifProperties.GPSInfo.DestDistance = Utils.getNumberValueInt32(property);
                        break;
                    case 0x001b:
                        exifProperties.GPSInfo.ProcessingMethod = Utils.getStringValue(property);
                        break;
                    case 0x001c:
                        exifProperties.GPSInfo.AreaInformation = Utils.getStringValue(property);
                        break;
                    case 0x001d:
                        exifProperties.GPSInfo.DateStamp = Utils.convertDateTime(property, "yyyy:MM:dd\0");
                        break;
                    case 0x001e:
                        exifProperties.GPSInfo.Differential = (Differential)Enum.ToObject(typeof(Differential), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0x001f:
                        exifProperties.GPSInfo.HPositioningError = Utils.getNumberValueInt32(property);
                        break;
                    case 0x8828:
                        exifProperties.OECF = Utils.getStringValue(property);
                        break;
                    case 0x8829:
                        exifProperties.Interlace = Utils.getNumberValueInt16(property);
                        break;
                    case 0x882a:
                        exifProperties.TimeZoneOffset = Utils.getNumberValueInt16(property);
                        break;
                    case 0x882b:
                        exifProperties.SelfTimerMode = Utils.getNumberValueInt16(property);
                        break;
                    case 0x920b:
                        exifProperties.FlashEnergy = Utils.getNumberValueInt32(property);
                        break;
                    case 0x920c:
                        exifProperties.SpatialFrequencyResponse = Utils.getStringValue(property);
                        break;
                    case 0x920d:
                        exifProperties.Noise = Utils.getStringValue(property);
                        break;
                    case 0x9211:
                        exifProperties.ImageNumber = Utils.getNumberValueInt64(property);
                        break;
                    case 0x9212:
                        exifProperties.SecurityClassification = (SecurityClassification)Enum.ToObject(typeof(SecurityClassification), new System.Text.ASCIIEncoding().GetString(property.Value));
                        break;
                    case 0x9213:
                        exifProperties.ImageHistory = Utils.getStringValue(property);
                        break;
                    case 0x9214:
                        exifProperties.SubjectLocation = Utils.getNumberValueInt16(property);
                        break;
                    case 0x9215:
                        exifProperties.ExposureIndex = Utils.getNumberValueInt32(property);
                        break;
                    case 0x9216:
                        exifProperties.TIFFEPStandardID = property.Value;
                        break;
                    case 0x9290:
                        exifProperties.SubSecTime = Utils.getStringValue(property);
                        break;
                    case 0x9291:
                        exifProperties.SubSecTimeOriginal = Utils.getStringValue(property);
                        break;
                    case 0x9292:
                        exifProperties.SubSecTimeDigitized = Utils.getStringValue(property);
                        break;
                    case 0xa20b:
                        exifProperties.FlashEnergy = Utils.getNumberValueInt32(property);
                        break;
                    case 0xa20c:
                        exifProperties.SpatialFrequencyResponse = Utils.getStringValue(property);
                        break;
                    case 0xa214:
                        exifProperties.SubjectLocation = Utils.getNumberValueInt16(property);
                        break;
                    case 0xa215:
                        exifProperties.ExposureIndex = Utils.getNumberValueInt32(property);
                        break;
                    case 0xa302:
                        exifProperties.CFAPattern = property.Value;
                        break;
                    case 0x0200:
                        exifProperties.SpecialMode = Utils.getNumberValueInt64(property);
                        break;
                    case 0x0201:
                        exifProperties.JpegQual = Utils.getNumberValueInt16(property);
                        break;
                    case 0x0202:
                        exifProperties.Macro = Utils.getNumberValueInt16(property);
                        break;
                    case 0x0203:
                        exifProperties.Unknown = Utils.getNumberValueInt16(property);
                        break;
                    case 0x0204:
                        exifProperties.DigiZoom = Utils.getNumberValueInt32(property);
                        break;
                    case 0x0207:
                        exifProperties.SoftwareRelease = Utils.getStringValue(property);
                        break;
                    case 0x0208:
                        exifProperties.PictInfo = Utils.getStringValue(property);
                        break;
                    case 0x0209:
                        exifProperties.CameraID = Utils.getStringValue(property);
                        break;
                    case 0x0f00:
                        exifProperties.DataDump = Utils.getNumberValueInt64(property);
                        break;
                    case 0xa404:
                        exifProperties.DigitalZoomRatio = Utils.getNumberValueInt32(property);
                        break;
                    case 0xa405:
                        exifProperties.FocalLengthIn35mmFormat = Utils.getNumberValueInt16(property);
                        break;
                    case 0xa406:
                        exifProperties.SceneCaptureType = (SceneCaptureType)Enum.ToObject(typeof(SceneCaptureType), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0xa407:
                        exifProperties.GainControl = (GainControl)Enum.ToObject(typeof(GainControl), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0xa408:
                        exifProperties.Contrast = (Contrast)Enum.ToObject(typeof(Contrast), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0xa409:
                        exifProperties.Saturation = (Saturation)Enum.ToObject(typeof(Saturation), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0xa40a:
                        exifProperties.Sharpness = (Sharpness)Enum.ToObject(typeof(Sharpness), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0xa40c:
                        exifProperties.SubjectDistanceRange = (SubjectDistanceRange)Enum.ToObject(typeof(SubjectDistanceRange), BitConverter.ToInt16(property.Value, 0));
                        break;
                    case 0xa432:
                        exifProperties.LensInfo = Utils.getStringValue(property);
                        break;
                    case 0xa433:
                        exifProperties.LensMake = Utils.getStringValue(property);
                        break;
                    case 0xa434:
                        exifProperties.LensModel = Utils.getStringValue(property);
                        break;
                    case 0xa435:
                        exifProperties.LensSerialNumber = Utils.getStringValue(property);
                        break;
                    default:
                        Debug.WriteLine($"Jpg Exif Unknown tag 0x{property.Id:X}");
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Jpg Exif unable to convert tag 0x{property.Id:X} {ex}");
            }
        }
    }
}
