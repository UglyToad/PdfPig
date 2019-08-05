namespace UglyToad.PdfPig.Graphics
{
    using System;
    using Colors;
    using Tokens;

    internal class ColorSpaceContext : IColorSpaceContext
    {
        private readonly Func<CurrentGraphicsState> currentStateFunc;

        public ColorSpace CurrentStrokingColorSpace { get; private set; } = ColorSpace.DeviceGray;

        public ColorSpace CurrentNonStrokingColorSpace { get; private set; } = ColorSpace.DeviceGray;
       
        public ColorSpaceContext(Func<CurrentGraphicsState> currentStateFunc)
        {
            this.currentStateFunc = currentStateFunc ?? throw new ArgumentNullException(nameof(currentStateFunc));
        }

        public void SetStrokingColorspace(NameToken colorspace)
        {
            if (colorspace.TryMapToColorSpace(out var colorspaceActual))
            {
                CurrentStrokingColorSpace = colorspaceActual;
                switch (colorspaceActual)
                {
                    case ColorSpace.DeviceGray:
                        currentStateFunc().CurrentStrokingColor = GrayColor.Black;
                        break;
                    case ColorSpace.DeviceRGB:
                        currentStateFunc().CurrentStrokingColor = RGBColor.Black;
                        break;
                    case ColorSpace.DeviceCMYK:
                        currentStateFunc().CurrentStrokingColor = CMYKColor.Black;
                        break;
                    default:
                        currentStateFunc().CurrentStrokingColor = GrayColor.Black;
                        break;
                }
            }
            else
            {
                CurrentStrokingColorSpace = ColorSpace.DeviceGray;
                currentStateFunc().CurrentStrokingColor = GrayColor.Black;
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
                        currentStateFunc().CurrentNonStrokingColor = GrayColor.Black;
                        break;
                    case ColorSpace.DeviceRGB:
                        currentStateFunc().CurrentNonStrokingColor = RGBColor.Black;
                        break;
                    case ColorSpace.DeviceCMYK:
                        currentStateFunc().CurrentNonStrokingColor = CMYKColor.Black;
                        break;
                    default:
                        currentStateFunc().CurrentNonStrokingColor = GrayColor.Black;
                        break;
                }
            }
            else
            {
                CurrentNonStrokingColorSpace = ColorSpace.DeviceGray;
                currentStateFunc().CurrentNonStrokingColor = GrayColor.Black;
            }
        }

        public void SetStrokingColorGray(decimal gray)
        {
            CurrentStrokingColorSpace = ColorSpace.DeviceGray;
            currentStateFunc().CurrentStrokingColor = new GrayColor(gray);
        }

        public void SetStrokingColorRgb(decimal r, decimal g, decimal b)
        {
            CurrentStrokingColorSpace = ColorSpace.DeviceRGB;
            currentStateFunc().CurrentStrokingColor = new RGBColor(r, g, b);
        }

        public void SetStrokingColorCmyk(decimal c, decimal m, decimal y, decimal k)
        {
            CurrentStrokingColorSpace = ColorSpace.DeviceCMYK;
            currentStateFunc().CurrentStrokingColor = new CMYKColor(c, m, y, k);
        }

        public void SetNonStrokingColorGray(decimal gray)
        {
            CurrentNonStrokingColorSpace = ColorSpace.DeviceGray;
            currentStateFunc().CurrentNonStrokingColor = new GrayColor(gray);
        }

        public void SetNonStrokingColorRgb(decimal r, decimal g, decimal b)
        {
            CurrentNonStrokingColorSpace = ColorSpace.DeviceRGB;
            currentStateFunc().CurrentNonStrokingColor = new RGBColor(r, g, b);
        }

        public void SetNonStrokingColorCmyk(decimal c, decimal m, decimal y, decimal k)
        {
            CurrentNonStrokingColorSpace = ColorSpace.DeviceCMYK;
            currentStateFunc().CurrentNonStrokingColor = new CMYKColor(c, m, y, k);
        }
    }
}