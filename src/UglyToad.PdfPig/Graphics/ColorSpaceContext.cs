namespace UglyToad.PdfPig.Graphics
{
    using System;
    using System.Diagnostics;
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

            var state = currentStateFunc();

            // No operands: the colour space derives its own initial colour. A Pattern colour space has none
            // and answers null, which this has always stored as-is; scn supplies the colour for that space.
            state.SetStrokingColor(CurrentStrokingColorSpace, null);
        }

        public void SetStrokingColor(double[] operands, NameToken? patternName)
        {
            if (CurrentStrokingColorSpace is UnsupportedColorSpaceDetails)
            {
                return;
            }

            var state = currentStateFunc();
            if (patternName is not null && CurrentStrokingColorSpace is PatternColorSpaceDetails patternCs)
            {
                Debug.Assert(CurrentStrokingColorSpace.Type == ColorSpace.Pattern);

                // The operands travel with the pattern: an uncoloured tiling pattern (/PaintType 2) paints
                // its cell in the colour they select from the underlying colour space (8.7.3.3), read back
                // through CurrentGraphicsState.CurrentStrokingUnderlyingColor.
                state.SetStrokingPatternColor(patternCs, patternName, operands);
            }
            else
            {
                state.SetStrokingColor(CurrentStrokingColorSpace, operands);
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

            var state = currentStateFunc();

            // No operands: the colour space derives its own initial colour. A Pattern colour space has none
            // and answers null, which this has always stored as-is; scn supplies the colour for that space.
            state.SetNonStrokingColor(CurrentNonStrokingColorSpace, null);
        }

        public void SetNonStrokingColor(double[] operands, NameToken? patternName)
        {
            if (CurrentNonStrokingColorSpace is UnsupportedColorSpaceDetails)
            {
                return;
            }

            var state = currentStateFunc();
            if (patternName is not null && CurrentNonStrokingColorSpace is PatternColorSpaceDetails patternCs)
            {
                Debug.Assert(CurrentNonStrokingColorSpace.Type == ColorSpace.Pattern);

                // See the stroking counterpart: the operands select the uncoloured tiling pattern's colour.
                state.SetNonStrokingPatternColor(patternCs, patternName, operands);
            }
            else
            {
                state.SetNonStrokingColor(CurrentNonStrokingColorSpace, operands);
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

            if (stroking)
            {
                CurrentStrokingColorSpace = colorSpace;

                if (colorSpace.RenderingIntentAffectsOutput)
                {
                    // Only allocate operand here, because the graphics state has to keep something to reconvert from
                    state.SetStrokingColor(colorSpace, values.ToArray());
                }
                else
                {
                    // The intent is still passed, even though it does not affect the output.
                    // It is unconditionally the right value here.
                    state.SetStrokingColor(colorSpace.GetColor(values, state.RenderingIntent));
                }
            }
            else
            {
                CurrentNonStrokingColorSpace = colorSpace;

                if (colorSpace.RenderingIntentAffectsOutput)
                {
                    // See the stroking counterpart.
                    state.SetNonStrokingColor(colorSpace, values.ToArray());
                }
                else
                {
                    // See the stroking counterpart for why the intent is passed here.
                    state.SetNonStrokingColor(colorSpace.GetColor(values, state.RenderingIntent));
                }
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
