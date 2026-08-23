namespace UglyToad.PdfPig.Graphics
{
    using System;
    using System.Diagnostics;
    using Colors;
    using Colors.Icc;
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
            state.SetStrokingColor(CurrentStrokingColorSpace, null, state.OutputIntentProfile);
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
                // through CurrentGraphicsState.CurrentStrokingUnderlyingColor. That colour is an ordinary
                // device colour, so it is output-intent managed like any other.
                state.SetStrokingPatternColor(patternCs, patternName, operands, state.OutputIntentProfile);
            }
            else
            {
                state.SetStrokingColor(CurrentStrokingColorSpace, operands, state.OutputIntentProfile);
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
            state.SetNonStrokingColor(CurrentNonStrokingColorSpace, null, state.OutputIntentProfile);
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
                state.SetNonStrokingPatternColor(patternCs, patternName, operands,
                    state.OutputIntentProfile);
            }
            else
            {
                state.SetNonStrokingColor(CurrentNonStrokingColorSpace, operands, state.OutputIntentProfile);
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
            }
            else
            {
                CurrentNonStrokingColorSpace = colorSpace;
            }

            var outputIntentProfile = state.OutputIntentProfile;

            // A managed colour varies by intent even when its colour space does not, because the profile
            // resolves its transform per intent - so the operands have to be kept in that case too.
            if (colorSpace.RenderingIntentAffectsOutput || outputIntentProfile is not null)
            {
                // Paying the cost of allocating operands only here: these are the cases where the graphics
                // state keeps them, to reconvert from if the intent moves before the mark is made.
                double[] operands = values.ToArray();

                if (stroking)
                {
                    state.SetStrokingColor(colorSpace, operands, outputIntentProfile);
                }
                else
                {
                    state.SetNonStrokingColor(colorSpace, operands, outputIntentProfile);
                }

                return;
            }

            // The intent is still passed, even though it cannot affect the output; it is unconditionally
            // the right value here.
            var color = colorSpace.GetColor(values, state.RenderingIntent);

            if (stroking)
            {
                state.SetStrokingColor(color);
            }
            else
            {
                state.SetNonStrokingColor(color);
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
