namespace UglyToad.PdfPig.Graphics.Colors
{
    using System;
    using UglyToad.PdfPig.Functions;

    /// <summary>
    /// <see cref="ColorSpaceDetails"/> that have a tint function: <see cref="SeparationColorSpaceDetails"/> and <see cref="DeviceNColorSpaceDetails"/>.
    /// </summary>
    internal static class TintColorSpaceDetailsHelper
    {
        /// <summary>
        /// Evaluate <paramref name="tintInput"/> through a tint <paramref name="tint"/> function and write exactly
        /// <paramref name="alternate"/>'s <see cref="NumberOfColorComponents"/> output values into
        /// <paramref name="buffer"/>, which must be at least <see cref="TintBufferSize"/> long.
        /// </summary>
        /// <returns>The slice of <paramref name="buffer"/> holding the alternate space's component values.</returns>
        private static ReadOnlySpan<double> EvalTint(PdfFunction tint, ColorSpaceDetails alternate,
            ReadOnlySpan<double> tintInput, Span<double> buffer)
        {
            int alternateComponents = alternate.NumberOfColorComponents;
            int written = tint.Eval(tintInput, buffer);

            if (written < alternateComponents)
            {
                // A buggy tint function under-filled the buffer. Zero the trailing slots so the alternate
                // space doesn't read uninitialised stack memory. A tint function that over-fills is trimmed
                // by the slice below, so the alternate space always sees the component count it expects.
                buffer.Slice(written, alternateComponents - written).Clear();
            }

            return buffer.Slice(0, alternateComponents);
        }

        /// <summary>
        /// Evaluate <paramref name="tintInput"/> through a tint <paramref name="tint"/> function whose output values
        /// are then mapped to RGB by <paramref name="alternate"/>'s <see cref="GetRgb"/>. Allocation-free for the
        /// typical case where the alternate colour space has at most <see cref="MaxStackallocComponents"/> components.
        /// </summary>
        public static void GetRgbViaTint(PdfFunction tint, ColorSpaceDetails alternate,
            ReadOnlySpan<double> tintInput, out double r, out double g, out double b)
        {
            int max = Math.Max(tint.MaxOutputComponentCount, alternate.NumberOfColorComponents);
            Span<double> buffer = max <= 32 ? stackalloc double[max] : new double[max];
            alternate.GetRgb(EvalTint(tint, alternate, tintInput, buffer), out r, out g, out b);
        }

        /// <summary>
        /// Evaluate <paramref name="tintInput"/> through a tint <paramref name="tint"/> function whose output values
        /// are then mapped to a colour by <paramref name="alternate"/>'s <see cref="GetColor"/>. The
        /// <see cref="GetRgb"/> equivalent is <see cref="GetRgbViaTint"/>; the two must stay in step so that a
        /// colour space renders the same whichever path it is reached through.
        /// </summary>
        public static IColor GetColorViaTint(PdfFunction tint, ColorSpaceDetails alternate,
            ReadOnlySpan<double> tintInput)
        {
            int max = Math.Max(tint.MaxOutputComponentCount, alternate.NumberOfColorComponents);
            Span<double> buffer = max <= 32 ? stackalloc double[max] : new double[max];
            return alternate.GetColor(EvalTint(tint, alternate, tintInput, buffer));
        }
    }
}
