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

        public void SetStrokingColorspace(NameToken colorspace, DictionaryToken dictionary = null)
        {
            CurrentStrokingColorSpace = resourceStore.GetColorSpaceDetails(colorspace, dictionary);
            if (CurrentStrokingColorSpace is UnsupportedColorSpaceDetails)
            {
                return;
            }

            currentStateFunc().CurrentStrokingColor = CurrentStrokingColorSpace.GetInitializeColor();
        }

        public void SetStrokingColor(IReadOnlyList<double> operands, NameToken patternName)
        {
            if (CurrentStrokingColorSpace is UnsupportedColorSpaceDetails)
            {
                return;
            }

            if (patternName != null && CurrentStrokingColorSpace.Type == ColorSpace.Pattern)
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
            CurrentStrokingColorSpace = DeviceGrayColorSpaceDetails.Instance;
            currentStateFunc().CurrentStrokingColor = CurrentStrokingColorSpace.GetColor(gray);
        }

        public void SetStrokingColorRgb(double r, double g, double b)
        {
            CurrentStrokingColorSpace = DeviceRgbColorSpaceDetails.Instance;
            currentStateFunc().CurrentStrokingColor = CurrentStrokingColorSpace.GetColor(r, g, b);
        }

        public void SetStrokingColorCmyk(double c, double m, double y, double k)
        {
            CurrentStrokingColorSpace = DeviceCmykColorSpaceDetails.Instance;
            currentStateFunc().CurrentStrokingColor = CurrentStrokingColorSpace.GetColor(c, m, y, k);
        }

        public void SetNonStrokingColorspace(NameToken colorspace, DictionaryToken dictionary = null)
        {
            CurrentNonStrokingColorSpace = resourceStore.GetColorSpaceDetails(colorspace, dictionary);
            if (CurrentNonStrokingColorSpace is UnsupportedColorSpaceDetails)
            {
                return;
            }

            currentStateFunc().CurrentNonStrokingColor = CurrentNonStrokingColorSpace.GetInitializeColor();
        }

        public void SetNonStrokingColor(IReadOnlyList<double> operands, NameToken patternName)
        {
            if (CurrentNonStrokingColorSpace is UnsupportedColorSpaceDetails)
            {
                return;
            }

            if (patternName != null && CurrentNonStrokingColorSpace.Type == ColorSpace.Pattern)
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
            CurrentNonStrokingColorSpace = DeviceGrayColorSpaceDetails.Instance;
            currentStateFunc().CurrentNonStrokingColor = CurrentNonStrokingColorSpace.GetColor(gray);
        }

        public void SetNonStrokingColorRgb(double r, double g, double b)
        {
            CurrentNonStrokingColorSpace = DeviceRgbColorSpaceDetails.Instance;
            currentStateFunc().CurrentNonStrokingColor = CurrentNonStrokingColorSpace.GetColor(r, g, b);
        }

        public void SetNonStrokingColorCmyk(double c, double m, double y, double k)
        {
            CurrentNonStrokingColorSpace = DeviceCmykColorSpaceDetails.Instance;
            currentStateFunc().CurrentNonStrokingColor = CurrentNonStrokingColorSpace.GetColor(c, m, y, k);
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
