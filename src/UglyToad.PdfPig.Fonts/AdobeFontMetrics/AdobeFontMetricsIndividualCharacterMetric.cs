namespace UglyToad.PdfPig.Fonts.AdobeFontMetrics
{
    using Core;

    /// <summary>
    /// The metrics for an individual character.
    /// </summary>
    public class AdobeFontMetricsIndividualCharacterMetric
    {
        /// <summary>
        /// Character code.
        /// </summary>
        public int CharacterCode { get; }

        /// <summary>
        /// PostScript language character name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Width.
        /// </summary>
        public AdobeFontMetricsVector Width { get; }

        /// <summary>
        /// Width for writing direction 0.
        /// </summary>
        public AdobeFontMetricsVector WidthDirection0 { get; }

        /// <summary>
        /// Width for writing direction 1.
        /// </summary>
        public AdobeFontMetricsVector WidthDirection1 { get; }
        
        /// <summary>
        /// Vector from origin of writing direction 1 to origin of writing direction 0.
        /// </summary>
        public AdobeFontMetricsVector VVector { get; }

        /// <summary>
        /// Character bounding box.
        /// </summary>
        public PdfRectangle BoundingBox { get; }

        /// <summary>
        /// Ligature information.
        /// </summary>
        public AdobeFontMetricsLigature Ligature { get; }

        /// <summary>
        /// Create a new <see cref="AdobeFontMetricsIndividualCharacterMetric"/>.
        /// </summary>
        public AdobeFontMetricsIndividualCharacterMetric(int characterCode, string name, AdobeFontMetricsVector width, 
            AdobeFontMetricsVector widthDirection0, 
            AdobeFontMetricsVector widthDirection1, 
            AdobeFontMetricsVector vVector, 
            PdfRectangle boundingBox,
            AdobeFontMetricsLigature ligature)
        {
            CharacterCode = characterCode;
            Name = name;
            Width = width;
            WidthDirection0 = widthDirection0;
            WidthDirection1 = widthDirection1;
            VVector = vVector;
            BoundingBox = boundingBox;
            Ligature = ligature;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"[{CharacterCode}] {Name} Width: {Width}.";
        }
    }
}