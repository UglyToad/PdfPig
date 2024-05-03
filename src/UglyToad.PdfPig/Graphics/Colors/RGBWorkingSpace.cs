#nullable disable

namespace UglyToad.PdfPig.Graphics.Colors
{
    // The RGB working space specifications below were obtained from: http://www.brucelindbloom.com/index.html?WorkingSpaceInfo.html
    internal class RGBWorkingSpace
    {
        public static readonly XYZReferenceWhite ReferenceWhites = new XYZReferenceWhite();

        public static readonly RGBWorkingSpace AdobeRGB1998 = new RGBWorkingSpace {
            GammaCorrection = CreateGammaFunc(2.2),
            ReferenceWhite = ReferenceWhites.D65,
            RedPrimary = (0.6400, 0.3300, 0.297361),
            GreenPrimary = (0.2100, 0.7100, 0.627355),
            BluePrimary = (0.1500, 0.0600, 0.075285),
        };

        public static readonly RGBWorkingSpace AppleRGB = new RGBWorkingSpace {
            GammaCorrection = CreateGammaFunc(1.8),
            ReferenceWhite = ReferenceWhites.D65,
            RedPrimary = (0.6250, 0.3400, 0.244634),
            GreenPrimary = (0.2800, 0.5950, 0.672034),
            BluePrimary = (0.1550, 0.0700, 0.083332)
        };

        public static readonly RGBWorkingSpace BestRGB = new RGBWorkingSpace {
            GammaCorrection = CreateGammaFunc(2.2),
            ReferenceWhite = ReferenceWhites.D50,
            RedPrimary = (0.7347, 0.2653, 0.228457),
            GreenPrimary = (0.2150, 0.7750, 0.737352),
            BluePrimary = (0.1300, 0.0350, 0.034191)
        };

        public static readonly RGBWorkingSpace BetaRGB = new RGBWorkingSpace {
            GammaCorrection = CreateGammaFunc(2.2),
            ReferenceWhite = ReferenceWhites.D50,
            RedPrimary = (0.6888, 0.3112, 0.303273),
            GreenPrimary = (0.1986, 0.7551, 0.663786),
            BluePrimary = (0.1265, 0.0352, 0.032941)
        };

        public static readonly RGBWorkingSpace BruceRGB = new RGBWorkingSpace {
            GammaCorrection = CreateGammaFunc(2.2),
            ReferenceWhite = ReferenceWhites.D65,
            RedPrimary = (0.6400, 0.3300, 0.240995),
            GreenPrimary = (0.2800, 0.6500, 0.683554),
            BluePrimary = (0.1500, 0.0600, 0.075452)
        };

        public static readonly RGBWorkingSpace CIE_RGB = new RGBWorkingSpace {
            GammaCorrection = CreateGammaFunc(2.2),
            ReferenceWhite = ReferenceWhites.E,
            RedPrimary = (0.7350, 0.2650, 0.176204),
            GreenPrimary = (0.2740, 0.7170, 0.812985),
            BluePrimary = (0.1670, 0.0090, 0.010811)
        };

        public static readonly RGBWorkingSpace ColorMatchRGB = new RGBWorkingSpace {
            GammaCorrection = CreateGammaFunc(1.8),
            ReferenceWhite = ReferenceWhites.D50,
            RedPrimary = (0.6300, 0.3400, 0.274884),
            GreenPrimary = (0.2950, 0.6050, 0.658132),
            BluePrimary = (0.1500, 0.0750, 0.066985)
        };

        public static readonly RGBWorkingSpace DonRGB4 = new RGBWorkingSpace {
            GammaCorrection = CreateGammaFunc(2.2),
            ReferenceWhite = ReferenceWhites.D50,
            RedPrimary = (0.6960, 0.3000, 0.278350),
            GreenPrimary = (0.2150, 0.7650, 0.687970),
            BluePrimary = (0.1300, 0.0350, 0.033680)
        };

        public static readonly RGBWorkingSpace EktaSpacePS5 = new RGBWorkingSpace {
            GammaCorrection = CreateGammaFunc(2.2),
            ReferenceWhite = ReferenceWhites.D50,
            RedPrimary = (0.6950, 0.3050, 0.260629),
            GreenPrimary = (0.2600, 0.7000, 0.734946),
            BluePrimary = (0.1100, 0.0050, 0.004425)
        };

