namespace UglyToad.PdfPig.Graphics.Colors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.Tokens;

    /// <summary>
    /// A pattern color.
    /// <para>
    /// Base class for <see cref="TilingPatternColor"/> and <see cref="ShadingPatternColor"/>.
    /// </para>
    /// </summary>
    public abstract class PatternColor : IColor
    {
        /// <summary>
        /// 1 for tiling, 2 for shading.
        /// </summary>
        public PatternType PatternType { get; }

        /// <summary>
        /// The dictionary defining the pattern.
        /// </summary>
        public DictionaryToken PatternDictionary { get; }

        /// <summary>
        /// Graphics state parameter dictionary containing graphics state parameters to be put into effect temporarily while the shading
        /// pattern is painted. Any parameters that are so specified shall be inherited from the graphics state that was in effect at the
        /// beginning of the content stream in which the pattern is defined as a resource.
        /// </summary>
        public DictionaryToken ExtGState { get; }

        /// <summary>
        /// The pattern matrix (see 8.7.2, "General Properties of Patterns"). Default value: the identity matrix [1 0 0 1 0 0].
        /// </summary>
        public TransformationMatrix Matrix { get; }

        /// <inheritdoc/>
        protected internal PatternColor(PatternType patternType, DictionaryToken patternDictionary, DictionaryToken extGState, TransformationMatrix matrix)
        {
            PatternType = patternType;
            PatternDictionary = patternDictionary;
            ExtGState = extGState;
            Matrix = matrix;
        }

        #region IColor
        /// <inheritdoc/>
        public ColorSpace ColorSpace { get; } = ColorSpace.Pattern;

        /// <summary>
        /// <inheritdoc/>
        /// <para>
        /// Cannot be called for <see cref="PatternColor"/>, will throw a <see cref="InvalidOperationException"/>.
        /// </para>
        /// </summary>
        public (double r, double g, double b) ToRGBValues()
        {
            throw new InvalidOperationException("Cannot call ToRGBValues in a Pattern color.");
        }
        #endregion

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Pattern: ({PatternType})";
        }
    }

    /// <summary>
    /// A tiling pattern consists of a small graphical figure called a pattern cell. Painting with the pattern
    /// replicates the cell at fixed horizontal and vertical intervals to fill an area. The effect is as if the
    /// figure were painted on the surface of a clear glass tile, identical copies of which were then laid down
    /// in an array covering the area and trimmed to its boundaries.
    /// </summary>
    public sealed class TilingPatternColor : PatternColor, IEquatable<TilingPatternColor>
    {
        /// <summary>
        /// Content stream containing the painting operators needed to paint one instance of the cell.
        /// </summary>
        public StreamToken PatternStream { get; }

        /// <summary>
        /// A code that determines how the colour of the pattern cell shall be specified.
        /// </summary>
        public PatternPaintType PaintType { get; }

        /// <summary>
        /// A code that controls adjustments to the spacing of tiles relative to the device pixel grid:.
        /// </summary>
        public PatternTilingType TilingType { get; }

        /// <summary>
        /// The pattern cell's bounding box. These boundaries shall be used to clip the pattern cell.
        /// </summary>
        public PdfRectangle BBox { get; }

        /// <summary>
        /// The desired horizontal spacing between pattern cells, measured in the pattern coordinate system.
        /// <para>
        /// XStep and YStep may differ from the dimensions of the pattern cell implied by the BBox entry. This allows tiling with irregularly shaped figures.
        /// XStep and YStep may be either positive or negative but shall not be zero.
        /// </para>
        /// </summary>
        public double XStep { get; }

        /// <summary>
        /// The desired vertical spacing between pattern cells, measured in the pattern coordinate system.
        /// <para>
        /// XStep and YStep may differ from the dimensions of the pattern cell implied by the BBox entry. This allows tiling with irregularly shaped figures.
        /// XStep and YStep may be either positive or negative but shall not be zero.
        /// </para>
        /// </summary>
        public double YStep { get; }

        /// <summary>
        /// A resource dictionary that shall contain all of the named resources required by the pattern's content stream.
        /// </summary>
        public DictionaryToken Resources { get; }

        /// <summary>
        /// Content containing the painting operators needed to paint one instance of the cell.
        /// </summary>
        public IReadOnlyList<byte> Data { get; }

        /// <summary>
        /// Create a new <see cref="TilingPatternColor"/>.
        /// </summary>
        public TilingPatternColor(TransformationMatrix matrix, DictionaryToken extGState, StreamToken patternStream,
            PatternPaintType paintType, PatternTilingType tilingType, PdfRectangle bbox, double xStep, double yStep,
            DictionaryToken resources, IReadOnlyList<byte> data)
            : base(PatternType.Tiling, patternStream.StreamDictionary, extGState, matrix)
        {
            PatternStream = patternStream;
            PaintType = paintType;
            TilingType = tilingType;
            BBox = bbox;
            XStep = xStep;
            YStep = yStep;
            Resources = resources;
            Data = data;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is TilingPatternColor other && Equals(other);
        }

        /// <inheritdoc/>
        public bool Equals(TilingPatternColor other)
        {
            return PatternType.Equals(other.PatternType) &&
                Matrix.Equals(other.Matrix) &&
                ExtGState.Equals(other.ExtGState) &&
                PaintType.Equals(other.PaintType) &&
                TilingType.Equals(other.TilingType) &&
                BBox.Equals(other.BBox) &&
                XStep.Equals(other.XStep) &&
                YStep.Equals(other.YStep) &&
                Resources.Equals(other.Resources) &&
                Data.SequenceEqual(other.Data);
        }

        /// <inheritdoc />
        public override int GetHashCode() => (PatternType, Matrix, ExtGState, PaintType, TilingType, BBox, XStep, YStep, Resources, Data).GetHashCode();

        /// <inheritdoc />
        public override string ToString()
        {
            return base.ToString() + $"[{PaintType}][{TilingType}]";
        }

        /// <summary>
        /// Equals.
        /// </summary>
        public static bool operator ==(TilingPatternColor color1, TilingPatternColor color2) =>
            EqualityComparer<TilingPatternColor>.Default.Equals(color1, color2);

        /// <summary>
        /// Not Equals.
        /// </summary>
        public static bool operator !=(TilingPatternColor color1, TilingPatternColor color2) => !(color1 == color2);
    }

    /// <summary>
    /// Shading patterns provide a smooth transition between colours across an area to be painted, independent of
    /// the resolution of any particular output device and without specifying the number of steps in the colour transition.
    /// </summary>
    public sealed class ShadingPatternColor : PatternColor, IEquatable<ShadingPatternColor>
    {
        /// <summary>
        /// A shading object defining the shading pattern's gradient fill.
        /// </summary>
        public Shading Shading { get; }

        /// <summary>
        /// Create a new <see cref="ShadingPatternColor"/>.
        /// </summary>
        public ShadingPatternColor(TransformationMatrix matrix, DictionaryToken extGState, DictionaryToken patternDictionary, Shading shading)
            : base(PatternType.Shading, patternDictionary, extGState, matrix)
        {
            Shading = shading;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is ShadingPatternColor other && Equals(other);
        }

        /// <inheritdoc/>
        public bool Equals(ShadingPatternColor other)
        {
            return PatternType.Equals(other.PatternType) &&
                   Matrix.Equals(other.Matrix) &&
                   ExtGState.Equals(other.ExtGState) &&
                   Shading.Equals(other.Shading);
        }

        /// <inheritdoc />
        public override int GetHashCode() => (PatternType, Matrix, ExtGState, Shading).GetHashCode();

        /// <summary>
        /// Equals.
        /// </summary>
        public static bool operator ==(ShadingPatternColor color1, ShadingPatternColor color2) =>
            EqualityComparer<ShadingPatternColor>.Default.Equals(color1, color2);

        /// <summary>
        /// Not Equals.
        /// </summary>
        public static bool operator !=(ShadingPatternColor color1, ShadingPatternColor color2) => !(color1 == color2);
    }

    /// <summary>
    /// TODO
    /// </summary>
    public enum PatternType : byte
    {
        /// <summary>
        /// TODO
        /// </summary>
        Tiling = 1,

        /// <summary>
        /// TODO
        /// </summary>
        Shading = 2
    }

    /// <summary>
    /// TODO
    /// </summary>
    public enum PatternPaintType : byte
    {
        /// <summary>
        /// TODO
        /// </summary>
        Coloured = 1,

        /// <summary>
        /// TODO
        /// </summary>
        Uncoloured = 2
    }

    /// <summary>
    /// TODO
    /// </summary>
    public enum PatternTilingType : byte
    {
        /// <summary>
        /// TODO
        /// </summary>
        ConstantSpacing = 1,

        /// <summary>
        /// TODO
        /// </summary>
        NoDistortion = 2,

        /// <summary>
        /// TODO
        /// </summary>
        ConstantSpacingFasterTiling = 3
    }
}
