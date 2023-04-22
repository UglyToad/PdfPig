using System;
using System.Linq;
using IccProfileNet.Tags;

namespace IccProfileNet.Parsers
{
    internal static class IccProfileV2TagParser
    {
        /// <summary>
        /// The profile version number consistent with this ICC specification is “2.4.0.0”.
        /// <para>TODO - update with correct parsers.</para>
        /// </summary>
        public static IccTagTypeBase Parse(byte[] profile, IccTagTableItem tag)
        {
            byte[] data = profile.Skip((int)tag.Offset).Take((int)tag.Size).ToArray();
            switch (tag.Signature)
            {
                case IccTags.AToB0Tag: // 6.4.1 AToB0Tag
                case IccTags.AToB1Tag: // 6.4.2 AToB1Tag
                case IccTags.AToB2Tag: // 6.4.3 AToB2Tag
                case IccTags.BToA0Tag: // 6.4.6 BToA0Tag
                case IccTags.BToA1Tag: // 6.4.7 BToA1Tag
                case IccTags.BToA2Tag: // 6.4.8 BToA2Tag
                case IccTags.GamutTag: // 6.4.18 gamutTag
                case IccTags.Preview0Tag: // 6.4.29 preview0Tag
                case IccTags.Preview1Tag: // 6.4.30 preview1Tag
                case IccTags.Preview2Tag: // 6.4.31 preview2Tag
                    return IccBaseLutType.Parse(data); // Tag Type: lut8Type or lut16Type

                case IccTags.BlueColorantTag: // 6.4.4 blueColorantTag
                case IccTags.GreenColorantTag: // 6.4.20 greenColorantTag
                case IccTags.LuminanceTag: // 6.4.22 luminanceTag
                case IccTags.MediaBlackPointTag: // 6.4.24 mediaBlackPointTag
                case IccTags.MediaWhitePointTag: // 6.4.25 mediaWhitePointTag
                case IccTags.RedColorantTag: // 6.4.40 redColorantTag
                    return new IccXyzType(data); // Tag Type: XYZType

                case IccTags.BlueTRCTag: // 6.4.5 blueTRCTag
                case IccTags.GrayTRCTag: // 6.4.19 grayTRCTag
                case IccTags.GreenTRCTag: // 6.4.21 greenTRCTag
                case IccTags.RedTRCTag: // 6.4.41 redTRCTag
                    return new IccCurveType(data); // Tag Type: curveType

                case IccTags.CalibrationDateTimeTag: // 6.4.9 calibrationDateTimeTag
                    return new IccDateTimeType(data); // Tag Type: dateTimeType

                case IccTags.CharTargetTag: // 6.4.10 charTargetTag
                case IccTags.CopyrightTag: // 6.4.13 copyrightTag
                case IccTags.DeviceMfgDescTag: // 6.4.15 deviceMfgDescTag
                case IccTags.DeviceModelDescTag: // 6.4.16 deviceModelDescTag
                case IccTags.ProfileDescriptionTag: // 6.4.32 profileDescriptionTag
                case IccTags.ScreeningDescTag: // 6.4.42 screeningDescTag
                case IccTags.ViewingCondDescTag: // 6.4.46 viewingCondDescTag
                    return new IccTextType(data); // Tag Type: textDescriptionType

                case IccTags.ChromaticAdaptationTag: // 6.4.11 chromaticAdaptationTag
                    return new IccS15Fixed16ArrayType(data); // Tag Type: s15Fixed16ArrayType

                case IccTags.ChromaticityTag: // 6.4.12 chromaticityTag
                    return new IccChromaticityType(data); // Tag Type: chromaticityType

                case IccTags.CrdInfoTag: // 6.4.14 crdInfoTag
                    // Tag Type: crdInfoType
                    break;

                case IccTags.DeviceSettingsTag: // 6.4.17 deviceSettingsTag
                    // Tag Type: deviceSettingsType
                    break;

                case IccTags.MeasurementTag: // 6.4.23 measurementTag
                    return new IccMeasurementType(data); // Tag Type: measurementType

                case IccTags.NamedColorTag: // 6.4.26 namedColorTag
                    // Tag Type: namedColorType
                    break;

                case IccTags.NamedColor2Tag: // 6.4.27 namedColor2Tag
                    // Tag Type: namedColor2Type
                    break;

                case IccTags.OutputResponseTag: // 6.4.28 outputResponseTag
                    // Tag Type: responseCurveSet16Type
                    break;

                case IccTags.ProfileSequenceDescTag: // 6.4.33 profileSequenceDescTag
                    // Tag Type: profileSequenceDescType
                    break;

                case IccTags.Ps2CRD0Tag: // 6.4.34 ps2CRD0Tag
                case IccTags.Ps2CRD1Tag: // 6.4.35 ps2CRD1Tag
                case IccTags.Ps2CRD2Tag: // 6.4.36 ps2CRD2Tag
                case IccTags.Ps2CRD3Tag: // 6.4.37 ps2CRD3Tag
                case IccTags.Ps2CSATag: // 6.4.38 ps2CSATag
                case IccTags.Ps2RenderingIntentTag: // 6.4.39 ps2RenderingIntentTag
                    // Tag Type: dataType
                    break;

                case IccTags.ScreeningTag: // 6.4.43 screeningTag
                    // Tag Type: screeningType
                    break;

                case IccTags.TechnologyTag: // 6.4.44 technologyTag
                    return new IccSignatureType(data); // Tag Type: signatureType

                case IccTags.UcrbgTag: // 6.4.45 ucrbgTag
                    // Tag Type: ucrbgType
                    break;

                case IccTags.ViewingConditionsTag: // 6.4.47 viewingConditionsTag
                    return new IccViewingConditionsType(data); // Tag Type: viewingConditionsType

                default:
                    return IccUnknownTagType.Parse(data);
            }

            throw new NotImplementedException($"Tag signature '{tag.Signature}' for ICC v2 profile to implement.");
        }
    }
}
