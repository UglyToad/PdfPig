namespace UglyToad.PdfPig.Graphics.Colors
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A color with alpha then base color
    /// </summary>
    public class AlphaColor : IColor, IEquatable<AlphaColor>
    {
        /// <summary>
        /// The alpha value between 0 and 1.
        /// </summary>
        public decimal A { get; }

        /// <summary>
        /// Base color without alpha.
        /// </summary>
        public IColor baseColor { get; }

        /// <summary>
        /// Create a new <see cref="AlphaColor"/>.
        /// </summary>
        /// <param name="a">The alpha value between 0 and 1.</param>
        /// <param name="baseColor">The base color without alpha.</param>

        public AlphaColor(decimal a, IColor baseColor)  
        {
            A = a;        
            this.baseColor = baseColor;
        }

        /// <inheritdoc/>
        public ColorSpace ColorSpace { get { return baseColor.ColorSpace; } }

        /// <inheritdoc/>
        public (decimal r, decimal g, decimal b) ToRGBValues()
        {
            var values = baseColor.ToRGBValues();
            return values;
        }



        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj.GetType() == baseColor.GetType())
            {
                return Equals(obj);
            }

            return false;
        }

        /// <inheritdoc />
        /// <summary>
        /// Whether 2 RGB colors are equal across all channels.
        /// </summary>
        public bool Equals(AlphaColor other)
        {
            if (other is null) return false;             
            if (A != other.A) return false;
            var values = baseColor.ToRGBValues();
            var valuesOther = baseColor.ToRGBValues();
            return                    
                   valuesOther.r == values.r &&
                   valuesOther.g == values.g &&
                   valuesOther.b == values.b;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            var values = baseColor.ToRGBValues();
            return (A, values.r, values.g, values.b).GetHashCode();
        }

        /// <summary>
        /// Equals.
        /// </summary>
        public static bool operator ==(AlphaColor color1, AlphaColor color2) =>
            EqualityComparer<AlphaColor>.Default.Equals(color1, color2);

        /// <summary>
        /// Not Equals.
        /// </summary>
        public static bool operator !=(AlphaColor color1, AlphaColor color2) => !(color1 == color2);

        /// <inheritdoc />
        public override string ToString()
        {
            var values = baseColor.ToRGBValues();
            return $"ARGB: ({A}, {values.r}, {values.g}, {values.b})";
        }
    }
}