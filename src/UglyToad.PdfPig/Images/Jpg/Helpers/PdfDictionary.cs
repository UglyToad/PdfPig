namespace UglyToad.PdfPig.Images.Jpg.Helpers
{
    using System;
    using System.Data;
    using System.Diagnostics;
    using Tokens;
    using UglyToad.PdfPig.Parser.Parts;

    internal class PdfDictionary
    {
        public static void Parse(DictionaryToken dictionary, Context context) {

            if (dictionary is null)
            {
                Debug.WriteLine($"Warning: Jpg parser not provided with DictionaryToken.");
                return;                
            }
            if (dictionary.TryGet(NameToken.BitsPerComponent, out NumericToken bitPerComponentToken) == false)
            {
                throw new DataException($"Jpg Dictionary does not contain BitsPerComponent.");
            }
            context.BitsPerComponent = bitPerComponentToken.Int;

            var hasImageMask = false;
            if (dictionary.TryGet(NameToken.ImageMask, out BooleanToken imageMaskToken))
            {
                hasImageMask = imageMaskToken.Data;                
            }
            context.HasImageMask = hasImageMask;


            // Unable to get ColorSpace without scanner.

            //if (dictionary.TryGet(NameToken.ColorSpace, out IndirectReferenceToken colorSpaceIndirectReferenceToken) == false)
            //{       
            //    throw new DataException($"");
            //}
            //if (!DirectObjectFinder.TryGet(colorSpaceIndirectReferenceToken, scanner, out ArrayToken arrayTokenFromIndirectReference))
            //{

            //}
        }
    }
}
