namespace UglyToad.PdfPig.Graphics
{
    using Colors;
    using Tokens;

    internal class ColorSpaceContext : IColorSpaceContext
    {
        public ColorSpace CurrentStrokingColorSpace { get; private set; } = ColorSpace.DeviceGray;
        public ColorSpace CurrentNonStrokingColorSpace { get; private set; } = ColorSpace.DeviceGray;

        public IColor CurrentStrokingColor { get; private set; } = GrayColor.Black;
        public IColor CurrentNonStrokingColor { get; private set; } = GrayColor.Black;

        public void SetStrokingColorspace(NameToken colorspace)
        {
            if (colorspace.TryMapToColorSpace(out var colorspaceActual))
            {
                CurrentStrokingColorSpace = colorspaceActual;
                switch (colorspaceActual)
                {
                    case ColorSpace.DeviceGray:
                        CurrentStrokingColor = GrayColor.Black;
                        break;
                    case ColorSpace.DeviceRGB:
                        CurrentStrokingColor = RGBColor.Black;
                        break;
                    case ColorSpace.DeviceCMYK:
                        CurrentStrokingColor = CMYKColor.Black;
                        break;
                    default:
                        CurrentStrokingColor = GrayColor.Black;
                        break;
                }
            }
            else
            {
                CurrentStrokingColorSpace = ColorSpace.DeviceGray;
                CurrentStrokingColor = GrayColor.Black;
            }
        }

        public void SetNonStrokingColorspace(NameToken colorspace)
        {
            if (colorspace.TryMapToColorSpace(out var colorspaceActual))
            {
                CurrentNonStrokingColorSpace = colorspaceActual;
                switch (colorspaceActual)
                {
                    case ColorSpace.DeviceGray:
                        CurrentNonStrokingColor = GrayColor.Black;
                        break;
                    case ColorSpace.DeviceRGB:
                        CurrentNonStrokingColor = RGBColor.Black;
                        break;
                    case ColorSpace.DeviceCMYK:
                        CurrentNonStrokingColor = CMYKColor.Black;
                        break;
                    default:
                        CurrentNonStrokingColor = GrayColor.Black;
                        break;
                }
            }
            else
            {
                CurrentNonStrokingColorSpace = ColorSpace.DeviceGray;
                CurrentNonStrokingColor = GrayColor.Black;
            }
        }

        public void SetStrokingColorGray(decimal gray)
        {
            CurrentStrokingColorSpace = ColorSpace.DeviceGray;
            CurrentStrokingColor = new GrayColor(gray);
        }

        public void SetStrokingColorRgb(decimal r, decimal g, decimal b)
        {
            CurrentStrokingColorSpace = ColorSpace.DeviceRGB;
            CurrentStrokingColor = new RGBColor(r, g, b);
        }

        public void SetStrokingColorCmyk(decimal c, decimal m, decimal y, decimal k)
        {
            CurrentStrokingColorSpace = ColorSpace.DeviceCMYK;
            CurrentStrokingColor = new CMYKColor(c, m, y, k);
        }

        public void SetNonStrokingColorGray(decimal gray)
        {
            CurrentNonStrokingColorSpace = ColorSpace.DeviceGray;
            CurrentNonStrokingColor = new GrayColor(gray);
        }

        public void SetNonStrokingColorRgb(decimal r, decimal g, decimal b)
        {
            CurrentNonStrokingColorSpace = ColorSpace.DeviceRGB;
            CurrentNonStrokingColor = new RGBColor(r, g, b);
        }

        public void SetNonStrokingColorCmyk(decimal c, decimal m, decimal y, decimal k)
        {
            CurrentNonStrokingColorSpace = ColorSpace.DeviceCMYK;
            CurrentNonStrokingColor = new CMYKColor(c, m, y, k);
        }
    }
}