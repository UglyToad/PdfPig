using System;
using System.Collections.Generic;
using System.Text;
using PropertyItem = UglyToad.PdfPig.Images.Jpg.Parts.Drawing.PropertyItem;
namespace UglyToad.PdfPig.Images.Jpg.Parts.ExifUtils
{
    class Utils
    {
        public static float calcFnumber(PropertyItem property)
        {
            return (float)BitConverter.ToInt32(property.Value, 0) / (float)BitConverter.ToInt32(property.Value, 4);
        }
        
        public static float calcShutterSpeedValue(PropertyItem property)
        {
            return (float)Math.Pow(2, Math.Abs(((float)BitConverter.ToInt32(property.Value, 0)) / (float)BitConverter.ToInt32(property.Value, 4)));
        }

        public static DateTime convertDateTime(PropertyItem property, string format)
        {
            return DateTime.ParseExact(new ASCIIEncoding().GetString(property.Value), format, System.Globalization.CultureInfo.InvariantCulture);
        }

        public static int getNumberValueInt32(PropertyItem property, int position = 0)
        {
            return BitConverter.ToInt32(property.Value, position);
        }

        public static short getNumberValueInt16(PropertyItem property, int position = 0)
        {
            return BitConverter.ToInt16(property.Value, position);
        }

        public static long getNumberValueInt64(PropertyItem property, int position = 0)
        {
            return BitConverter.ToInt64(property.Value, position);
        }

        public static float getNumberValueFloat(PropertyItem property, int position = 0)
        {
            return BitConverter.ToInt32(property.Value, position);
        }

        public static string getStringValue(PropertyItem property)
        {
            return new ASCIIEncoding().GetString(property.Value);
        }
    }
}
