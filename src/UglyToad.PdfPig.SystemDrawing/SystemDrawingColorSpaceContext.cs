using System;
using System.Drawing;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Graphics;
using UglyToad.PdfPig.Graphics.Colors;
using UglyToad.PdfPig.Tokens;

namespace UglyToad.PdfPig.SystemDrawing
{
    public class SystemDrawingColorSpaceContext : IColorSpaceContext
    {
        private readonly Func<CurrentSystemGraphicsState> currentStateFunc;
        private readonly IResourceStore resourceStore;

        public ColorSpace CurrentStrokingColorSpace { get; private set; } = ColorSpace.DeviceGray;

        public ColorSpace CurrentNonStrokingColorSpace { get; private set; } = ColorSpace.DeviceGray;

        public NameToken AdvancedStrokingColorSpace { get; private set; }

        public NameToken AdvancedNonStrokingColorSpace { get; private set; }


        public SystemDrawingColorSpaceContext(Func<CurrentSystemGraphicsState> currentStateFunc, IResourceStore resourceStore)
        {
            this.currentStateFunc = currentStateFunc ?? throw new ArgumentNullException(nameof(currentStateFunc));
            this.resourceStore = resourceStore ?? throw new ArgumentNullException(nameof(resourceStore));
        }

        public void SetStrokingColorspace(NameToken colorspace)
        {
            void DefaultColorSpace(ColorSpace? colorSpaceActual = null)
            {
                if (colorSpaceActual.HasValue)
                {
                    switch (colorSpaceActual)
                    {
                        case ColorSpace.DeviceGray:
                            currentStateFunc().CurrentStrokingColor = Color.Black; // GrayColor.Black;
                            break;
                        case ColorSpace.DeviceRGB:
                            currentStateFunc().CurrentStrokingColor = Color.Black; // RGBColor.Black;
                            break;
                        case ColorSpace.DeviceCMYK:
                            currentStateFunc().CurrentStrokingColor = Color.Black; // CMYKColor.Black;
                            break;
                        default:
                            currentStateFunc().CurrentStrokingColor = Color.Black; // GrayColor.Black;
                            break;
                    }
                }
                else
                {

                    CurrentStrokingColorSpace = ColorSpace.DeviceGray;
                    currentStateFunc().CurrentStrokingColorSpace = ColorSpace.DeviceGray;
                    currentStateFunc().CurrentStrokingColor = Color.Black; // GrayColor.Black;
                }
            }

            AdvancedStrokingColorSpace = null;

            if (colorspace.TryMapToColorSpace(out var colorspaceActual))
            {
                CurrentStrokingColorSpace = colorspaceActual;
                currentStateFunc().CurrentStrokingColorSpace = colorspaceActual;
            }
            else if (resourceStore.TryGetNamedColorSpace(colorspace, out var namedColorSpace))
            {
                if (namedColorSpace.Name == NameToken.Separation && namedColorSpace.Data is ArrayToken separationArray
                                                                 && separationArray.Length == 4
                                                                 && separationArray[2] is NameToken alternativeColorSpaceName
                                                                 && alternativeColorSpaceName.TryMapToColorSpace(out colorspaceActual))
                {
                    AdvancedStrokingColorSpace = namedColorSpace.Name;
                    CurrentStrokingColorSpace = colorspaceActual;
                    currentStateFunc().CurrentStrokingColorSpace = colorspaceActual;
                    DefaultColorSpace(colorspaceActual);
                }
                else
                {
                    DefaultColorSpace();
                }
            }
            else
            {
                DefaultColorSpace();
            }
        }

        public void SetNonStrokingColorspace(NameToken colorspace)
        {
            void DefaultColorSpace(ColorSpace? colorSpaceActual = null)
            {
                if (colorSpaceActual.HasValue)
                {
                    switch (colorSpaceActual)
                    {
                        case ColorSpace.DeviceGray:
                            currentStateFunc().CurrentNonStrokingColor = Color.Black; // GrayColor.Black;
                            break;
                        case ColorSpace.DeviceRGB:
                            currentStateFunc().CurrentNonStrokingColor = Color.Black; // RGBColor.Black;
                            break;
                        case ColorSpace.DeviceCMYK:
                            currentStateFunc().CurrentNonStrokingColor = Color.Black; // CMYKColor.Black;
                            break;
                        default:
                            currentStateFunc().CurrentNonStrokingColor = Color.Black; // GrayColor.Black;
                            break;
                    }
                }
                else
                {
                    CurrentNonStrokingColorSpace = ColorSpace.DeviceGray;
                    currentStateFunc().CurrentNonStrokingColorSpace = ColorSpace.DeviceGray;
                    currentStateFunc().CurrentNonStrokingColor = Color.Black; // GrayColor.Black;
                }
            }

            AdvancedNonStrokingColorSpace = null;

            if (colorspace.TryMapToColorSpace(out var colorspaceActual))
            {
                CurrentNonStrokingColorSpace = colorspaceActual;
                currentStateFunc().CurrentNonStrokingColorSpace = colorspaceActual;
            }
            else if (resourceStore.TryGetNamedColorSpace(colorspace, out var namedColorSpace))
            {
                if (namedColorSpace.Name == NameToken.Separation && namedColorSpace.Data is ArrayToken separationArray
                                                                 && separationArray.Length == 4
                                                                 && separationArray[2] is NameToken alternativeColorSpaceName
                                                                 && alternativeColorSpaceName.TryMapToColorSpace(out colorspaceActual))
                {
                    AdvancedNonStrokingColorSpace = namedColorSpace.Name;
                    CurrentNonStrokingColorSpace = colorspaceActual;
                    currentStateFunc().CurrentNonStrokingColorSpace = colorspaceActual;
                    DefaultColorSpace(colorspaceActual);
                }
                else
                {
                    DefaultColorSpace();
                }
            }
            else
            {
                DefaultColorSpace();
            }
        }

