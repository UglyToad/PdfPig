namespace UglyToad.PdfPig.Fonts.TrueType.Names
{
    using System;

    /// <summary>
    /// A record in a TrueType font which is a human-readable name for
    /// a feature, setting, copyright notice, font name or other font related
    /// information.
    /// </summary>
    public class TrueTypeNameRecord
    {
        /// <summary>
        /// The supported platform identifier.
        /// </summary>
        public TrueTypePlatformIdentifier PlatformId { get; }

        /// <summary>
        /// The platform specific encoding id. Interpretation depends on the value of the <see cref="PlatformId"/>.
        /// </summary>
        public ushort PlatformEncodingId { get; }

        /// <summary>
        /// The language id uniquely defines the language in which the string is written for this record.
        /// </summary>
        public ushort LanguageId { get; }

        /// <summary>
        /// Used to reference this record by other tables in the font.
        /// </summary>
        public ushort NameId { get; }
        
        /// <summary>
        /// The value of this record.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// Create a new <see cref="TrueTypeNameRecord"/>.
        /// </summary>
        public TrueTypeNameRecord(TrueTypePlatformIdentifier platformId, 
            ushort platformEncodingId,
            ushort languageId, 
            ushort nameId,
            string value)
        {
            PlatformId = platformId;
            PlatformEncodingId = platformEncodingId;
            LanguageId = languageId;
            NameId = nameId;
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"(Platform: {PlatformId}, Id: {NameId}) - {Value}";
        }
    }
}
