using System;
using System.Linq;

namespace IccProfileNet.Tags
{
    // TODO make lazy
    internal class IccChromaticityType : IccTagTypeBase
    {
        public const int NumberOfDeviceChannelsOffset = 8;
        public const int NumberOfDeviceChannelsLength = 2;
        public const int PhosphorOrColorantOffset = 10;
        public const int PhosphorOrColorantLength = 2;
        public const int XyCoordinateValuesOfChannel1Offset = 12;
        public const int XyCoordinateValuesOfChannel1Length = 8;
        public const int XyCoordinateValuesOfOtherChannelsOffset = 20;

        /// <summary>
        /// Number of device channels (n).
        /// </summary>
        public int NumberOfDeviceChannels { get; }

        /// <summary>
        /// Phosphor or colorant type.
        /// </summary>
        public PhosphorOrColorantType PhosphorOrColorant { get; }

        /// <summary>
        /// CIE xy coordinate values of channel 1.
        /// </summary>
        public double[] XyCoordinateValuesOfChannel1 { get; }

        public double[] XyCoordinateValuesOfOtherChannels { get; }

        public IccChromaticityType(byte[] bytes)
        {
            string typeSignature = IccHelper.GetString(bytes, TypeSignatureOffset, TypeSignatureLength);

            if (typeSignature != "chrm")
            {
                throw new ArgumentException(nameof(typeSignature));
            }

            NumberOfDeviceChannels = IccHelper.ReadUInt16(bytes
                .Skip(NumberOfDeviceChannelsOffset)
                .Take(NumberOfDeviceChannelsLength)
                .ToArray());

            /*
            * Phosphor or colorant      Encoded value   Channel 1 (x, y)    Channel 2 (x, y)    Channel 3 (x, y)
            * type as defined in:
            * Unknown                   0000h           Any                 Any                 Any
            * ITU-R BT.709-2            0001h           (0,640, 0,330)      (0,300, 0,600)      (0,150, 0,060)
            * SMPTE RP145               0002h           (0,630, 0,340)      (0,310, 0,595)      (0,155, 0,070)
            * EBU Tech. 3213-E          0003h           (0,640 0,330)       (0,290, 0,600)      (0,150, 0,060)
            * P22                       0004h           (0,625, 0,340)      (0,280, 0,605)      (0,155, 0,070)
            * P3                        0005h           (0,680, 0,320)      (0,265, 0.690)      (0,150, 0,060)
            * ITU-R BT.2020             0006h           (0,780, 0,292)      (0,170, 0,797)      (0,131, 0.046)
            */
            string encodedPhosphorOrColorant = IccHelper.GetHexadecimalString(bytes, PhosphorOrColorantOffset, PhosphorOrColorantLength);
            switch (encodedPhosphorOrColorant)
            {
                case "0000":
                    PhosphorOrColorant = PhosphorOrColorantType.Unknown;
                    break;

                case "0001":
                    PhosphorOrColorant = PhosphorOrColorantType.ITU_R_BT_709_2;
                    break;

                case "0002":
                    PhosphorOrColorant = PhosphorOrColorantType.SMPTE_RP145;
                    break;

                case "0003":
                    PhosphorOrColorant = PhosphorOrColorantType.EBU_Tech_3213_E;
                    break;

                case "0004":
                    PhosphorOrColorant = PhosphorOrColorantType.P22;
                    break;

                case "0005":
                    PhosphorOrColorant = PhosphorOrColorantType.P3;
                    break;

                case "0006":
                    PhosphorOrColorant = PhosphorOrColorantType.ITU_R_BT_2020;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(encodedPhosphorOrColorant));
            }

            var channel1Bytes = bytes
                .Skip(XyCoordinateValuesOfChannel1Offset)
                .Take(XyCoordinateValuesOfChannel1Length)
                .ToArray();

            // TODO - array of type u16Fixed16Number[2]

            var otherChannelsBytes = bytes
                .Skip(XyCoordinateValuesOfOtherChannelsOffset)
                .ToArray();