        public void SetStrokingColorGray(decimal gray)
        {
            CurrentStrokingColorSpace = ColorSpace.DeviceGray;
            currentStateFunc().CurrentStrokingColorSpace = ColorSpace.DeviceGray;

            if (gray == 0)
            {
                currentStateFunc().CurrentStrokingColor = Color.Black; // GrayColor.Black;
            }
            else if (gray == 1)
            {
                currentStateFunc().CurrentStrokingColor = Color.White; // GrayColor.White;
            }
            else
            {
                currentStateFunc().CurrentStrokingColor = new GrayColor(gray).ToSystemColor();
            }
        }

        public void SetStrokingColorRgb(decimal r, decimal g, decimal b)
        {
            CurrentStrokingColorSpace = ColorSpace.DeviceRGB;
            currentStateFunc().CurrentStrokingColorSpace = ColorSpace.DeviceRGB;

            if (r == 0 && g == 0 && b == 0)
            {
                currentStateFunc().CurrentStrokingColor = Color.Black; // RGBColor.Black;
            }
            else if (r == 1 && g == 1 && b == 1)
            {
                currentStateFunc().CurrentStrokingColor = Color.White; // RGBColor.White;
            }
            else
            {
                currentStateFunc().CurrentStrokingColor = new RGBColor(r, g, b).ToSystemColor();
            }
        }

        public void SetStrokingColorCmyk(decimal c, decimal m, decimal y, decimal k)
        {
            CurrentStrokingColorSpace = ColorSpace.DeviceCMYK;
            currentStateFunc().CurrentStrokingColorSpace = ColorSpace.DeviceCMYK;
            currentStateFunc().CurrentStrokingColor = new CMYKColor(c, m, y, k).ToSystemColor();
        }

        public void SetNonStrokingColorGray(decimal gray)
        {
            CurrentNonStrokingColorSpace = ColorSpace.DeviceGray;
            currentStateFunc().CurrentNonStrokingColorSpace = ColorSpace.DeviceGray;

            if (gray == 0)
            {
                currentStateFunc().CurrentNonStrokingColor = Color.Black; // GrayColor.Black;
            }
            else if (gray == 1)
            {
                currentStateFunc().CurrentNonStrokingColor = Color.White; // GrayColor.White;
            }
            else
            {
                currentStateFunc().CurrentNonStrokingColor = new GrayColor(gray).ToSystemColor();
            }
        }

        public void SetNonStrokingColorRgb(decimal r, decimal g, decimal b)
        {
            CurrentNonStrokingColorSpace = ColorSpace.DeviceRGB;
            currentStateFunc().CurrentNonStrokingColorSpace = ColorSpace.DeviceRGB;

            if (r == 0 && g == 0 && b == 0)
            {
                currentStateFunc().CurrentNonStrokingColor = Color.Black; // RGBColor.Black;
            }
            else if (r == 1 && g == 1 && b == 1)
            {
                currentStateFunc().CurrentNonStrokingColor = Color.White; // RGBColor.White;
            }
            else
            {
                currentStateFunc().CurrentNonStrokingColor = new RGBColor(r, g, b).ToSystemColor();
            }
        }

        public void SetNonStrokingColorCmyk(decimal c, decimal m, decimal y, decimal k)
        {
            CurrentNonStrokingColorSpace = ColorSpace.DeviceCMYK;
            currentStateFunc().CurrentNonStrokingColorSpace = ColorSpace.DeviceCMYK;
            currentStateFunc().CurrentNonStrokingColor = new CMYKColor(c, m, y, k).ToSystemColor();
        }
    }
}
