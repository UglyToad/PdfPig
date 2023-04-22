using System;
using System.Linq;
using IccProfileNet.Tags;

namespace IccProfileNet.Parsers
{
    internal static class IccProfileV4TagParser
    {
        /// <summary>
        /// The profile version number consistent with this ICC specification is “4.4.0.0”.
        /// <para>TODO - update with correct parsers.</para>
        /// </summary>
        public static IccTagTypeBase Parse(byte[] profile, IccTagTableItem tag)
        {
            byte[] data = profile.Skip((int)tag.Offset).Take((int)tag.Size).ToArray();
            switch (tag.Signature)
            {
                case IccTags.AToB0Tag: // 9.2.1 AToB0Tag
                case IccTags.AToB1Tag: // 9.2.2 AToB1Tag
                case IccTags.AToB2Tag: // 9.2.3 AToB2Tag
                case IccTags.BToA0Tag: // 9.2.6 BToA0Tag
                case IccTags.BToA1Tag: // 9.2.7 BToA1Tag
                case IccTags.BToA2Tag: // 9.2.8 BToA2Tag
                case IccTags.Preview0Tag: // 9.2.40 preview0Tag
                case IccTags.Preview1Tag: // 9.2.41 preview1Tag
                case IccTags.Preview2Tag: // 9.2.42 preview2Tag
                    return ReadlutTypeOrlutABType(data); // Permitted tag types: lut8Type or lut16Type or lutBToAType

                case IccTags.GreenMatrixColumnTag: // 9.2.31 greenMatrixColumnTag
                case IccTags.LuminanceTag: // 9.2.33 luminanceTag
                case IccTags.MediaWhitePointTag: // 9.2.36 mediaWhitePointTag
                case IccTags.BlueMatrixColumnTag: // 9.2.4 blueMatrixColumnTag
                case IccTags.RedMatrixColumnTag: // 9.2.46 redMatrixColumnTag
                    return new IccXyzType(data); // Permitted tag type: XYZType

                case IccTags.GrayTRCTag: // 9.2.30 grayTRCTag
                case IccTags.GreenTRCTag: // 9.2.32 greenTRCTag
                case IccTags.RedTRCTag: // 9.2.47 redTRCTag
                case IccTags.BlueTRCTag: // 9.2.5 blueTRCTag
                    return IccBaseCurveType.Parse(data); // Permitted tag types: curveType or parametricCurveType

                case IccTags.BToD0Tag: // 9.2.9 BToD0Tag
                case IccTags.BToD1Tag: // 9.2.10 BToD1Tag
                case IccTags.BToD2Tag: // 9.2.11 BToD2Tag
                case IccTags.BToD3Tag: // 9.2.12 BToD3Tag
                    // Allowed tag types: multiProcessElementsType
                    break;

                case IccTags.CalibrationDateTimeTag: // 9.2.13 calibrationDateTimeTag
                    return new IccDateTimeType(data); // Permitted tag type: dateTimeType

                case IccTags.CharTargetTag: // 9.2.14 charTargetTag
                    return new IccTextType(data); // Permitted tag type: textType

                case IccTags.ChromaticAdaptationTag: // 9.2.15 chromaticAdaptationTag
                    return new IccS15Fixed16ArrayType(data); // Permitted tag type: s15Fixed16ArrayType

                case IccTags.ChromaticityTag: // 9.2.16 chromaticityTag
                    // Permitted tag type: chromaticityType
                    break;

                case IccTags.CicpTag: // 9.2.17 cicpTag
                    // Permitted tag type: cicpType
                    break;

                case IccTags.ColorantOrderTag: // 9.2.18 colorantOrderTag
                    // Permitted tag type: colorantOrderType
                    break;

                case IccTags.ColorantTableTag: // 9.2.19 colorantTableTag
                case IccTags.ColorantTableOutTag: // 9.2.20 colorantTableOutTag
                    // Permitted tag type: colorantTableType
                    break;

                case IccTags.ColorimetricIntentImageStateTag: // 9.2.21 colorimetricIntentImageStateTag
                case IccTags.PerceptualRenderingIntentGamutTag: // 9.2.39 perceptualRenderingIntentGamutTag
                case IccTags.SaturationRenderingIntentGamutTag: // 9.2.48 saturationRenderingIntentGamutTag
                case IccTags.TechnologyTag: // 9.2.49 technologyTag
                    return new IccSignatureType(data); // Permitted tag type: signatureType

                case IccTags.CopyrightTag: // 9.2.22 copyrightTag
                case IccTags.DeviceMfgDescTag: // 9.2.23 deviceMfgDescTag
                case IccTags.DeviceModelDescTag: // 9.2.24 deviceModelDescTag
                case IccTags.ProfileDescriptionTag: // 9.2.43 profileDescriptionTag
                case IccTags.ViewingCondDescTag: // 9.2.50 viewingCondDescTag
                    return new IccMultiLocalizedUnicodeType(data); // Permitted tag type: multiLocalizedUnicodeType

                case IccTags.DToB0Tag: // 9.2.25 DToB0Tag
                case IccTags.DToB1Tag: // 9.2.26 DToB1Tag
                case IccTags.DToB2Tag: // 9.2.27 DToB2Tag
                case IccTags.DToB3Tag: // 9.2.28 DToB3Tag
                    // Allowed tag types: multiProcessElementsType
                    break;

                case IccTags.GamutTag: // 9.2.29 gamutTag
                    return ReadlutTypeOrlutABType(data); // Permitted tag types: lut8Type or lut16Type or lutBToAType

                case IccTags.MeasurementTag: // 9.2.34 measurementTag
                    return new IccMeasurementType(data);

                case IccTags.MetadataTag: // 9.2.35 metadataTag
                    // Allowed tag types: dictType
                    break;

                case IccTags.NamedColor2Tag: // 9.2.37 namedColor2Tag
                    // Permitted tag type: namedColor2Type
                    break;

                case IccTags.OutputResponseTag: // 9.2.38 outputResponseTag
                    // Permitted tag type: responseCurveSet16Type
                    break;

                case IccTags.ProfileSequenceDescTag: // 9.2.44 profileSequenceDescTag
                    // Permitted tag type: profileSequenceDescType
                    break;

                case IccTags.ProfileSequenceIdentifierTag: // 9.2.45 profileSequenceIdentifierTag
                    // Permitted tag type: profileSequenceIdentifierType
                    break;

                case IccTags.ViewingConditionsTag: // 9.2.51 viewingConditionsTag
                    return new IccViewingConditionsType(data); // Permitted tag type: viewingConditionsType

                default:
                    return IccUnknownTagType.Parse(data);
            }

            throw new NotImplementedException($"Tag signature '{tag.Signature}' for ICC v4 profile.");
        }

        private static IccTagTypeBase ReadlutTypeOrlutABType(byte[] bytes)
        {
            string typeSignature = IccHelper.GetString(bytes, 0, 4);
            switch (typeSignature)
            {
                case "mft1":
                case "mft2":
                    return IccBaseLutType.Parse(bytes);

                case "mAB ":
                case "mBA ":
                    return new IccLutABType(bytes);

                default:
                    throw new InvalidOperationException($"{typeSignature}");
            }
        }
    }
}
