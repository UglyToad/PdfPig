using System;
using System.Collections.Generic;
using System.ComponentModel;
using PropertyItem = UglyToad.PdfPig.Images.Jpg.Parts.Drawing.PropertyItem;
 

namespace UglyToad.PdfPig.Images.Jpg.Parts.ExifTypes
{
    internal class GPSInfo
    {
        internal short VersionID { get; set; }
        internal LatitudeRef LatitudeRef { get; set; }
        internal double Latitude { get; set; }
        internal LongitudeRef LongitudeRef { get; set; }
        internal double Longitude { get; set; }
        internal AltitudeRef AltitudeRef { get; set; }
        internal float Altitude { get; set; }
        internal DateTime TimeStamp { get; set; }
        internal string Satellites { get; set; }
        internal Status Status { get; set; }
        internal string MeasureMode { get; set; }
        internal long DOP { get; set; }
        internal string SpeedRef { get; set; }
        internal long Speed { get; set; }
        internal TrackRef TrackRef { get; set; }
        internal long Track { get; set; }
        internal ImgDirectionRef ImgDirectionRef { get; set; }
        internal long ImgDirection { get; set; }
        internal string MapDatum { get; set; }
        internal string DestLatitudeRef { get; set; }
        internal double DestLatitude { get; set; }
        internal DestLongitudeRef DestLongitudeRef { get; set; }
        internal int DestLongitude { get; set; }
        internal DestBearingRef DestBearingRef { get; set; }
        internal double DestBearing { get; set; }
        internal DestDistanceRef DestDistanceRef { get; set; }
        internal double DestDistance { get; set; }
        internal string ProcessingMethod { get; set; }
        internal string AreaInformation { get; set; }
        internal DateTime DateStamp { get; set; }
        internal Differential Differential { get; set; }
        internal double HPositioningError { get; set; }

        internal static double ExifGpsToFloat(string gpsRef, PropertyItem propItem)
        {
            uint degreesNumerator = BitConverter.ToUInt32(propItem.Value, 0);
            uint degreesDenominator = BitConverter.ToUInt32(propItem.Value, 4);
            float degrees = degreesNumerator / (float)degreesDenominator;

            uint minutesNumerator = BitConverter.ToUInt32(propItem.Value, 8);
            uint minutesDenominator = BitConverter.ToUInt32(propItem.Value, 12);
            float minutes = minutesNumerator / (float)minutesDenominator;

            uint secondsNumerator = BitConverter.ToUInt32(propItem.Value, 16);
            uint secondsDenominator = BitConverter.ToUInt32(propItem.Value, 20);
            float seconds = secondsNumerator / (float)secondsDenominator;

            float coorditate = degrees + (minutes / 60f) + (seconds / 3600f);

            if (gpsRef == "South" || gpsRef == "West")
                coorditate = 0 - coorditate;
            return coorditate;
        }

        internal static DateTime GetDateTime(PropertyItem propItem, string formatacao)
        {
            var data = new DateTime();

            DateTime.ParseExact(BitConverter.ToInt16(propItem.Value, 0).ToString(), formatacao, System.Globalization.CultureInfo.InvariantCulture);

            return data;
        }
    }

    internal enum LatitudeRef
    {
        North = 'N',
        South = 'S'
    }

    internal enum LongitudeRef
    {
        East = 'E',
        West = 'W'
    }

    internal enum AltitudeRef
    {
        [Description("Above Sea Level")]
        AboveSeaLevel = 0,
        [Description("Below Sea Level")]
        BelowSeaLevel = 1
    }

    internal enum Status
    {
        [Description("Measurement Active")]
        MeasurementActive = 'A',
        [Description("Measurement Void")]
        MeasurementVoid = 'V'
    }
    internal enum TrackRef
    {
        [Description("Magnetic North")]
        MagneticNorth = 'M',
        [Description("True North")]
        TrueNorth = 'T'
    }
    internal enum ImgDirectionRef
    {
        [Description("Magnetic North")]
        MagneticNorth = 'M',
        [Description("True North")]
        TrueNorth = 'T'
    }

    internal enum DestLongitudeRef
    {
        East = 'E',
        West = 'W'
    }
    internal enum DestBearingRef
    {
        [Description("Magnetic North")]
        MagneticNorth = 'M',
        [Description("True North")]
        TrueNorth = 'T'
    }
    internal enum DestDistanceRef
    {
        Kilometers = 'K',
        Miles = 'M',
        [Description("Nautical Miles")]
        NauticalMiles = 'N'
    }
    internal enum Differential
    {
        [Description("No Correction")]
        NoCorrection = 0,
        [Description(" Differential Corrected")]
        DifferentialCorrected = 1
    }
}
