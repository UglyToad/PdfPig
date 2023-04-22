using System;

namespace IccProfileNet.Tags
{
    // PRivate tag instead of unknown tag
    internal sealed class IccUnknownTagType : IccTagTypeBase
    {
        public string Signature { get; }

        private IccUnknownTagType(byte[] rawData, string signature)
        {
            RawData = rawData;
            Signature = signature;
        }

        /// <summary>
        /// Parse the bytes.
        /// </summary>
        public static IccTagTypeBase Parse(byte[] bytes)
        {
            string typeSignature = IccHelper.GetString(bytes, TypeSignatureOffset, TypeSignatureLength);

            switch (typeSignature)
            {
                case "curv":
                    return IccCurveType.Parse(bytes);

                case "para":
                    return IccParametricCurveType.Parse(bytes);

                case "mft1":
                    return IccLut8Type.Parse(bytes);

                case "mft2":
                    return IccLut16Type.Parse(bytes);

                case "mAB ":
                case "mBA ":
                    return new IccLutABType(bytes);

                case IccTags.MeasurementTag:
                    return new IccMeasurementType(bytes);

                case "mluc":
                    return new IccMultiLocalizedUnicodeType(bytes);

                case "sf32":
                    return new IccS15Fixed16ArrayType(bytes);

                case "sig ":
                    return new IccSignatureType(bytes);

                case "text":
                case IccTags.ProfileDescriptionTag:
                    return new IccTextType(bytes);

                case IccTags.ViewingConditionsTag:
                    return new IccViewingConditionsType(bytes);

                case "XYZ ":
                    return new IccXyzType(bytes);

                case "dtim":
                    return new IccDateTimeType(bytes);
            }

            // TODO - implement others tags

            throw new InvalidOperationException($"Invalid tag signature '{typeSignature}' for ICC profile.");
            //return new IccUnknownTagType(bytes, typeSignature);
        }
    }
}
