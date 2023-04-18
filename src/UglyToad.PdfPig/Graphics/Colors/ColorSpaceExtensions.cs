namespace UglyToad.PdfPig.Graphics.Colors
{
    using System;
    using Tokens;

    /// <summary>
    /// Provides utility extension methods for dealing with <see cref="ColorSpace"/>s.
    /// </summary>
    public static class ColorSpaceExtensions
    {
        /// <summary>
        /// Gets the corresponding <see cref="ColorSpaceFamily"/> for a given <see cref="ColorSpace"/>.
        /// </summary>
        public static ColorSpaceFamily GetFamily(this ColorSpace colorSpace)
        {
            switch (colorSpace)
            {
                case ColorSpace.DeviceGray:
                case ColorSpace.DeviceRGB:
                case ColorSpace.DeviceCMYK:
                    return ColorSpaceFamily.Device;
                case ColorSpace.CalGray:
                case ColorSpace.CalRGB:
                case ColorSpace.Lab:
                case ColorSpace.ICCBased:
                    return ColorSpaceFamily.CIEBased;
                case ColorSpace.Indexed:
                case ColorSpace.Pattern:
                case ColorSpace.Separation:
                case ColorSpace.DeviceN:
                    return ColorSpaceFamily.Special;
                default:
                    throw new ArgumentException($"Unrecognized colorspace: {colorSpace}.");
            }
        }

        /// <summary>
        /// Maps from a <see cref="NameToken"/> to the corresponding <see cref="ColorSpace"/> if one exists.
        /// <para>Includes extended color spaces.</para>
        /// </summary>
        public static bool TryMapToColorSpace(this NameToken name, out ColorSpace colorspace)
        {
            colorspace = ColorSpace.DeviceGray;

            if (name.Data == NameToken.Devicegray.Data || name.Data == "G")
            {
                colorspace = ColorSpace.DeviceGray;
            }
            else if (name.Data == NameToken.Devicergb.Data || name.Data == "RGB")
            {
                colorspace = ColorSpace.DeviceRGB;
            }
            else if (name.Data == NameToken.Devicecmyk.Data || name.Data == "CMYK")
            {
                colorspace = ColorSpace.DeviceCMYK;
            }
            else if (name.Data == NameToken.Calgray.Data)
            {
                colorspace = ColorSpace.CalGray;
            }
            else if (name.Data == NameToken.Calrgb.Data)
            {
                colorspace = ColorSpace.CalRGB;
            }
            else if (name.Data == NameToken.Lab.Data)
            {
                colorspace = ColorSpace.Lab;
            }
            else if (name.Data == NameToken.Iccbased.Data)
            {
                colorspace = ColorSpace.ICCBased;
            }
            else if (name.Data == NameToken.Indexed.Data || name.Data == "I")
            {
                colorspace = ColorSpace.Indexed;
            }
            else if (name.Data == NameToken.Pattern.Data)
            {
                colorspace = ColorSpace.Pattern;
            }
            else if (name.Data == NameToken.Separation.Data)
            {
                colorspace = ColorSpace.Separation;
            }
            else if (name.Data == NameToken.Devicen.Data)
            {
                colorspace = ColorSpace.DeviceN;
            }
            else
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the corresponding <see cref="NameToken"/> for a given <see cref="ColorSpace"/>.
        /// </summary>
        public static NameToken ToNameToken(this ColorSpace colorSpace)
        {
            switch (colorSpace)
            {
                case ColorSpace.DeviceGray:
                    return NameToken.Devicegray;
                case ColorSpace.DeviceRGB:
                    return NameToken.Devicergb;
                case ColorSpace.DeviceCMYK:
                    return NameToken.Devicecmyk;
                case ColorSpace.CalGray:
                    return NameToken.Calgray;
                case ColorSpace.CalRGB:
                    return NameToken.Calrgb;
                case ColorSpace.Lab:
                    return NameToken.Lab;
                case ColorSpace.ICCBased:
                    return NameToken.Iccbased;
                case ColorSpace.Indexed:
                    return NameToken.Indexed;
                case ColorSpace.Pattern:
                    return NameToken.Pattern;
                case ColorSpace.Separation:
                    return NameToken.Separation;
                case ColorSpace.DeviceN:
                    return NameToken.Devicen;
                default:
                    throw new ArgumentException($"Unrecognized colorspace: {colorSpace}.");
            }
        }
    }
}