        public static readonly RGBWorkingSpace NTSC_RGB = new RGBWorkingSpace {
            GammaCorrection = CreateGammaFunc(2.2),
            ReferenceWhite = ReferenceWhites.C,
            RedPrimary = (0.6700, 0.3300, 0.298839),
            GreenPrimary = (0.2100, 0.7100, 0.586811),
            BluePrimary = (0.1400, 0.0800, 0.114350)
        };

        public static readonly RGBWorkingSpace PAL_SECAM_RGB = new RGBWorkingSpace {
            GammaCorrection = CreateGammaFunc(2.2),
            ReferenceWhite = ReferenceWhites.D65,
            RedPrimary = (0.6400, 0.3300, 0.222021),
            GreenPrimary = (0.2900, 0.6000, 0.706645),
            BluePrimary = (0.1500, 0.0600, 0.071334)
        };

        public static readonly RGBWorkingSpace ProPhotoRGB = new RGBWorkingSpace {
            GammaCorrection = CreateGammaFunc(1.8),
            ReferenceWhite = ReferenceWhites.D50,
            RedPrimary = (0.7347, 0.2653, 0.288040),
            GreenPrimary = (0.1596, 0.8404, 0.711874),
            BluePrimary = (0.0366, 0.0001, 0.000086)
        };

        public static readonly RGBWorkingSpace SMPTE_C_RGB = new RGBWorkingSpace {
            GammaCorrection = CreateGammaFunc(2.2),
            ReferenceWhite = ReferenceWhites.D65,
            RedPrimary = (0.6300, 0.3400, 0.212395),
            GreenPrimary = (0.3100, 0.5950, 0.701049),
            BluePrimary = (0.1550, 0.0700, 0.086556)
        };

        public static readonly RGBWorkingSpace sRGB = new RGBWorkingSpace {
            // sRGB gamma correction obtained from: http://www.brucelindbloom.com/index.html?Eqn_XYZ_to_RGB.html
            GammaCorrection = val => val <= 0.0031308 ? 12.92 * val : (1.055 * Math.Pow(val, (1 / 2.4)) - 0.055),
            ReferenceWhite = ReferenceWhites.D65,
            RedPrimary = (0.6400, 0.3300, 0.212656),
            GreenPrimary = (0.3000, 0.6000, 0.715158),
            BluePrimary = (0.1500, 0.0600, 0.072186),
        };

        public static readonly RGBWorkingSpace WideGamutRGB = new RGBWorkingSpace {
            GammaCorrection = CreateGammaFunc(2.2),
            ReferenceWhite = ReferenceWhites.D50,
            RedPrimary = (0.7350, 0.2650, 0.258187),
            GreenPrimary = (0.1150, 0.8260, 0.724938),
            BluePrimary = (0.1570, 0.0180, 0.016875)
        };

        public Func<double, double> GammaCorrection { get; private set; }
        public (double X, double Y, double Z) ReferenceWhite { get; private set; }
        public (double x, double y, double Y) RedPrimary { get; private set; }
        public (double x, double y, double Y) BluePrimary { get; private set; }
        public (double x, double y, double Y) GreenPrimary { get; private set; }

        private static Func<double, double> CreateGammaFunc(double gamma)
        {
            return val => {
                var result = Math.Pow(val, 1 / gamma);
                return double.IsNaN(result) ? 0 : result;
            };
        }

        // The reference white values below were obtained from: http://www.brucelindbloom.com/index.html?Eqn_ChromAdapt.html
        internal class XYZReferenceWhite
        {
            internal XYZReferenceWhite() { }
            public readonly (double X, double Y, double Z) A = (1.09850, 1.00000, 0.35585);
            public readonly (double X, double Y, double Z) B = (0.99072, 1.00000, 0.85223);
            public readonly (double X, double Y, double Z) C = (0.98074, 1.00000, 1.18232);
            public readonly (double X, double Y, double Z) D50 = (0.96422, 1.00000, 0.82521);
            public readonly (double X, double Y, double Z) D55 = (0.95682, 1.00000, 0.92149);
            public readonly (double X, double Y, double Z) D65 = (0.95047, 1.00000, 1.08883);
            public readonly (double X, double Y, double Z) D75 = (0.94972, 1.00000, 1.22638);
            public readonly (double X, double Y, double Z) E = (1.00000, 1.00000, 1.00000);
            public readonly (double X, double Y, double Z) F2 = (0.99186, 1.00000, 0.67393);
            public readonly (double X, double Y, double Z) F7 = (0.95041, 1.00000, 1.08747);
            public readonly (double X, double Y, double Z) F11 = (1.00962, 1.00000, 0.64350);
        }
    }
}