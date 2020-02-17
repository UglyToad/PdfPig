using System;
using System.Collections.Generic;
using System.Text;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Geometry;
using Xunit;
using static UglyToad.PdfPig.Core.PdfPath;

namespace UglyToad.PdfPig.Tests.Geometry
{
    public class BezierCurveTests
    {
        private static readonly DoubleComparer DoubleComparer = new DoubleComparer(3);
        private static readonly DoubleComparer PreciseDoubleComparer = new DoubleComparer(6);
        private static readonly PointComparer PointComparer = new PointComparer(DoubleComparer);

        //public static void Split()
        //{

        //}

        [Fact]
        public static void IntersectLine()
        {
            /**************************************************
             * WARNING: need to fix commented checks
             **************************************************/

            BezierCurve bezierCurve4 = new BezierCurve(new PdfPoint(838.9977979318809, 95.671193235772), new PdfPoint(395.7034695493279, 147.3440136838414), new PdfPoint(275.0076248732246, 55.57600730645929), new PdfPoint(957.0714444435359, 489.38731097550294));
            Assert.Null(bezierCurve4.Intersect(new PdfLine(405.7081633435599, 275.8998117922303, 421.00503949913616, 818.5738856248036)));
            //Assert.Null(bezierCurve4.Intersect(new PdfLine(619.6238321741257, 498.911907946902, 783.1759979436561, 402.350366461589)));
            var intersection40 = bezierCurve4.Intersect(new PdfLine(811.0936318489453, 676.5356886755211, 774.5959976348438, 102.96093324952781));
            Assert.Equal(2, intersection40.Length);
            Assert.Contains(new PdfPoint(792.5322042541575, 384.83551351939366), intersection40, PointComparer);
            //Assert.Null(bezierCurve4.Intersect(new PdfLine(856.1474641701632, 743.2580461805339, 720.8662662616595, 410.99413446464274)));
            var intersection41 = bezierCurve4.Intersect(new PdfLine(823.8777951547044, 36.37893536078452, 391.2772169996215, 486.92603750085107));
            Assert.Equal(2, intersection41.Length);
            Assert.Contains(new PdfPoint(759.0373802670636, 103.90926819985287), intersection41, PointComparer);
            Assert.Contains(new PdfPoint(605.601651552348, 263.7103096870932), intersection41, PointComparer);
            Assert.Null(bezierCurve4.Intersect(new PdfLine(967.0466595737648, 752.2192070165648, 596.5753647734898, 692.7867576537766)));
            Assert.Null(bezierCurve4.Intersect(new PdfLine(90.34993765825116, 172.47123605680758, 76.04014183010798, 962.5061481219865)));
            Assert.Null(bezierCurve4.Intersect(new PdfLine(253.4349734143222, 960.0704523419075, 132.1427266993902, 445.71575980737555)));
            var intersection42 = bezierCurve4.Intersect(new PdfLine(579.5358092911656, 37.6583106222359, 722.8194514003167, 857.3034484990549));
            Assert.Equal(2, intersection42.Length);
            Assert.Contains(new PdfPoint(593.3107646145511, 116.45708338571966), intersection42, PointComparer);
            Assert.Contains(new PdfPoint(620.8362608321851, 273.91496769357286), intersection42, PointComparer);
            Assert.Null(bezierCurve4.Intersect(new PdfLine(737.9400377401382, 699.699900336188, 49.684585625688335, 441.15809056920654)));
            Assert.Null(bezierCurve4.Intersect(new PdfLine(173.3662389987205, 783.1311908140424, 615.2539944045337, 790.1019706842866)));
            Assert.Null(bezierCurve4.Intersect(new PdfLine(296.94025013379644, 73.59632463536614, 42.0769232546675, 387.9093886277065)));
            var intersection43 = bezierCurve4.Intersect(new PdfLine(74.0977239040721, 619.3200447685273, 608.9545284349134, 100.34604393376978));
            Assert.Equal(2, intersection43.Length);
            Assert.Contains(new PdfPoint(592.2674109899074, 116.53763022279675), intersection43, PointComparer);
            Assert.Contains(new PdfPoint(511.3671755763147, 195.0354982229876), intersection43, PointComparer);
            var intersection44 = bezierCurve4.Intersect(new PdfLine(820.2163923944734, 717.173364146329, 953.5257410985023, 427.48244361487843));
            //Assert.Equal(2, intersection44.Length);
            Assert.Contains(new PdfPoint(932.2891103920052, 473.6311883065225), intersection44, PointComparer);
            var intersection45 = bezierCurve4.Intersect(new PdfLine(981.6646618092392, 105.86073123564721, 311.7334388081725, 996.5229973668335));
            //Assert.Equal(2, intersection45.Length);
            Assert.Contains(new PdfPoint(778.5328059619519, 375.92110883964796), intersection45, PointComparer);
            Assert.Null(bezierCurve4.Intersect(new PdfLine(123.98284286148686, 691.6900651406168, 49.549147941199024, 884.1694445599976)));
            var intersection46 = bezierCurve4.Intersect(new PdfLine(805.4005941396833, 728.6098454141719, 665.5457765690878, 34.05657022823405));
            Assert.Equal(2, intersection46.Length);
            Assert.Contains(new PdfPoint(680.8807262610109, 110.21368600267137), intersection46, PointComparer);
            Assert.Contains(new PdfPoint(727.8660773480029, 343.5544464434673), intersection46, PointComparer);
            var intersection47 = bezierCurve4.Intersect(new PdfLine(971.6420598824315, 380.15558036002307, 119.62220192520923, 98.55075618282515));
            //Assert.Equal(2, intersection47.Length);
            Assert.Contains(new PdfPoint(592.5403745718742, 254.85701784398492), intersection47, PointComparer);
            //Assert.Null(bezierCurve4.Intersect(new PdfLine(655.3922871321146, 433.7908714978125, 146.52092563817533, 408.2134759325372)));
            var intersection48 = bezierCurve4.Intersect(new PdfLine(567.711506436318, 464.54448311094296, 665.8463685771701, 182.0483112809783));
            Assert.Equal(2, intersection48.Length);
            Assert.Contains(new PdfPoint(631.4789419878045, 280.98019170903984), intersection48, PointComparer);

        }

        //public static void FindIntersectionT()
        //{

        //}
    }
}
