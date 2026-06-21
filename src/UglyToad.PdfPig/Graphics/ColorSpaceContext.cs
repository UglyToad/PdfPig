namespace UglyToad.PdfPig.Graphics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Colors;
    using Content;
    using Tokens;

    internal class ColorSpaceContext : IColorSpaceContext
    {
        private readonly Func<CurrentGraphicsState> currentStateFunc;
        private readonly IResourceStore resourceStore;

        public ColorSpaceDetails CurrentStrokingColorSpace { get; private set; } = DeviceGrayColorSpaceDetails.Instance;

        public ColorSpaceDetails CurrentNonStrokingColorSpace { get; private set; } = DeviceGrayColorSpaceDetails.Instance;

        public ColorSpaceContext(Func<CurrentGraphicsState> currentStateFunc, IResourceStore resourceStore)
        {
            this.currentStateFunc = currentStateFunc ?? throw new ArgumentNullException(nameof(currentStateFunc));
            this.resourceStore = resourceStore ?? throw new ArgumentNullException(nameof(resourceStore));
        }

        public void SetStrokingColorspace(NameToken colorspace, DictionaryToken? dictionary = null)
        {
            CurrentStrokingColorSpace = resourceStore.GetColorSpaceDetails(colorspace, dictionary);
            if (CurrentStrokingColorSpace is UnsupportedColorSpaceDetails)
            {
                return;
            }

            currentStateFunc().CurrentStrokingColor = CurrentStrokingColorSpace.GetInitializeColor();
        }

        public void SetStrokingColor(IReadOnlyList<double> operands, NameToken? patternName)
        {
            if (CurrentStrokingColorSpace is UnsupportedColorSpaceDetails)
            {
                return;
            }

            if (patternName is not null && CurrentStrokingColorSpace.Type == ColorSpace.Pattern)
            {
                currentStateFunc().CurrentStrokingColor = ((PatternColorSpaceDetails)CurrentStrokingColorSpace).GetColor(patternName);
                // TODO - use operands values for Uncoloured Tiling Patterns
            }
            else
            {
                currentStateFunc().CurrentStrokingColor = CurrentStrokingColorSpace.GetColor(operands.ToArray());
            }
        }

        public void SetStrokingColorGray(double gray)
        {
            SetDeviceColor(ColorSpace.DeviceGray, [gray], stroking: true);
        }

        public void SetStrokingColorRgb(double r, double g, double b)
        {
            SetDeviceColor(ColorSpace.DeviceRGB, [r, g, b], stroking: true);
        }

        public void SetStrokingColorCmyk(double c, double m, double y, double k)
        {
            SetDeviceColor(ColorSpace.DeviceCMYK, [c, m, y, k], stroking: true);
        }

        public void SetNonStrokingColorspace(NameToken colorspace, DictionaryToken? dictionary = null)
        {
            CurrentNonStrokingColorSpace = resourceStore.GetColorSpaceDetails(colorspace, dictionary);
            if (CurrentNonStrokingColorSpace is UnsupportedColorSpaceDetails)
            {
                return;
            }

            currentStateFunc().CurrentNonStrokingColor = CurrentNonStrokingColorSpace.GetInitializeColor();
        }

        public void SetNonStrokingColor(IReadOnlyList<double> operands, NameToken? patternName)
        {
            if (CurrentNonStrokingColorSpace is UnsupportedColorSpaceDetails)
            {
                return;
            }

            if (patternName is not null && CurrentNonStrokingColorSpace.Type == ColorSpace.Pattern)
            {
                currentStateFunc().CurrentNonStrokingColor = ((PatternColorSpaceDetails)CurrentNonStrokingColorSpace).GetColor(patternName);
                // TODO - use operands values for Uncoloured Tiling Patterns
            }
            else
            {
                currentStateFunc().CurrentNonStrokingColor = CurrentNonStrokingColorSpace.GetColor(operands.ToArray());
            }
        }

        public void SetNonStrokingColorGray(double gray)
        {
            SetDeviceColor(ColorSpace.DeviceGray, [gray], stroking: false);
        }

        public void SetNonStrokingColorRgb(double r, double g, double b)
        {
            SetDeviceColor(ColorSpace.DeviceRGB, [r, g, b], stroking: false);
        }

        public void SetNonStrokingColorCmyk(double c, double m, double y, double k)
        {
            SetDeviceColor(ColorSpace.DeviceCMYK, [c, m, y, k], stroking: false);
        }

        /// <summary>
        /// Set a colour selected directly through a device colour operator (<c>g</c>/<c>rg</c>/<c>k</c>
        /// and their stroking variants). Per 8.6.5.6, "Default colour spaces", the device colour space is
        /// first remapped to the corresponding <c>DefaultGray</c>/<c>DefaultRGB</c>/<c>DefaultCMYK</c> space
        /// when one is defined in the current resource dictionary; otherwise the device space is used as-is.
        /// </summary>
        private void SetDeviceColor(ColorSpace deviceColorSpace, ReadOnlySpan<double> values, bool stroking)
        {
            var colorSpace = resourceStore.GetDeviceColorSpaceDetails(deviceColorSpace);
            var state = currentStateFunc();

            IColor color = colorSpace.GetColor(values.ToArray());

            if (stroking)
            {
                CurrentStrokingColorSpace = colorSpace;
                state.CurrentStrokingColor = color;
            }
            else
            {
                CurrentNonStrokingColorSpace = colorSpace;
                state.CurrentNonStrokingColor = color;
            }
        }

        public IColorSpaceContext DeepClone()
        {
            return new ColorSpaceContext(currentStateFunc, resourceStore)
            {
                CurrentStrokingColorSpace = CurrentStrokingColorSpace,
                CurrentNonStrokingColorSpace = CurrentNonStrokingColorSpace
            };
        }
    }
}
