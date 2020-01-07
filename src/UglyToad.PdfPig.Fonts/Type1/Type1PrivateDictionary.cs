namespace UglyToad.PdfPig.Fonts.Type1
{
    using System;
    using System.Collections.Generic;
    using CharStrings;

    /// <summary>
    /// The Private dictionary for a Type 1 font contains hints that apply across all characters in the font. These hints
    /// help preserve properties of character outline shapes when rendered at smaller sizes and lower resolutions.
    /// These hints help ensure that the shape is as close as possible to the original design even where the character
    /// must be represented in few pixels.
    /// Note that subroutines are also defined in the private dictionary however for the purposes of this API they are
    /// stored on the parent <see cref="Type1Font"/>.
    /// </summary>
    public class Type1PrivateDictionary : AdobeStylePrivateDictionary
    {
        /// <summary>
        /// Optional: Uniquely identifies this font.
        /// </summary>
        public int? UniqueId { get; set; }
        
        /// <summary>
        /// Optional: Indicates the number of random bytes used for charstring encryption/decryption.
        /// Default: 4
        /// </summary>
        public int LenIv { get; }

        /// <summary>
        /// Optional: Preserved for backwards compatibility. Must be set if the <see cref="AdobeStylePrivateDictionary.LanguageGroup"/> is 1.
        /// </summary>
        public bool? RoundStemUp { get; }

        /// <summary>
        /// Required: Backwards compatibility.
        /// Default: 5839
        /// </summary>
        public int Password { get; } = 5839;

        /// <summary>
        /// Required: Backwards compatibility.
        /// Default: {16 16}
        /// </summary>
        public MinFeature MinFeature { get; } = new MinFeature(16, 16);

        /// <inheritdoc />
        /// <summary>
        /// Creates a new <see cref="T:UglyToad.PdfPig.Fonts.Type1.Type1PrivateDictionary" />.
        /// </summary>
        /// <param name="builder">The builder used to gather property values.</param>
        internal Type1PrivateDictionary(Builder builder) : base(builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            UniqueId = builder.UniqueId;
            RoundStemUp = builder.RoundStemUp;
            LenIv = builder.LenIv;

            if (builder.Password.HasValue)
            {
                Password = builder.Password.Value;
            }
        }

        /// <summary>
        /// A mutable builder which can set any property of the private dictionary and performs no validation.
        /// </summary>
        internal class Builder : BaseBuilder
        {
            /// <summary>
            /// Temporary storage for the Rd procedure tokens.
            /// </summary>
            public object Rd { get; set; }

            /// <summary>
            /// Temporary storage for the No Access Put procedure tokens.
            /// </summary>
            public object NoAccessPut { get; set; }

            /// <summary>
            /// Temporary storage for the No Access Def procedure tokens.
            /// </summary>
            public object NoAccessDef { get; set; }

            /// <summary>
            /// Temporary storage for the decrypted but raw bytes of the subroutines in this private dictionary.
            /// </summary>
            public IReadOnlyList<Type1CharstringDecryptedBytes> Subroutines { get; set; }

            /// <summary>
            /// Temporary storage for the tokens of the other subroutine procedures.
            /// </summary>
            public object[] OtherSubroutines { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.UniqueId"/>.
            /// </summary>
            public int? UniqueId { get; set; }
            
            /// <summary>
            /// <see cref="Type1PrivateDictionary.Password"/>.
            /// </summary>
            public int? Password { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.LenIv"/>.
            /// </summary>
            public int LenIv { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.MinFeature"/>.
            /// </summary>
            public MinFeature MinFeature { get; set; }

            /// <summary>
            /// <see cref="Type1PrivateDictionary.RoundStemUp"/>.
            /// </summary>
            public bool? RoundStemUp { get; set; }

            /// <summary>
            /// Generate a <see cref="Type1PrivateDictionary"/> from the values in this builder.
            /// </summary>
            /// <returns>The generated <see cref="Type1PrivateDictionary"/>.</returns>
            public  Type1PrivateDictionary Build()
            {
                return new Type1PrivateDictionary(this);
            }
        }
    }
}
