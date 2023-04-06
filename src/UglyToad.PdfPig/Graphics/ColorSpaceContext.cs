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

        public ColorSpaceDetails CurrentNonStrokingColorSpace { get; set; } = DeviceGrayColorSpaceDetails.Instance;

        public ColorSpaceContext(Func<CurrentGraphicsState> currentStateFunc, IResourceStore resourceStore)
        {
            this.currentStateFunc = currentStateFunc ?? throw new ArgumentNullException(nameof(currentStateFunc));
            this.resourceStore = resourceStore ?? throw new ArgumentNullException(nameof(resourceStore));
        }

        public void SetStrokingColorspace(NameToken colorspace)
        {
            CurrentStrokingColorSpace = resourceStore.GetColorSpaceDetails(colorspace, null);
            if (CurrentStrokingColorSpace is UnsupportedColorSpaceDetails)
            {
                return;
            }

            currentStateFunc().CurrentStrokingColor = CurrentStrokingColorSpace.GetInitializeColor();
        }

        public void SetStrokingColor(IReadOnlyList<decimal> operands, NameToken patternName)
        {
            if (CurrentStrokingColorSpace is UnsupportedColorSpaceDetails)
            {
                return;
            }

            currentStateFunc().CurrentStrokingColor = CurrentStrokingColorSpace.GetColor(operands.Select(v => (double)v).ToArray());
        }

        public void SetStrokingColorGray(decimal gray)
        {
            CurrentStrokingColorSpace = DeviceGrayColorSpaceDetails.Instance;
            currentStateFunc().CurrentStrokingColor = CurrentStrokingColorSpace.GetColor((double)gray);
        }

        public void SetStrokingColorRgb(decimal r, decimal g, decimal b)
        {
            CurrentStrokingColorSpace = DeviceRgbColorSpaceDetails.Instance;
            currentStateFunc().CurrentStrokingColor = CurrentStrokingColorSpace.GetColor((double)r, (double)g, (double)b);
        }

        public void SetStrokingColorCmyk(decimal c, decimal m, decimal y, decimal k)
        {
            CurrentStrokingColorSpace = DeviceCmykColorSpaceDetails.Instance;
            currentStateFunc().CurrentStrokingColor = CurrentStrokingColorSpace.GetColor((double)c, (double)m, (double)y, (double)k);
        }

        public void SetNonStrokingColorspace(NameToken colorspace)
        {
            CurrentNonStrokingColorSpace = resourceStore.GetColorSpaceDetails(colorspace, null);
            if (CurrentNonStrokingColorSpace is UnsupportedColorSpaceDetails)
            {
                return;
            }

            currentStateFunc().CurrentNonStrokingColor = CurrentNonStrokingColorSpace.GetInitializeColor();
        }

        public void SetNonStrokingColor(IReadOnlyList<decimal> operands, NameToken patternName)
        {
            if (CurrentNonStrokingColorSpace is UnsupportedColorSpaceDetails)
            {
                return;
            }

            currentStateFunc().CurrentNonStrokingColor = CurrentNonStrokingColorSpace.GetColor(operands.Select(v => (double)v).ToArray());
        }

        public void SetNonStrokingColorGray(decimal gray)
        {
            CurrentNonStrokingColorSpace = DeviceGrayColorSpaceDetails.Instance;
            currentStateFunc().CurrentNonStrokingColor = CurrentNonStrokingColorSpace.GetColor((double)gray);
        }

        public void SetNonStrokingColorRgb(decimal r, decimal g, decimal b)
        {
            CurrentNonStrokingColorSpace = DeviceRgbColorSpaceDetails.Instance;
            currentStateFunc().CurrentNonStrokingColor = CurrentNonStrokingColorSpace.GetColor((double)r, (double)g, (double)b);
        }

        public void SetNonStrokingColorCmyk(decimal c, decimal m, decimal y, decimal k)
        {
            CurrentNonStrokingColorSpace = DeviceCmykColorSpaceDetails.Instance;
            currentStateFunc().CurrentNonStrokingColor = CurrentNonStrokingColorSpace.GetColor((double)c, (double)m, (double)y, (double)k);
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