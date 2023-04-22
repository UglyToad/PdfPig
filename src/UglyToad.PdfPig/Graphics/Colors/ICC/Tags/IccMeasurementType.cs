using System;
using System.Linq;

namespace IccProfileNet.Tags
{
    internal sealed class IccMeasurementType : IccTagTypeBase
    {
        public const int StandardObserverOffset = 8;
        public const int StandardObserverLength = 4;
        public const int TristimulusOffset = 12;
        public const int TristimulusLength = 12;
        public const int MeasurementGeometryOffset = 24;
        public const int MeasurementGeometryLength = 4;
        public const int MeasurementFlareOffset = 28;
        public const int MeasurementFlareLength = 4;
        public const int StandardIlluminantOffset = 32;
        public const int StandardIlluminantLength = 4;

        private readonly Lazy<string> _standardObserver;
        /// <summary>
        /// TODO
        /// </summary>
        public string StandardObserver => _standardObserver.Value;

        private readonly Lazy<IccXyz> _tristimulus;
        /// <summary>
        /// TODO
        /// </summary>
        public IccXyz Tristimulus => _tristimulus.Value;

        private readonly Lazy<string> _measurementGeometry;
        /// <summary>
        /// TODO
        /// </summary>
        public string MeasurementGeometry => _measurementGeometry.Value;

        private readonly Lazy<string> _measurementFlare;
        /// <summary>
        /// TODO
        /// </summary>
        public string MeasurementFlare => _measurementFlare.Value;

        private readonly Lazy<string> _standardIlluminant;
        /// <summary>
        /// TODO
        /// </summary>
        public string StandardIlluminant => _standardIlluminant.Value;

        public IccMeasurementType(byte[] bytes)
        {
            string typeSignature = IccHelper.GetString(bytes, TypeSignatureOffset, TypeSignatureLength);

            if (typeSignature != IccTags.MeasurementTag)
            {
                throw new ArgumentException(nameof(typeSignature));
            }

            RawData = bytes;

            _standardObserver = new Lazy<string>(() =>
            {
                // Encoded value for standard observer
                // 8 to 11
                byte[] standardObserverBytes = bytes
                    .Skip(StandardObserverOffset)
                    .Take(StandardObserverLength)
                    .ToArray();
                string standardObserverHex = IccHelper.GetHexadecimalString(standardObserverBytes);

                /*
                 * Table 50 — Standard observer encodings (v4.4)
                 * Standard observer                            Hex encoding
                 * Unknown                                      00000000h
                 * CIE 1931 standard colorimetric observer      00000001h
                 * CIE 1964 standard colorimetric observer      00000002h
                 */

                /*
                 * Standard Observer                            Encoded Value
                 * unknown                                      00000000h
                 * 1931 2 degree Observer                       00000001h
                 * 1964 10 degree Observer                      00000002h
                 */

                switch (standardObserverHex)
                {
                    case "00000000":
                        return "Unknown";

                    case "00000001":
                        return "1931";

                    case "00000002":
                        return "1964";

                    default:
                        throw new ArgumentOutOfRangeException(nameof(standardObserverHex));
                }
            });

            _tristimulus = new Lazy<IccXyz>(() =>
            {
                // nCIEXYZ tristimulus values for measurement backing
                // 12 to 23
                return IccHelper.ReadXyz(RawData
                    .Skip(TristimulusOffset)
                    .Take(TristimulusLength)
                    .ToArray());
            });

            _measurementGeometry = new Lazy<string>(() =>
            {
                // Encoded value for measurement geometry
                // 24 to 27
                byte[] measurementGeometry = RawData
                    .Skip(MeasurementGeometryOffset)
                    .Take(MeasurementGeometryLength)
                    .ToArray();
                string measurementGeometryHex = IccHelper.GetHexadecimalString(measurementGeometry);
                /*
                 * Table 51 — Measurement geometry encodings
                 * Geometry             Hex encoding
                 * Unknown              00000000h
                 * 0°:45° or 45°:0°     00000001h
                 * 0°:d or d:0°         00000002h
                 */
                return measurementGeometryHex; // TODO
            });

            _measurementFlare = new Lazy<string>(() =>
            {
                // Encoded value for measurement flare
                // 28 to 31
                byte[] measurementFlare = RawData
                    .Skip(MeasurementFlareOffset)
                    .Take(MeasurementFlareLength)
                    .ToArray();
                string measurementFlareHex = IccHelper.GetHexadecimalString(measurementFlare);
                /*
                 * Table 52 — Measurement flare encodings
                 * Flare                 Hex encoding
                 * 0 (0 %)               00000000h
                 * 1,0 (or 100 %)        00010000h
                 */
                return measurementFlareHex; // TODO
            });

            _standardIlluminant = new Lazy<string>(() =>
            {
                // Encoded value for standard illuminant
                // 32 to 35
                byte[] standardIlluminantBytes = bytes
                    .Skip(StandardIlluminantOffset)
                    .Take(StandardIlluminantLength)
                    .ToArray();
                string standardIlluminantHex = IccHelper.GetHexadecimalString(standardIlluminantBytes);

                /*
                 * Table 53 — Standard illuminant encodings
                 *  Standard illuminant      Hex encoding
                 *  Unknown                  00000000h
                 *  D50                      00000001h
                 *  D65                      00000002h
                 *  D93                      00000003h
                 *  F2                       00000004h
                 *  D55                      00000005h
                 *  A                        00000006h
                 *  Equi-Power (E)           00000007h
                 *  F8                       00000008h
                 */

                switch (standardIlluminantHex)
                {
                    case "00000000":
                        return "Standard illuminant";

                    case "00000001":
                        return "D50";

                    case "00000002":
                        return "D65";

                    case "00000003":
                        return "D93";

                    case "00000004":
                        return "F2";

                    case "00000005":
                        return "D55";

                    case "00000006":
                        return "A";

                    case "00000007":
                        return "Equi-Power (E)";

                    case "00000008":
                        return "F8";

                    default:
                        throw new ArgumentOutOfRangeException(nameof(standardIlluminantHex));
                }
            });
        }
    }
}
