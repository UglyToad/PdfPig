namespace UglyToad.PdfPig.Tests.Geometry
{
    using PdfPig.Geometry;
    using PdfPig.Core;
    using Xunit;
    using System.Collections.Generic;

    public class PdfRectangleTests
    {
        private static readonly DoubleComparer DoubleComparer = new DoubleComparer(3);
        private static readonly DoubleComparer PreciseDoubleComparer = new DoubleComparer(6);
        private static readonly PointComparer PointComparer = new PointComparer(DoubleComparer);
        private static readonly PdfRectangle UnitRectangle = new PdfRectangle(new PdfPoint(0, 0), new PdfPoint(1, 1));

        [Fact]
        public void Area()
        {
            PdfRectangle rectangle = new PdfRectangle(10, 10, 20, 20);
            Assert.Equal(100, rectangle.Area);

            PdfRectangle rectangle1 = new PdfRectangle(149.95376d, 687.13456d, 451.73539d, 1478.4997d);
            Assert.Equal(238819.4618743782d, rectangle1.Area, DoubleComparer);

            PdfRectangle rectangle2 = new PdfRectangle(558.1943459048198, 730.6304475255059, 571.3017547365034, 962.7770981990296);
            Assert.Equal(3042.841059283922, rectangle2.Area, DoubleComparer);
            Assert.Equal(3042.841059283922, TransformationMatrix.GetRotationMatrix(46.63115240869564).Transform(rectangle2).Area, DoubleComparer);
            Assert.Equal(3042.841059283922, TransformationMatrix.GetRotationMatrix(42).Transform(rectangle2).Area, DoubleComparer);
            Assert.Equal(3042.841059283922, TransformationMatrix.GetRotationMatrix(194.045).Transform(rectangle2).Area, DoubleComparer);
            Assert.Equal(3042.841059283922, TransformationMatrix.GetRotationMatrix(-74.4657).Transform(rectangle2).Area, DoubleComparer);
            Assert.Equal(3042.841059283922, TransformationMatrix.GetRotationMatrix(45).Transform(rectangle2).Area, DoubleComparer);

            PdfRectangle rectangle3 = new PdfRectangle(523.0784251391958, 417.882005884581, 248.94510082667455, 897.0802138593754);
            Assert.Equal(131364.1977567333, rectangle3.Area, DoubleComparer);
            Assert.Equal(131364.1977567333, TransformationMatrix.GetRotationMatrix(-84.98758555772564).Transform(rectangle3).Area, DoubleComparer);
            Assert.Equal(131364.1977567333, TransformationMatrix.GetRotationMatrix(49.789).Transform(rectangle3).Area, DoubleComparer);
            Assert.Equal(131364.1977567333, TransformationMatrix.GetRotationMatrix(-250.564).Transform(rectangle3).Area, DoubleComparer);
            Assert.Equal(131364.1977567333, TransformationMatrix.GetRotationMatrix(278.457968).Transform(rectangle3).Area, DoubleComparer);
            Assert.Equal(131364.1977567333, TransformationMatrix.GetRotationMatrix(45).Transform(rectangle3).Area, DoubleComparer);
        }

        [Fact]
        public void Centroid()
        {
            PdfRectangle rectangle = new PdfRectangle(10, 10, 20, 20);
            Assert.Equal(new PdfPoint(15, 15), rectangle.Centroid, PointComparer);

            PdfRectangle rectangle1 = new PdfRectangle(149.95376d, 687.13456d, 451.73539d, 1478.4997d);
            Assert.Equal(new PdfPoint(300.844575d, 1082.81713d), rectangle1.Centroid,
                PointComparer);

            var rectangle2 = new PdfRectangle(250.6895432725822, 314.1665092335595, 609.5698098944074, 210.305632858515);
            Assert.Equal(new PdfPoint(430.12967658349476, 262.23607104603724), rectangle2.Centroid, PointComparer);
            Assert.Equal(new PdfPoint(-382.3464610180157, -328.00987695873295), 
                TransformationMatrix.GetRotationMatrix(189.25654524492694).Transform(rectangle2).Centroid, PointComparer);
            
            var rectangle3 = new PdfRectangle(29.760198327045796, 57.91029427107597, 49.486162626911856, 135.5406100042995);
            Assert.Equal(new PdfPoint(39.62318047697882, 96.72545213768774), rectangle3.Centroid, PointComparer);
            Assert.Equal(new PdfPoint(-104.14627286173634, 8.908612201691023), 
                TransformationMatrix.GetRotationMatrix(107.38710946826126).Transform(rectangle3).Centroid, PointComparer);
        }

        [Fact]
        public void Intersect()
        {
            PdfRectangle rectangle = new PdfRectangle(10, 10, 20, 20);
            PdfRectangle rectangle1 = new PdfRectangle(149.95376d, 687.13456d, 451.73539d, 1478.4997d);
            Assert.Null(rectangle.Intersect(rectangle1));
            Assert.Equal(rectangle1, rectangle1.Intersect(rectangle1));

            PdfRectangle rectangle2 = new PdfRectangle(50, 687.13456d, 350, 1478.4997d);
            Assert.Equal(new PdfRectangle(149.95376d, 687.13456d, 350, 1478.4997d), rectangle1.Intersect(rectangle2));

            PdfRectangle rectangle3 = new PdfRectangle(200, 800, 350, 1200);
            Assert.Equal(rectangle3, rectangle1.Intersect(rectangle3));
        }

        [Fact]
        public void IntersectsWith()
        {
            PdfRectangle rectangle = new PdfRectangle(10, 10, 20, 20);
            PdfRectangle rectangle1 = new PdfRectangle(149.95376d, 687.13456d, 451.73539d, 1478.4997d);
            Assert.False(rectangle.IntersectsWith(rectangle1));
            Assert.True(rectangle1.IntersectsWith(rectangle1));

            PdfRectangle rectangle2 = new PdfRectangle(50, 687.13456d, 350, 1478.4997d);
            Assert.True(rectangle1.IntersectsWith(rectangle2));

            PdfRectangle rectangle3 = new PdfRectangle(200, 800, 350, 1200);
            Assert.True(rectangle1.IntersectsWith(rectangle3));

            PdfRectangle rectangle4 = new PdfRectangle(5, 7, 10, 25);
            Assert.False(rectangle1.IntersectsWith(rectangle4));

            PdfRectangle rectangle5 = new PdfRectangle(new PdfPoint(878.2919644480792, 284.98675185325374), new PdfPoint(862.1750371743453, 246.06041964596272), new PdfPoint(674.8837206318682, 369.20521494929005), new PdfPoint(658.7667933581344, 330.2788827419989));
            Assert.False(rectangle5.IntersectsWith(new PdfRectangle(new PdfPoint(353.1968952039725, 430.7443580752997), new PdfPoint(446.46498829172396, 861.3602670280086), new PdfPoint(279.1778049563087, 446.7763235832159), new PdfPoint(372.44589804406013, 877.3922325359247))));
            Assert.True(rectangle5.IntersectsWith(new PdfRectangle(new PdfPoint(667.0669096951665, 198.51789529471972), new PdfPoint(382.6159639765757, 304.5417365505567), new PdfPoint(805.4174478896282, 569.6980087856942), new PdfPoint(520.9665021710373, 675.7218500415313))));
            Assert.False(rectangle5.IntersectsWith(new PdfRectangle(new PdfPoint(551.3092120057465, 227.06692642283235), new PdfPoint(573.7600100241283, 224.45067656822854), new PdfPoint(637.1984096402393, 964.1070163284926), new PdfPoint(659.6492076586211, 961.4907664738888))));
            Assert.False(rectangle5.IntersectsWith(new PdfRectangle(new PdfPoint(576.0264582268862, 237.3036405985971), new PdfPoint(681.8346969097365, 836.3029346389208), new PdfPoint(81.61426291333743, 324.63743896852714), new PdfPoint(187.42250159618789, 923.6367330088508))));
            Assert.False(rectangle5.IntersectsWith(new PdfRectangle(new PdfPoint(497.24281873991447, 91.31821450873753), new PdfPoint(479.5633816162531, 120.25708474837967), new PdfPoint(670.0527524235065, 196.89187137934994), new PdfPoint(652.373315299845, 225.83074161899208))));
            Assert.False(rectangle5.IntersectsWith(new PdfRectangle(new PdfPoint(-4.249112230964041, 686.3187905377893), new PdfPoint(378.01392804203846, 712.0632534788804), new PdfPoint(29.827995232697745, 180.3296490056303), new PdfPoint(412.09103550570023, 206.07411194672144))));
            Assert.False(rectangle5.IntersectsWith(new PdfRectangle(new PdfPoint(58.89628923557103, 129.3271736138968), new PdfPoint(123.33158287815809, 105.24630822621918), new PdfPoint(128.32775177944993, 315.111056585911), new PdfPoint(192.763045422037, 291.0301911982334))));
            Assert.True(rectangle5.IntersectsWith(new PdfRectangle(new PdfPoint(950.8344574600331, 283.16273567796645), new PdfPoint(758.3541118235311, 171.63094500244426), new PdfPoint(920.0101055145042, 336.3590654469167), new PdfPoint(727.5297598780021, 224.8272747713944))));
            Assert.False(rectangle5.IntersectsWith(new PdfRectangle(new PdfPoint(943.8618293653394, 590.2759142098903), new PdfPoint(959.0937596337767, 645.9921037613167), new PdfPoint(821.0587308322029, 623.8483506716165), new PdfPoint(836.29066110064, 679.564540223043))));
            Assert.False(rectangle5.IntersectsWith(new PdfRectangle(new PdfPoint(516.4441064067341, 494.14183676455775), new PdfPoint(56.05953048666899, 609.4959156104007), new PdfPoint(580.6399682740255, 750.3511094430946), new PdfPoint(120.25539235396036, 865.7051882889376))));
            Assert.False(rectangle5.IntersectsWith(new PdfRectangle(new PdfPoint(771.0120694406086, 725.248829666573), new PdfPoint(837.8274511325825, 721.7292934742848), new PdfPoint(757.4530291699984, 467.84205742119985), new PdfPoint(824.2684108619724, 464.32252122891174))));
            Assert.False(rectangle5.IntersectsWith(new PdfRectangle(new PdfPoint(246.66456794858738, 922.5874548402979), new PdfPoint(640.4301432906257, 807.0536493780463), new PdfPoint(82.57457921919291, 363.3313257672401), new PdfPoint(476.34015456123115, 247.79752030498844))));
            Assert.False(rectangle5.IntersectsWith(new PdfRectangle(new PdfPoint(589.6127906613359, 428.73052433904), new PdfPoint(590.4285001742446, 450.33962086005704), new PdfPoint(83.55291951467768, 447.83349102689664), new PdfPoint(84.3686290275864, 469.44258754791366))));
            Assert.False(rectangle5.IntersectsWith(new PdfRectangle(new PdfPoint(527.4319517027001, 713.9214038056743), new PdfPoint(474.4501775934585, 656.9306524140234), new PdfPoint(574.9484349425286, 669.74743662673), new PdfPoint(521.966660833287, 612.7566852350792))));
            Assert.False(rectangle5.IntersectsWith(new PdfRectangle(new PdfPoint(227.56613088253334, 434.82640067032025), new PdfPoint(302.4286146616149, 238.31257488185557), new PdfPoint(-75.2573302718133, 319.4649681724581), new PdfPoint(-0.39484649273174455, 122.95114238399341))));
            
            PdfRectangle rectangle6 = new PdfRectangle(new PdfPoint(662.1090997035524, 32.17519235014163), new PdfPoint(806.0379110211408, 315.93110789147556), new PdfPoint(339.06556720072024, 196.03176446165605), new PdfPoint(482.9943785183086, 479.78768000299));
            Assert.False(rectangle6.IntersectsWith(new PdfRectangle(new PdfPoint(189.05553239397122, 954.3244361250967), new PdfPoint(383.00565538205376, 941.1111183775815), new PdfPoint(153.72614299627176, 435.74616912881055), new PdfPoint(347.67626598435436, 422.5328513812954))));
            Assert.True(rectangle6.IntersectsWith(new PdfRectangle(new PdfPoint(995.5897815154967, 1.1229217531037676), new PdfPoint(379.34059275027573, 21.64016669347191), new PdfPoint(999.1015392990057, 106.60091802001234), new PdfPoint(382.8523505337847, 127.11816296038049))));
            Assert.False(rectangle6.IntersectsWith(new PdfRectangle(new PdfPoint(241.11149345040388, 619.9967264825813), new PdfPoint(237.1881211805317, 604.4557261946209), new PdfPoint(226.79603116379286, 623.6107079976066), new PdfPoint(222.8726588939207, 608.0697077096462))));
            Assert.True(rectangle6.IntersectsWith(new PdfRectangle(new PdfPoint(337.21506865564413, 584.7741433244227), new PdfPoint(109.32748084033773, 127.07497626075303), new PdfPoint(625.6331322198964, 441.17131090136075), new PdfPoint(397.7455444045901, -16.52785616230892))));
            Assert.True(rectangle6.IntersectsWith(new PdfRectangle(new PdfPoint(93.37323974780458, 181.63603918905022), new PdfPoint(431.92088160752087, 100.95768061242754), new PdfPoint(295.0623780749654, 1027.9767850103665), new PdfPoint(633.6100199346818, 947.2984264337438))));
            Assert.False(rectangle6.IntersectsWith(new PdfRectangle(new PdfPoint(353.2714114563955, 682.7722235976627), new PdfPoint(516.3517717255074, 938.3777636233134), new PdfPoint(288.68961521742494, 723.9764267476444), new PdfPoint(451.76997548653685, 979.5819667732951))));
            Assert.False(rectangle6.IntersectsWith(new PdfRectangle(new PdfPoint(284.50251692724146, 243.0225227763899), new PdfPoint(113.63929025302508, 270.76705018145725), new PdfPoint(330.68991999126365, 527.4652384155129), new PdfPoint(159.82669331704727, 555.2097658205802))));
            Assert.False(rectangle6.IntersectsWith(new PdfRectangle(new PdfPoint(176.28996298129402, 627.4254462807612), new PdfPoint(254.264379445621, 619.6483463564534), new PdfPoint(167.6767079500521, 541.0676083390957), new PdfPoint(245.6511244143791, 533.290508414788))));
            Assert.True(rectangle6.IntersectsWith(new PdfRectangle(new PdfPoint(931.8451889288237, 559.8728346000398), new PdfPoint(515.5501301285348, 197.4090859556618), new PdfPoint(911.9621263795686, 582.7088305417828), new PdfPoint(495.66706757927966, 220.24508189740482))));
            Assert.True(rectangle6.IntersectsWith(new PdfRectangle(new PdfPoint(406.27507112318654, 355.8737650175066), new PdfPoint(422.7958992566672, 1198.7815883306412), new PdfPoint(708.8559600519006, 349.9432388980332), new PdfPoint(725.3767881853813, 1192.8510622111676))));
            Assert.False(rectangle6.IntersectsWith(new PdfRectangle(new PdfPoint(132.0349088943558, 605.2652596778812), new PdfPoint(276.42412597859493, 376.7615638987222), new PdfPoint(237.5219367274915, 671.9214654317269), new PdfPoint(381.9111538117305, 443.4177696525678))));
            Assert.False(rectangle6.IntersectsWith(new PdfRectangle(new PdfPoint(641.6260921527592, 707.5199584318012), new PdfPoint(243.2473794850697, 656.7270448460315), new PdfPoint(635.3739718826574, 756.5565552484187), new PdfPoint(236.99525921496797, 705.763641662649))));
            Assert.False(rectangle6.IntersectsWith(new PdfRectangle(new PdfPoint(717.68521670706, 809.7007029409066), new PdfPoint(621.190366829182, 813.5865480300052), new PdfPoint(707.18690874391, 549.0025465172622), new PdfPoint(610.692058866032, 552.8883916063609))));
            Assert.True(rectangle6.IntersectsWith(new PdfRectangle(new PdfPoint(294.3463877234756, 636.4837934326679), new PdfPoint(689.1715874504388, 198.35020500129144), new PdfPoint(573.8239023166606, 888.3356679112038), new PdfPoint(968.6491020436237, 450.2020794798273))));
            Assert.False(rectangle6.IntersectsWith(new PdfRectangle(new PdfPoint(523.8760324273468, 59.56931986367704), new PdfPoint(537.4510792959534, 87.35541425053748), new PdfPoint(591.3110514063164, 26.623576194938323), new PdfPoint(604.886098274923, 54.409670581798764))));
            
            PdfRectangle rectangle7 = new PdfRectangle(new PdfPoint(182.95967923508024, 378.6167914103907), new PdfPoint(426.47638943669153, 379.5221368891931), new PdfPoint(180.78120599362617, 964.5750244187219), new PdfPoint(424.29791619523746, 965.4803698975243));
            Assert.False(rectangle7.IntersectsWith(new PdfRectangle(new PdfPoint(761.5597778351872, 941.1192604974003), new PdfPoint(633.51933021415, 795.815811545735), new PdfPoint(1137.453093713675, 609.8845322780608), new PdfPoint(1009.4126460926377, 464.5810833263956))));
            Assert.True(rectangle7.IntersectsWith(new PdfRectangle(new PdfPoint(346.38019022487805, 962.9027155935489), new PdfPoint(958.9455360803695, 469.3068278732299), new PdfPoint(-41.2821063348334, 481.803714011534), new PdfPoint(571.2832395206581, -11.792173708785015))));
            Assert.False(rectangle7.IntersectsWith(new PdfRectangle(new PdfPoint(944.7329525257767, 570.6504069116083), new PdfPoint(982.5761178272382, 510.51399836788244), new PdfPoint(520.2007288578434, 303.4970549720583), new PdfPoint(558.0438941593047, 243.36064642833253))));
            Assert.True(rectangle7.IntersectsWith(new PdfRectangle(new PdfPoint(98.85248861424049, 989.4091194618815), new PdfPoint(568.5889599061336, 566.9303589820568), new PdfPoint(155.57992869188985, 1052.4819884914), new PdfPoint(625.316399983783, 630.0032280115754))));
            Assert.False(rectangle7.IntersectsWith(new PdfRectangle(new PdfPoint(228.91597594992538, 121.37473780400387), new PdfPoint(992.4741132926845, 394.4902007151348), new PdfPoint(175.71193170242228, 270.11908384619034), new PdfPoint(939.2700690451813, 543.2345467573214))));
            Assert.True(rectangle7.IntersectsWith(new PdfRectangle(new PdfPoint(148.60828079133148, 349.8652320702263), new PdfPoint(543.2698201392228, -87.77807332587977), new PdfPoint(521.482521758113, 686.118797722132), new PdfPoint(916.1440611060044, 248.47549232602594))));
            Assert.False(rectangle7.IntersectsWith(new PdfRectangle(new PdfPoint(502.1889544681512, 681.5057338048485), new PdfPoint(987.8306916693583, 656.09516711646), new PdfPoint(509.2653302721178, 816.74803204518), new PdfPoint(994.9070674733248, 791.3374653567914))));
            Assert.True(rectangle7.IntersectsWith(new PdfRectangle(new PdfPoint(457.4837329520253, 837.8034196314052), new PdfPoint(419.5564572679925, 675.7503436833417), new PdfPoint(231.62264648772998, 890.6644692752387), new PdfPoint(193.6953708036972, 728.6113933271752))));
            Assert.False(rectangle7.IntersectsWith(new PdfRectangle(new PdfPoint(534.3547447025437, 496.9672228866692), new PdfPoint(330.84483574853044, 273.554909990856), new PdfPoint(568.0566381729994, 466.26762006330654), new PdfPoint(364.5467292189862, 242.85530716749338))));
            Assert.True(rectangle7.IntersectsWith(new PdfRectangle(new PdfPoint(354.943174387019, -53.20559212698666), new PdfPoint(155.4210594908152, 339.28743315399277), new PdfPoint(764.5103231147291, 154.9960788394219), new PdfPoint(564.9882082185254, 547.4891041204013))));
            Assert.True(rectangle7.IntersectsWith(new PdfRectangle(new PdfPoint(61.636058359621416, 380.97389019976936), new PdfPoint(188.03105500987925, 706.7055436871105), new PdfPoint(423.7997253163123, 240.4420291363649), new PdfPoint(550.1947219665701, 566.1736826237061))));
            Assert.True(rectangle7.IntersectsWith(new PdfRectangle(new PdfPoint(367.43988364323116, 238.54339642513554), new PdfPoint(415.54278724001426, 570.5182440019055), new PdfPoint(-66.58394809280296, 301.43312041808593), new PdfPoint(-18.481044496019905, 633.4079679948559))));
            Assert.True(rectangle7.IntersectsWith(new PdfRectangle(new PdfPoint(802.8192348116105, 416.2791077053364), new PdfPoint(799.4741400026777, 395.89519632164547), new PdfPoint(322.222983998549, 495.1471911426298), new PdfPoint(318.8778891896162, 474.7632797589389))));
            Assert.False(rectangle7.IntersectsWith(new PdfRectangle(new PdfPoint(586.2037586388597, 470.8287115308515), new PdfPoint(522.4985869468035, 277.11479640636696), new PdfPoint(606.4591337194657, 464.167485638185), new PdfPoint(542.7539620274097, 270.45357051370047))));
            Assert.True(rectangle7.IntersectsWith(new PdfRectangle(new PdfPoint(434.6663895248496, 651.5308843669638), new PdfPoint(456.0878643202198, 234.94337283417667), new PdfPoint(246.87900283942474, 641.8746112925039), new PdfPoint(268.3004776347949, 225.28709975971685))));
            
            PdfRectangle rectangle8 = new PdfRectangle(new PdfPoint(72.24297257469115, 649.2596478120211), new PdfPoint(775.2698559740438, 649.2596478120211), new PdfPoint(72.24297257469115, 259.0291933094048), new PdfPoint(775.2698559740438, 259.0291933094048));
            Assert.True(rectangle8.IntersectsWith(new PdfRectangle(new PdfPoint(135.73325218195308, 188.61070668538804), new PdfPoint(971.1947687261684, 188.61070668538804), new PdfPoint(135.73325218195308, 980.9459766541829), new PdfPoint(971.1947687261684, 980.9459766541829))));
            Assert.False(rectangle8.IntersectsWith(new PdfRectangle(new PdfPoint(887.1665126362909, 685.0924487297474), new PdfPoint(361.59633731366625, 685.0924487297474), new PdfPoint(887.1665126362909, 958.0750301074823), new PdfPoint(361.59633731366625, 958.0750301074823))));
            Assert.False(rectangle8.IntersectsWith(new PdfRectangle(new PdfPoint(94.56638479872082, 36.92587654212054), new PdfPoint(462.6070546281588, 36.92587654212054), new PdfPoint(94.56638479872082, 200.29051698988076), new PdfPoint(462.6070546281588, 200.29051698988076))));
            Assert.True(rectangle8.IntersectsWith(new PdfRectangle(new PdfPoint(189.65706086837952, 645.6122492497387), new PdfPoint(515.1224387868391, 645.6122492497387), new PdfPoint(189.65706086837952, 404.0571801331626), new PdfPoint(515.1224387868391, 404.0571801331626))));
            Assert.False(rectangle8.IntersectsWith(new PdfRectangle(new PdfPoint(306.14208421891686, 138.14570543330552), new PdfPoint(632.3673627180267, 138.14570543330552), new PdfPoint(306.14208421891686, 241.07238617284787), new PdfPoint(632.3673627180267, 241.07238617284787))));
            Assert.True(rectangle8.IntersectsWith(new PdfRectangle(new PdfPoint(884.1244198542938, 281.37923606318674), new PdfPoint(726.3671595463759, 281.37923606318674), new PdfPoint(884.1244198542938, 814.1228706660955), new PdfPoint(726.3671595463759, 814.1228706660955))));
            Assert.True(rectangle8.IntersectsWith(new PdfRectangle(new PdfPoint(428.0822141808371, 663.6028736907089), new PdfPoint(322.75792240641687, 663.6028736907089), new PdfPoint(428.0822141808371, 7.667206549313521), new PdfPoint(322.75792240641687, 7.667206549313521))));
            Assert.False(rectangle8.IntersectsWith(new PdfRectangle(new PdfPoint(521.7187655885231, 889.2323835874813), new PdfPoint(43.390276391606534, 889.2323835874813), new PdfPoint(521.7187655885231, 894.6134560273221), new PdfPoint(43.390276391606534, 894.6134560273221))));
            Assert.True(rectangle8.IntersectsWith(new PdfRectangle(new PdfPoint(417.07533672015984, 119.75997450771847), new PdfPoint(589.823316551358, 119.75997450771847), new PdfPoint(417.07533672015984, 419.39962853921907), new PdfPoint(589.823316551358, 419.39962853921907))));
            Assert.True(rectangle8.IntersectsWith(new PdfRectangle(new PdfPoint(632.8285194923662, 466.8323292496054), new PdfPoint(323.8409211696145, 466.8323292496054), new PdfPoint(632.8285194923662, 814.194402048563), new PdfPoint(323.8409211696145, 814.194402048563))));
            Assert.True(rectangle8.IntersectsWith(new PdfRectangle(new PdfPoint(6.297499978868415, 2.494216207784894), new PdfPoint(156.55039831311956, 2.494216207784894), new PdfPoint(6.297499978868415, 495.463635179213), new PdfPoint(156.55039831311956, 495.463635179213))));
            Assert.True(rectangle8.IntersectsWith(new PdfRectangle(new PdfPoint(888.9497553644643, 8.035225856757089), new PdfPoint(55.58493438243828, 8.035225856757089), new PdfPoint(888.9497553644643, 918.5466742549241), new PdfPoint(55.58493438243828, 918.5466742549241))));
            Assert.True(rectangle8.IntersectsWith(new PdfRectangle(new PdfPoint(444.2990302895924, 361.35131179733884), new PdfPoint(683.6918159556293, 361.35131179733884), new PdfPoint(444.2990302895924, 371.7479591330548), new PdfPoint(683.6918159556293, 371.7479591330548))));
            Assert.True(rectangle8.IntersectsWith(new PdfRectangle(new PdfPoint(893.0590924218519, 683.0027652560051), new PdfPoint(738.6303815244727, 683.0027652560051), new PdfPoint(893.0590924218519, 392.96008379091865), new PdfPoint(738.6303815244727, 392.96008379091865))));
            Assert.True(rectangle8.IntersectsWith(new PdfRectangle(new PdfPoint(876.8447498709081, 105.49457512519412), new PdfPoint(72.8325766734833, 105.49457512519412), new PdfPoint(876.8447498709081, 464.6650759722718), new PdfPoint(72.8325766734833, 464.6650759722718))));
            
            PdfRectangle rectangle9 = new PdfRectangle(new PdfPoint(527.2438195115226, 482.9801794154479), new PdfPoint(527.2438195115226, 533.4881831146383), new PdfPoint(768.1236383182934, 482.9801794154479), new PdfPoint(768.1236383182934, 533.4881831146383));
            Assert.True(rectangle9.IntersectsWith(new PdfRectangle(new PdfPoint(896.2540086673047, 50.788729046773454), new PdfPoint(585.7259398610713, 50.788729046773454), new PdfPoint(896.2540086673047, 981.9968313141578), new PdfPoint(585.7259398610713, 981.9968313141578))));
            Assert.True(rectangle9.IntersectsWith(new PdfRectangle(new PdfPoint(975.4166831750707, 490.84681151603615), new PdfPoint(78.87287902477968, 490.84681151603615), new PdfPoint(975.4166831750707, 388.2081094509828), new PdfPoint(78.87287902477968, 388.2081094509828))));
            Assert.False(rectangle9.IntersectsWith(new PdfRectangle(new PdfPoint(948.3251791106021, 66.97725486792005), new PdfPoint(873.5802713629491, 66.97725486792005), new PdfPoint(948.3251791106021, 385.08786496108115), new PdfPoint(873.5802713629491, 385.08786496108115))));
            Assert.True(rectangle9.IntersectsWith(new PdfRectangle(new PdfPoint(921.1503539472649, 710.9364473468173), new PdfPoint(600.960717896737, 710.9364473468173), new PdfPoint(921.1503539472649, 124.98529491562627), new PdfPoint(600.960717896737, 124.98529491562627))));
            Assert.True(rectangle9.IntersectsWith(new PdfRectangle(new PdfPoint(738.692740418769, 34.060716290039814), new PdfPoint(675.1630544733399, 34.060716290039814), new PdfPoint(738.692740418769, 786.2058842514037), new PdfPoint(675.1630544733399, 786.2058842514037))));
            Assert.True(rectangle9.IntersectsWith(new PdfRectangle(new PdfPoint(852.5598556754906, 626.6367318686681), new PdfPoint(639.7864784022581, 626.6367318686681), new PdfPoint(852.5598556754906, 390.4806086082071), new PdfPoint(639.7864784022581, 390.4806086082071))));
            Assert.False(rectangle9.IntersectsWith(new PdfRectangle(new PdfPoint(950.8922395235849, 889.4922824691322), new PdfPoint(918.4267065041784, 889.4922824691322), new PdfPoint(950.8922395235849, 248.70746439209978), new PdfPoint(918.4267065041784, 248.70746439209978))));
            Assert.False(rectangle9.IntersectsWith(new PdfRectangle(new PdfPoint(96.28740018581439, 929.0333663834276), new PdfPoint(279.253124414377, 929.0333663834276), new PdfPoint(96.28740018581439, 431.08938285889656), new PdfPoint(279.253124414377, 431.08938285889656))));
            Assert.False(rectangle9.IntersectsWith(new PdfRectangle(new PdfPoint(747.822532035857, 44.14859346951883), new PdfPoint(337.10659147509205, 44.14859346951883), new PdfPoint(747.822532035857, 52.40052413917362), new PdfPoint(337.10659147509205, 52.40052413917362))));
            Assert.True(rectangle9.IntersectsWith(new PdfRectangle(new PdfPoint(917.1485015686014, 98.5584682462215), new PdfPoint(157.69920400366223, 98.5584682462215), new PdfPoint(917.1485015686014, 614.8675582567737), new PdfPoint(157.69920400366223, 614.8675582567737))));
            Assert.False(rectangle9.IntersectsWith(new PdfRectangle(new PdfPoint(209.12117687089537, 462.5900957548379), new PdfPoint(18.436560594277452, 462.5900957548379), new PdfPoint(209.12117687089537, 307.70544340337915), new PdfPoint(18.436560594277452, 307.70544340337915))));
            Assert.False(rectangle9.IntersectsWith(new PdfRectangle(new PdfPoint(489.8181985321417, 915.7432861165021), new PdfPoint(120.15594156180487, 915.7432861165021), new PdfPoint(489.8181985321417, 75.37282584094507), new PdfPoint(120.15594156180487, 75.37282584094507))));
            Assert.False(rectangle9.IntersectsWith(new PdfRectangle(new PdfPoint(17.796763977895647, 355.96614105536116), new PdfPoint(102.82502664462689, 355.96614105536116), new PdfPoint(17.796763977895647, 881.6150247557526), new PdfPoint(102.82502664462689, 881.6150247557526))));
            Assert.True(rectangle9.IntersectsWith(new PdfRectangle(new PdfPoint(587.2618612142032, 318.14001496409116), new PdfPoint(731.2896862521135, 318.14001496409116), new PdfPoint(587.2618612142032, 696.6383491864503), new PdfPoint(731.2896862521135, 696.6383491864503))));
            Assert.False(rectangle9.IntersectsWith(new PdfRectangle(new PdfPoint(741.2224344081615, 780.9041501122545), new PdfPoint(99.26197376045431, 780.9041501122545), new PdfPoint(741.2224344081615, 767.463545155378), new PdfPoint(99.26197376045431, 767.463545155378))));
            
            PdfRectangle rectangle10 = new PdfRectangle(new PdfPoint(234.60559171191113, 296.83326039714007), new PdfPoint(371.9089402414718, 544.7935633876145), new PdfPoint(724.3131470663056, 25.666923126748543), new PdfPoint(861.6164955958661, 273.627226117223));
            Assert.True(rectangle10.IntersectsWith(new PdfRectangle(new PdfPoint(555.6927483036488, 427.9916727628121), new PdfPoint(577.2339652380674, 484.66031814130065), new PdfPoint(-84.30005120431736, 671.2694790097953), new PdfPoint(-62.7588342698989, 727.9381243882838))));
            Assert.False(rectangle10.IntersectsWith(new PdfRectangle(new PdfPoint(785.2982960489057, 998.9422662129181), new PdfPoint(538.5359876657087, 525.8777450375484), new PdfPoint(572.5356879132348, 1109.924574061206), new PdfPoint(325.7733795300377, 636.8600528858361))));
            Assert.True(rectangle10.IntersectsWith(new PdfRectangle(new PdfPoint(497.80599419376176, 373.5540389184093), new PdfPoint(482.9973315724043, 502.2649741422026), new PdfPoint(-94.0991400195588, 305.45319401992487), new PdfPoint(-108.90780264091634, 434.16412924371826))));
            Assert.True(rectangle10.IntersectsWith(new PdfRectangle(new PdfPoint(749.2087325701312, 101.61218036362123), new PdfPoint(701.8375960057706, 323.3718333188651), new PdfPoint(650.5081252075694, 80.52827313320682), new PdfPoint(603.1369886432087, 302.28792608845066))));
            Assert.False(rectangle10.IntersectsWith(new PdfRectangle(new PdfPoint(869.0924683094668, 561.9488806901714), new PdfPoint(265.75717180671944, 862.5198539742104), new PdfPoint(774.3578761971887, 371.7883918158349), new PdfPoint(171.02257969444162, 672.3593650998739))));
            Assert.True(rectangle10.IntersectsWith(new PdfRectangle(new PdfPoint(111.34000681935197, 248.88970925099966), new PdfPoint(269.3534532975954, 307.17715081678796), new PdfPoint(10.2062207034094, 523.0567968823316), new PdfPoint(168.21966718165285, 581.3442384481199))));
            Assert.False(rectangle10.IntersectsWith(new PdfRectangle(new PdfPoint(146.8543283440638, 733.3774054670405), new PdfPoint(223.79363017852756, 719.7908774787355), new PdfPoint(163.86842324244788, 829.7267144261642), new PdfPoint(240.80772507691165, 816.1401864378593))));
            Assert.False(rectangle10.IntersectsWith(new PdfRectangle(new PdfPoint(17.275259921412044, 827.2197778280545), new PdfPoint(32.217559044340646, 805.2338581316862), new PdfPoint(145.15634368197493, 914.131648338991), new PdfPoint(160.09864280490353, 892.1457286426228))));
            Assert.True(rectangle10.IntersectsWith(new PdfRectangle(new PdfPoint(266.40747509031644, 295.76885609410755), new PdfPoint(713.7570545033277, 293.49829217284), new PdfPoint(269.87297664783057, 978.5466672308406), new PdfPoint(717.2225560608418, 976.276103309573))));
            Assert.False(rectangle10.IntersectsWith(new PdfRectangle(new PdfPoint(514.3183832641009, 536.7467901918496), new PdfPoint(571.8161294277977, 745.0564471408502), new PdfPoint(575.3340435330706, 519.9052140616827), new PdfPoint(632.8317896967673, 728.2148710106833))));
            Assert.True(rectangle10.IntersectsWith(new PdfRectangle(new PdfPoint(895.8912331166259, 174.65782545800892), new PdfPoint(955.031319803297, 301.77893724801174), new PdfPoint(664.872519150143, 282.13381097952254), new PdfPoint(724.0126058368139, 409.25492276952536))));
            Assert.False(rectangle10.IntersectsWith(new PdfRectangle(new PdfPoint(284.24589643056004, 833.7035999407603), new PdfPoint(370.21278248860017, 803.0313147232888), new PdfPoint(222.51416230752548, 660.6847030475731), new PdfPoint(308.4810483655656, 630.0124178301015))));
            Assert.False(rectangle10.IntersectsWith(new PdfRectangle(new PdfPoint(832.0308622768154, 450.56370671531886), new PdfPoint(843.3888672465317, 365.2571921131329), new PdfPoint(1051.4808825867563, 479.78204288704205), new PdfPoint(1062.8388875564726, 394.47552828485607))));
            Assert.True(rectangle10.IntersectsWith(new PdfRectangle(new PdfPoint(426.76665329938896, 286.8156667367974), new PdfPoint(346.3700210902215, 52.45309286983695), new PdfPoint(483.73864647712355, 267.27177666016075), new PdfPoint(403.3420142679561, 32.90920279320022))));
            Assert.True(rectangle10.IntersectsWith(new PdfRectangle(new PdfPoint(-84.27216243167831, 546.7183364008613), new PdfPoint(57.151186598288035, 910.3213174761316), new PdfPoint(497.1573375111396, 320.57138110483265), new PdfPoint(638.5806865411059, 684.1743621801029))));
        }

        [Fact]
        public void Contains()
        {
            PdfRectangle rectangle = new PdfRectangle(10, 10, 20, 20);
            Assert.True(rectangle.Contains(new PdfPoint(15, 15)));
            Assert.False(rectangle.Contains(new PdfPoint(10, 15)));
            Assert.True(rectangle.Contains(new PdfPoint(10, 15), true));
            Assert.False(rectangle.Contains(new PdfPoint(100, 100), true));
            Assert.True(rectangle.Contains(new PdfPoint(10, 10), true));
            Assert.False(rectangle.Contains(new PdfPoint(10, 10), false));


            PdfRectangle rectangle1 = new PdfRectangle(new PdfPoint(-9.065741219039126, 152.06038717005336), new PdfPoint(561.5649100338235, 794.7285258775772), new PdfPoint(337.3202440365057, -155.49875330445175), new PdfPoint(907.9508952893684, 487.16938540307206));

            Assert.False(rectangle1.Contains(new PdfPoint(878.0710297128604, 958.6338401320028)));
            Assert.False(rectangle1.Contains(new PdfPoint(712.1637218020475, 900.3271263983642)));
            Assert.False(rectangle1.Contains(new PdfPoint(199.43768004955032, 591.4025974045339)));
            Assert.True(rectangle1.Contains(new PdfPoint(597.9104731012599, 480.4185063107069)));
            Assert.False(rectangle1.Contains(new PdfPoint(700.2982490595053, 158.6133100628294)));
            Assert.False(rectangle1.Contains(new PdfPoint(28.734629187027515, 909.8703127623256)));
            Assert.False(rectangle1.Contains(new PdfPoint(554.0646001615492, 954.6615145411652)));
            Assert.False(rectangle1.Contains(new PdfPoint(686.447597346381, 166.4497312374238)));
            Assert.False(rectangle1.Contains(new PdfPoint(992.4536098090343, 946.7044762273416)));
            Assert.True(rectangle1.Contains(new PdfPoint(414.96597355085817, 285.8427924045842)));
            Assert.False(rectangle1.Contains(new PdfPoint(330.7504470152678, 569.4851521373695)));
            Assert.True(rectangle1.Contains(new PdfPoint(507.589986029339, 409.90959038016814)));
            Assert.False(rectangle1.Contains(new PdfPoint(38.27056750112745, 42.44622532838471)));
            Assert.False(rectangle1.Contains(new PdfPoint(285.1014485856095, 716.2526900134626)));
            Assert.False(rectangle1.Contains(new PdfPoint(845.2049433809495, 289.08844597429317)));
            Assert.False(rectangle1.Contains(new PdfPoint(833.8869678152687, 851.951147143669)));
            Assert.False(rectangle1.Contains(new PdfPoint(638.3868249570894, 172.42271562536382)));
            Assert.True(rectangle1.Contains(new PdfPoint(736.8099796762018, 296.43874609827606)));
            Assert.True(rectangle1.Contains(new PdfPoint(224.47853252872696, 343.6627195711096)));
            Assert.False(rectangle1.Contains(new PdfPoint(5.940326169825316, 758.3358465772909)));

            Assert.False(rectangle1.Contains(new PdfPoint(-9.065741219039126, 152.06038717005336), false));
            Assert.True(rectangle1.Contains(new PdfPoint(-9.065741219039126, 152.06038717005336), true));
            Assert.False(rectangle1.Contains(new PdfPoint(337.3202440365057, -155.49875330445175), false));
            Assert.True(rectangle1.Contains(new PdfPoint(337.3202440365057, -155.49875330445175), true));
            Assert.False(rectangle1.Contains(new PdfPoint(907.9508952893684, 487.16938540307206), false));
            Assert.True(rectangle1.Contains(new PdfPoint(907.9508952893684, 487.16938540307206), true));


            PdfRectangle rectangle2 = new PdfRectangle(new PdfPoint(0.3057755364282002, 838.311937987381), new PdfPoint(700.7384584344007, 1011.32036557429), new PdfPoint(205.6195042611102, 7.089737703669428), new PdfPoint(906.0521871590828, 180.09816529057827));

            Assert.True(rectangle2.Contains(new PdfPoint(376.4595323878466, 585.092466894829)));
            Assert.False(rectangle2.Contains(new PdfPoint(889.798553549375, 624.5142970059035)));
            Assert.True(rectangle2.Contains(new PdfPoint(276.01474168405093, 442.2367004765932)));
            Assert.True(rectangle2.Contains(new PdfPoint(440.58965440664844, 168.6401533253292)));
            Assert.True(rectangle2.Contains(new PdfPoint(393.30559374931494, 922.4899257142977)));
            Assert.True(rectangle2.Contains(new PdfPoint(637.8415134465054, 555.3212289436499)));
            Assert.True(rectangle2.Contains(new PdfPoint(431.71251570244385, 684.7101086263384)));
            Assert.True(rectangle2.Contains(new PdfPoint(409.9878414724731, 459.0868058788861)));
            Assert.True(rectangle2.Contains(new PdfPoint(445.50959139924583, 93.381052789144)));
            Assert.True(rectangle2.Contains(new PdfPoint(217.30667514232294, 703.8064336607619)));
            Assert.False(rectangle2.Contains(new PdfPoint(942.4601244779722, 659.7345040749511)));
            Assert.True(rectangle2.Contains(new PdfPoint(453.5491945054733, 239.614386832138)));
            Assert.True(rectangle2.Contains(new PdfPoint(205.91858994783374, 513.556359996595)));
            Assert.True(rectangle2.Contains(new PdfPoint(501.95666715066614, 692.9696469791322)));
            Assert.True(rectangle2.Contains(new PdfPoint(262.0508674491778, 797.2378520510254)));
            Assert.True(rectangle2.Contains(new PdfPoint(766.6612527451449, 549.3199995967624)));
            Assert.False(rectangle2.Contains(new PdfPoint(345.79665362473577, 940.2827090711647)));
            Assert.True(rectangle2.Contains(new PdfPoint(292.636051026567, 127.78856059031418)));
            Assert.False(rectangle2.Contains(new PdfPoint(879.0358662373549, 335.0232907107596)));
            Assert.False(rectangle2.Contains(new PdfPoint(165.64977019810345, 910.3962071898146)));

            Assert.False(rectangle2.Contains(new PdfPoint(0.3057755364282002, 838.311937987381), false));
            Assert.True(rectangle2.Contains(new PdfPoint(0.3057755364282002, 838.311937987381), true));
            Assert.False(rectangle2.Contains(new PdfPoint(700.7384584344007, 1011.32036557429), false));
            Assert.True(rectangle2.Contains(new PdfPoint(700.7384584344007, 1011.32036557429), true));
            Assert.False(rectangle2.Contains(new PdfPoint(906.0521871590828, 180.09816529057827), false));
            Assert.True(rectangle2.Contains(new PdfPoint(906.0521871590828, 180.09816529057827), true));


            PdfRectangle rectangle3 = new PdfRectangle(new PdfPoint(493.4136678771659, 550.5731610863402), new PdfPoint(290.7393736458551, 237.61572595373514), new PdfPoint(306.45151010410984, 671.6516820633589), new PdfPoint(103.77721587279905, 358.6942469307538));

            Assert.False(rectangle3.Contains(new PdfPoint(155.11783158265857, 491.7120489589787)));
            Assert.False(rectangle3.Contains(new PdfPoint(449.9104128221276, 625.461645123356)));
            Assert.False(rectangle3.Contains(new PdfPoint(285.64010643047635, 85.39523722525433)));
            Assert.False(rectangle3.Contains(new PdfPoint(289.35503778910464, 662.5718899355262)));
            Assert.False(rectangle3.Contains(new PdfPoint(674.149696132224, 570.563080839261)));
            Assert.False(rectangle3.Contains(new PdfPoint(605.2688442286128, 299.09211063874784)));
            Assert.False(rectangle3.Contains(new PdfPoint(188.26992534772492, 294.3385299879571)));
            Assert.False(rectangle3.Contains(new PdfPoint(2.1020437691612326, 886.8975972850164)));
            Assert.False(rectangle3.Contains(new PdfPoint(459.17085407159186, 490.9267774829792)));
            Assert.False(rectangle3.Contains(new PdfPoint(457.2372207149109, 858.522491065221)));
            Assert.False(rectangle3.Contains(new PdfPoint(818.4596223855638, 334.3782740680763)));
            Assert.False(rectangle3.Contains(new PdfPoint(598.3966707314544, 142.6132440573391)));
            Assert.False(rectangle3.Contains(new PdfPoint(971.3243011858224, 526.8587848079378)));
            Assert.True(rectangle3.Contains(new PdfPoint(274.03460206251873, 278.8272927120462)));
            Assert.False(rectangle3.Contains(new PdfPoint(794.212342121175, 833.4323334553933)));
            Assert.False(rectangle3.Contains(new PdfPoint(809.1326302285668, 467.4568034493124)));
            Assert.False(rectangle3.Contains(new PdfPoint(522.5601035362702, 387.83173224351884)));
            Assert.False(rectangle3.Contains(new PdfPoint(727.3924963693886, 903.3302201639223)));
            Assert.False(rectangle3.Contains(new PdfPoint(449.6411730880507, 649.8411525298924)));
            Assert.False(rectangle3.Contains(new PdfPoint(820.139699646277, 731.795976628666)));

            Assert.False(rectangle3.Contains(new PdfPoint(493.4136678771659, 550.5731610863402), false));
            Assert.True(rectangle3.Contains(new PdfPoint(493.4136678771659, 550.5731610863402), true));
            Assert.False(rectangle3.Contains(new PdfPoint(306.45151010410984, 671.6516820633589), false));
            Assert.True(rectangle3.Contains(new PdfPoint(306.45151010410984, 671.6516820633589), true));
            Assert.False(rectangle3.Contains(new PdfPoint(103.77721587279905, 358.6942469307538), false));
            Assert.True(rectangle3.Contains(new PdfPoint(103.77721587279905, 358.6942469307538), true));


            PdfRectangle rectangle4 = new PdfRectangle(new PdfPoint(640.988045066141, 125.5653379379294), new PdfPoint(640.988045066141, 367.79778854671156), new PdfPoint(204.81813807089276, 125.5653379379294), new PdfPoint(204.81813807089276, 367.79778854671156));

            Assert.False(rectangle4.Contains(new PdfPoint(778.1017360060002, 258.11710112182675)));
            Assert.True(rectangle4.Contains(new PdfPoint(330.46781278490147, 349.1426132444837)));
            Assert.False(rectangle4.Contains(new PdfPoint(168.6719344494385, 600.7344020728498)));
            Assert.False(rectangle4.Contains(new PdfPoint(778.1287869775505, 865.1647528141028)));
            Assert.False(rectangle4.Contains(new PdfPoint(876.1469290255752, 977.6220496848591)));
            Assert.False(rectangle4.Contains(new PdfPoint(800.9401149116893, 395.4873173955958)));
            Assert.False(rectangle4.Contains(new PdfPoint(906.3614397158185, 241.70531616419288)));
            Assert.False(rectangle4.Contains(new PdfPoint(830.8149315200259, 586.9741177384781)));
            Assert.True(rectangle4.Contains(new PdfPoint(344.0788636255755, 181.97071150147158)));
            Assert.True(rectangle4.Contains(new PdfPoint(448.88417556372406, 249.18035940106364)));
            Assert.False(rectangle4.Contains(new PdfPoint(539.0738980024036, 389.2124263604438)));
            Assert.True(rectangle4.Contains(new PdfPoint(435.2544223448116, 210.8032253465768)));
            Assert.False(rectangle4.Contains(new PdfPoint(705.5475399465194, 574.8559155688396)));
            Assert.True(rectangle4.Contains(new PdfPoint(594.1559093581004, 355.69416179006663)));
            Assert.True(rectangle4.Contains(new PdfPoint(412.82394222471765, 127.0938582360459)));
            Assert.False(rectangle4.Contains(new PdfPoint(587.1061737052607, 77.6418956945213)));
            Assert.False(rectangle4.Contains(new PdfPoint(209.16452295361077, 858.1778637009602)));
            Assert.False(rectangle4.Contains(new PdfPoint(161.2472615486529, 196.57579681357763)));
            Assert.False(rectangle4.Contains(new PdfPoint(588.1152549305227, 793.9163431203691)));
            Assert.False(rectangle4.Contains(new PdfPoint(419.08428311983016, 682.940017491896)));


            PdfRectangle rectangle5 = new PdfRectangle(new PdfPoint(936.1838457735406, 938.4568236371585), new PdfPoint(469.11519762997943, 938.4568236371585), new PdfPoint(936.1838457735406, 570.546962665707), new PdfPoint(469.11519762997943, 570.546962665707));

            Assert.False(rectangle5.Contains(new PdfPoint(363.6763214828964, 553.90419673017)));
            Assert.False(rectangle5.Contains(new PdfPoint(269.11022001568995, 907.7211013369512)));
            Assert.False(rectangle5.Contains(new PdfPoint(545.9247943971187, 467.2045212920185)));
            Assert.False(rectangle5.Contains(new PdfPoint(957.854368553749, 743.5297047037499)));
            Assert.False(rectangle5.Contains(new PdfPoint(96.22391424834854, 867.8498123277581)));
            Assert.False(rectangle5.Contains(new PdfPoint(947.4676645726615, 841.4543556976749)));
            Assert.False(rectangle5.Contains(new PdfPoint(207.58387888714313, 944.6083173703909)));
            Assert.False(rectangle5.Contains(new PdfPoint(12.070767054576216, 530.89689237058)));
            Assert.False(rectangle5.Contains(new PdfPoint(80.97174825482334, 583.3836110476417)));
            Assert.False(rectangle5.Contains(new PdfPoint(254.72654444548726, 400.535850508089)));
            Assert.False(rectangle5.Contains(new PdfPoint(383.8062039614576, 215.97802102071296)));
            Assert.True(rectangle5.Contains(new PdfPoint(558.2282435794427, 861.1564197263626)));
            Assert.False(rectangle5.Contains(new PdfPoint(468.3442214235617, 124.51907061851975)));
            Assert.False(rectangle5.Contains(new PdfPoint(306.0817906101611, 491.1373171345088)));
            Assert.False(rectangle5.Contains(new PdfPoint(0.652571433107263, 790.7203375108246)));
            Assert.False(rectangle5.Contains(new PdfPoint(133.65649584827932, 120.35392855410065)));
            Assert.False(rectangle5.Contains(new PdfPoint(145.0509582539651, 278.46839438943783)));
            Assert.False(rectangle5.Contains(new PdfPoint(993.9850498426819, 233.99784697488047)));
            Assert.False(rectangle5.Contains(new PdfPoint(115.04632034090689, 345.5836747158564)));
            Assert.False(rectangle5.Contains(new PdfPoint(439.30650931376914, 336.1235681440722)));
        }

        [Fact]
        public void Translate()
        {
            var tm = TransformationMatrix.GetTranslationMatrix(5, 7);

            var translated = tm.Transform(UnitRectangle);

            Assert.Equal(new PdfPoint(5, 7), translated.BottomLeft);
            Assert.Equal(new PdfPoint(6, 7), translated.BottomRight);
            Assert.Equal(new PdfPoint(5, 8), translated.TopLeft);
            Assert.Equal(new PdfPoint(6, 8), translated.TopRight);

            Assert.Equal(1, translated.Width);
            Assert.Equal(1, translated.Height);
        }

        [Fact]
        public void Scale()
        {
            var tm = TransformationMatrix.GetScaleMatrix(3, 5);

            var scaled = tm.Transform(UnitRectangle);

            Assert.Equal(new PdfPoint(0, 0), scaled.BottomLeft);
            Assert.Equal(new PdfPoint(3, 0), scaled.BottomRight);
            Assert.Equal(new PdfPoint(0, 5), scaled.TopLeft);
            Assert.Equal(new PdfPoint(3, 5), scaled.TopRight);

            Assert.Equal(3, scaled.Width);
            Assert.Equal(5, scaled.Height);
        }

        [Fact]
        public void Rotate360()
        {
            var tm = TransformationMatrix.GetRotationMatrix(360);

            var rotated = tm.Transform(UnitRectangle);

            Assert.Equal(new PdfPoint(0, 0), rotated.BottomLeft);
            Assert.Equal(new PdfPoint(1, 0), rotated.BottomRight);
            Assert.Equal(new PdfPoint(0, 1), rotated.TopLeft);
            Assert.Equal(new PdfPoint(1, 1), rotated.TopRight);

            Assert.Equal(1, rotated.Width);
            Assert.Equal(1, rotated.Height);
        }

        [Fact]
        public void Rotate90()
        {
            var tm = TransformationMatrix.GetRotationMatrix(90);

            var rotated = tm.Transform(UnitRectangle);

            Assert.Equal(new PdfPoint(0, 0), rotated.BottomLeft);
            Assert.Equal(new PdfPoint(0, 1), rotated.BottomRight);
            Assert.Equal(new PdfPoint(-1, 0), rotated.TopLeft);
            Assert.Equal(new PdfPoint(-1, 1), rotated.TopRight);

            Assert.Equal(1, rotated.Width, PreciseDoubleComparer);
            Assert.Equal(-1, rotated.Height, PreciseDoubleComparer);
            Assert.Equal(90, rotated.Rotation, PreciseDoubleComparer);
        }

        [Fact]
        public void Rotate180()
        {
            var tm = TransformationMatrix.GetRotationMatrix(180);

            var rotated = tm.Transform(UnitRectangle);

            Assert.Equal(new PdfPoint(0, 0), rotated.BottomLeft);
            Assert.Equal(new PdfPoint(-1, 0), rotated.BottomRight);
            Assert.Equal(new PdfPoint(0, -1), rotated.TopLeft);
            Assert.Equal(new PdfPoint(-1, -1), rotated.TopRight);

            Assert.Equal(-1, rotated.Width, PreciseDoubleComparer);
            Assert.Equal(-1, rotated.Height, PreciseDoubleComparer);
            Assert.Equal(180, rotated.Rotation, PreciseDoubleComparer);
        }

        public static IEnumerable<object[]> RotateData => new[]
        {
            new object[]
            {
                new double[][]
                {
                    new double[] { 100.11345, 50.24535, 150.24853, 100.77937 }, // AABB points
                    new double[] { 30 } // rotation angle
                },
                new PdfPoint[]
                {
                    // OBB points
                    new PdfPoint(104.99636886126835, 118.63801452204044),
                    new PdfPoint(79.72935886126837, 162.40175959739133),
                    new PdfPoint(36.31110596050322, 137.33421959739135),
                    new PdfPoint(61.57811596050321, 93.57047452204044)
                }
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 256.8793214, 72.7342895, 571.548482, 243.721896 },
                    new double[] { -78.14568 }
                },
                new PdfPoint[]
                {
                    new PdfPoint(188.5928589557637, -544.4177419008914),
                    new PdfPoint(355.93382538513987, -509.2927859424674),
                    new PdfPoint(291.2932316380764, -201.33455131845915),
                    new PdfPoint(123.95226520870021, -236.45950727688313)
                }
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 78.14, 48.49, -741.115482, -245.18796 },
                    new double[] { 178.215 }
                },
                new PdfPoint[]
                {
                    new PdfPoint(739.2454360229797, -71.55154141796652),
                    new PdfPoint(748.3932365836752, 221.98391118471883),
                    new PdfPoint(-70.46470122718794, 247.50297212341658),
                    new PdfPoint(-79.61250178788342, -46.03248047926875)
                }
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 594.9624245956629, 764.989849297414, 184.2241612768326, 272.5808412761548 },
                    new double[] { 45 }
                },
                new PdfPoint[]
                {
                    new PdfPoint(-410.66335627982403, 671.1956636743291),
                    new PdfPoint(-62.47760759065051, 323.00991498515555),
                    new PdfPoint(227.9582036948802, 613.4457262706862),
                    new PdfPoint(-120.22754499429334, 961.6314749598598)
                }
            },
            new object[]
            {
                new double[][]
                {
                    new double[] { 877.5628740259508, 768.3577588617107, 471.16446155789913, 881.5192835958403 },
                    new double[] { -45 }
                },
                new PdfPoint[]
                {
                    new PdfPoint(876.4745674901126, 210.14739584671491),
                    new PdfPoint(956.4918489990248, 290.16467735562713),
                    new PdfPoint(1243.8589223186318, 2.797604036020175),
                    new PdfPoint(1163.8416408097196, -77.21967747289204)
                }
            }
        };

        [Theory]
        [MemberData(nameof(RotateData))]
        public void Rotate(double[][] data, PdfPoint[] expected)
        {
            var points = data[0];
            var angle = data[1][0];

            var rect = new PdfRectangle(points[0], points[1], points[2], points[3]);
            var rectR = TransformationMatrix.GetRotationMatrix(angle).Transform(rect);

            Assert.Equal(expected[0], rectR.BottomRight, PointComparer);
            Assert.Equal(expected[1], rectR.TopRight, PointComparer);
            Assert.Equal(expected[2], rectR.TopLeft, PointComparer);
            Assert.Equal(expected[3], rectR.BottomLeft, PointComparer);
        }
    }
}
