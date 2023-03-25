namespace UglyToad.PdfPig.Graphics
{
    using System;
    using Colors;
    using Content;
    using Tokens;

    internal class ColorSpaceContext : IColorSpaceContext
    {
        private readonly Func<CurrentGraphicsState> currentStateFunc;
        private readonly IResourceStore resourceStore;

        public ColorSpace CurrentStrokingColorSpace { get; private set; } = ColorSpace.DeviceGray;

        public ColorSpace CurrentNonStrokingColorSpace { get; private set; } = ColorSpace.DeviceGray;

        public NameToken AdvancedStrokingColorSpace { get; private set; }

        public NameToken AdvancedNonStrokingColorSpace { get; private set; }

        public ColorSpaceContext(Func<CurrentGraphicsState> currentStateFunc, IResourceStore resourceStore)
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

            AdvancedStrokingColorSpace = null;

            if (colorspace.TryMapToColorSpace(out var colorspaceActual))
            {
                CurrentStrokingColorSpace = colorspaceActual;
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

            AdvancedNonStrokingColorSpace = null;

            if (colorspace.TryMapToColorSpace(out var colorspaceActual))
            {
                CurrentNonStrokingColorSpace = colorspaceActual;
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

            if (gray == 0)
            {
                currentStateFunc().CurrentStrokingColor = GrayColor.Black;
            }
            else if (gray == 1)
            {
                currentStateFunc().CurrentStrokingColor = GrayColor.White;
            }
            else
            {
                currentStateFunc().CurrentStrokingColor = new GrayColor(gray);
            }
        }

        public void SetStrokingColorRgb(decimal r, decimal g, decimal b)
        {
            CurrentStrokingColorSpace = ColorSpace.DeviceRGB;

            if (r == 0 && g == 0 && b == 0)
            {
                currentStateFunc().CurrentStrokingColor = RGBColor.Black;
            }
            else if (r == 1 && g == 1 && b == 1)
            {
                currentStateFunc().CurrentStrokingColor = RGBColor.White;
            }
            else
            {
                currentStateFunc().CurrentStrokingColor = new RGBColor(r, g, b);
            }
        }

        public void SetStrokingColorCmyk(decimal c, decimal m, decimal y, decimal k)
        {
            CurrentStrokingColorSpace = ColorSpace.DeviceCMYK;
            currentStateFunc().CurrentStrokingColor = new CMYKColor(c, m, y, k);
        }

        public void SetNonStrokingColorGray(decimal gray)
        {
            CurrentNonStrokingColorSpace = ColorSpace.DeviceGray;

            if (gray == 0)
            {
                currentStateFunc().CurrentNonStrokingColor = GrayColor.Black;
            }
            else if (gray == 1)
            {
                currentStateFunc().CurrentNonStrokingColor = GrayColor.White;
            }
            else
            {
                currentStateFunc().CurrentNonStrokingColor = new GrayColor(gray);
            }
        }

        public void SetNonStrokingColorRgb(decimal r, decimal g, decimal b)
        {
            CurrentNonStrokingColorSpace = ColorSpace.DeviceRGB;

            if (r == 0 && g == 0 && b == 0)
            {
                currentStateFunc().CurrentNonStrokingColor = RGBColor.Black;
            }
            else if (r == 1 && g == 1 && b == 1)
            {
                currentStateFunc().CurrentNonStrokingColor = RGBColor.White;
            }
            else
            {
                currentStateFunc().CurrentNonStrokingColor = new RGBColor(r, g, b);
            }
        }

        public void SetNonStrokingColorCmyk(decimal c, decimal m, decimal y, decimal k)
        {
            CurrentNonStrokingColorSpace = ColorSpace.DeviceCMYK;
            currentStateFunc().CurrentNonStrokingColor = new CMYKColor(c, m, y, k);
        }

        public IColorSpaceContext DeepClone()
        {
            return new ColorSpaceContext(currentStateFunc, resourceStore)
            {
                CurrentStrokingColorSpace = CurrentStrokingColorSpace,
                CurrentNonStrokingColorSpace = CurrentNonStrokingColorSpace,
                AdvancedStrokingColorSpace = AdvancedStrokingColorSpace,
                AdvancedNonStrokingColorSpace = AdvancedNonStrokingColorSpace
            };
        }
    }
}