            // TODO - array of type u16Fixed16Number[...]

            RawData = bytes;

            throw new NotImplementedException("IccChromaticityType implementation not finished");
        }

        /// <summary>
        /// Phosphor or colorant type.
        /// </summary>
        public enum PhosphorOrColorantType : byte
        {
            /// <summary>
            /// Unknown.
            /// <list type="bullet">
            /// <item>
            /// <term>Channel 1 (x, y)</term>
            /// Any
            /// </item>
            /// <item>
            /// <term>Channel 2 (x, y)</term>
            /// Any
            /// </item>
            /// <item>
            /// <term>Channel 3 (x, y)</term>
            /// Any
            /// </item>
            /// </list>
            /// </summary>
            Unknown = 0,

            /// <summary>
            /// ITU-R BT.709-2.
            /// <list type="bullet">
            /// <item>
            /// <term>Channel 1 (x, y)</term>
            /// (0,640, 0,330)
            /// </item>
            /// <item>
            /// <term>Channel 2 (x, y)</term>
            /// (0,300, 0,600)
            /// </item>
            /// <item>
            /// <term>Channel 3 (x, y)</term>
            /// (0,150, 0,060)
            /// </item>
            /// </list>
            /// </summary>
            ITU_R_BT_709_2 = 1,

            /// <summary>
            /// SMPTE RP145.
            /// <list type="bullet">
            /// <item>
            /// <term>Channel 1 (x, y)</term>
            /// (0,630, 0,340)
            /// </item>
            /// <item>
            /// <term>Channel 2 (x, y)</term>
            /// (0,310, 0,595)
            /// </item>
            /// <item>
            /// <term>Channel 3 (x, y)</term>
            /// (0,155, 0,070)
            /// </item>
            /// </list>
            /// </summary>
            SMPTE_RP145 = 2,

            /// <summary>
            /// EBU Tech. 3213-E.
            /// <list type="bullet">
            /// <item>
            /// <term>Channel 1 (x, y)</term>
            /// (0,640 0,330)
            /// </item>
            /// <item>
            /// <term>Channel 2 (x, y)</term>
            /// (0,290, 0,600)
            /// </item>
            /// <item>
            /// <term>Channel 3 (x, y)</term>
            /// (0,150, 0,060)
            /// </item>
            /// </list>
            /// </summary>
            EBU_Tech_3213_E = 3,

            /// <summary>
            /// P22.
            /// <list type="bullet">
            /// <item>
            /// <term>Channel 1 (x, y)</term>
            /// (0,625, 0,340)
            /// </item>
            /// <item>
            /// <term>Channel 2 (x, y)</term>
            /// (0,280, 0,605)
            /// </item>
            /// <item>
            /// <term>Channel 3 (x, y)</term>
            /// (0,155, 0,070)
            /// </item>
            /// </list>
            /// </summary>
            P22 = 4,

            /// <summary>
            /// P3.
            /// <list type="bullet">
            /// <item>
            /// <term>Channel 1 (x, y)</term>
            /// (0,680, 0,320)
            /// </item>
            /// <item>
            /// <term>Channel 2 (x, y)</term>
            /// (0,265, 0.690)
            /// </item>
            /// <item>
            /// <term>Channel 3 (x, y)</term>
            /// (0,150, 0,060)
            /// </item>
            /// </list>
            /// </summary>
            P3 = 5,

            /// <summary>
            /// ITU-R BT.2020.
            /// <list type="bullet">
            /// <item>
            /// <term>Channel 1 (x, y)</term>
            /// (0,780, 0,292)
            /// </item>
            /// <item>
            /// <term>Channel 2 (x, y)</term>
            /// (0,170, 0,797)
            /// </item>
            /// <item>
            /// <term>Channel 3 (x, y)</term>
            /// (0,131, 0.046)
            /// </item>
            /// </list>
            /// </summary>
            ITU_R_BT_2020 = 6
        }
    }
}
