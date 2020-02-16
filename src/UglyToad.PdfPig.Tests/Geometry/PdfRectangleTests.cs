namespace UglyToad.PdfPig.Tests.Geometry
{
    using PdfPig.Geometry;
    using PdfPig.Core;
    using Xunit;

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
            Assert.Equal(100d, rectangle.Area);

            PdfRectangle rectangle1 = new PdfRectangle(149.95376d, 687.13456d, 451.73539d, 1478.4997d);
            Assert.Equal(238819.4618743782d, rectangle1.Area, DoubleComparer);

            PdfRectangle rectangle2 = new PdfRectangle(558.1943459048198, 730.6304475255059, 571.3017547365034, 962.7770981990296);
            Assert.Equal(3042.841059283922, rectangle2.Area);
            var tm2 = TransformationMatrix.GetRotationMatrix(46.63115240869564);
            Assert.Equal(3042.841059283922, tm2.Transform(rectangle2).Area);
            
            PdfRectangle rectangle3 = new PdfRectangle(523.0784251391958, 417.882005884581, 248.94510082667455, 897.0802138593754);
            Assert.Equal(131364.1977567333, rectangle3.Area);
            var tm3 = TransformationMatrix.GetRotationMatrix(-84.98758555772564);
            Assert.Equal(131364.1977567333, tm3.Transform(rectangle3).Area);
        }

        [Fact]
        public void Centroid()
        {
            PdfRectangle rectangle = new PdfRectangle(10, 10, 20, 20);
            Assert.Equal(new PdfPoint(15, 15), rectangle.Centroid);

            PdfRectangle rectangle1 = new PdfRectangle(149.95376d, 687.13456d, 451.73539d, 1478.4997d);
            Assert.Equal(new PdfPoint(300.844575d, 1082.81713d), rectangle1.Centroid,
                PointComparer);

            var rectangle2 = new PdfRectangle(250.6895432725822, 314.1665092335595, 609.5698098944074, 210.305632858515);
            Assert.Equal(new PdfPoint(430.12967658349476, 262.23607104603724), rectangle2.Centroid, PointComparer);
            var tm2 = TransformationMatrix.GetRotationMatrix(189.25654524492694);
            Assert.Equal(new PdfPoint(-382.3464610180157, -328.00987695873295), tm2.Transform(rectangle2).Centroid, PointComparer);
            
            var rectangle3 = new PdfRectangle(29.760198327045796, 57.91029427107597, 49.486162626911856, 135.5406100042995);
            Assert.Equal(new PdfPoint(39.62318047697882, 96.72545213768774), rectangle3.Centroid, PointComparer);
            var tm3 = TransformationMatrix.GetRotationMatrix(107.38710946826126);
            Assert.Equal(new PdfPoint(-104.14627286173634, 8.908612201691023), tm3.Transform(rectangle3).Centroid, PointComparer);
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
            Assert.False(rectangle1.IntersectsWith(rectangle4)); // special case where they share one border
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


            PdfRectangle rectangle1 = new PdfRectangle(
                new PdfPoint(-9.065741219039126, 152.06038717005336),
                new PdfPoint(561.5649100338235, 794.7285258775772),
                new PdfPoint(337.3202440365057, -155.49875330445175),
                new PdfPoint(907.9508952893684, 487.16938540307206));

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


            PdfRectangle rectangle2 = new PdfRectangle(
                new PdfPoint(0.3057755364282002, 838.311937987381),
                new PdfPoint(700.7384584344007, 1011.32036557429),
                new PdfPoint(205.6195042611102, 7.089737703669428),
                new PdfPoint(906.0521871590828, 180.09816529057827));

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


            PdfRectangle rectangle3 = new PdfRectangle(
                new PdfPoint(493.4136678771659, 550.5731610863402),
                new PdfPoint(290.7393736458551, 237.61572595373514),
                new PdfPoint(306.45151010410984, 671.6516820633589),
                new PdfPoint(103.77721587279905, 358.6942469307538));

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

        [Fact]
        public void Rotate()
        {
            // 1
            var rect0 = new PdfRectangle(100.11345, 50.24535, 150.24853, 100.77937);
            var tm0 = TransformationMatrix.GetRotationMatrix(30);
            var rect0R = tm0.Transform(rect0);

            Assert.Equal(104.99636886126835, rect0R.BottomRight.X, 6);
            Assert.Equal(118.63801452204044, rect0R.BottomRight.Y, 6);

            Assert.Equal(79.72935886126837, rect0R.TopRight.X, 6);
            Assert.Equal(162.40175959739133, rect0R.TopRight.Y, 6);

            Assert.Equal(36.31110596050322, rect0R.TopLeft.X, 6);
            Assert.Equal(137.33421959739135, rect0R.TopLeft.Y, 6);

            Assert.Equal(61.57811596050321, rect0R.BottomLeft.X, 6);
            Assert.Equal(93.57047452204044, rect0R.BottomLeft.Y, 6);
            
            // 2
            var rect1 = new PdfRectangle(256.8793214, 72.7342895, 571.548482, 243.721896);
            var tm1 = TransformationMatrix.GetRotationMatrix(-78.14568);
            var rect1R = tm1.Transform(rect1);

            Assert.Equal(188.5928589557637, rect1R.BottomRight.X, 6);
            Assert.Equal(-544.4177419008914, rect1R.BottomRight.Y, 6);

            Assert.Equal(355.93382538513987, rect1R.TopRight.X, 6);
            Assert.Equal(-509.2927859424674, rect1R.TopRight.Y, 6);

            Assert.Equal(291.2932316380764, rect1R.TopLeft.X, 6);
            Assert.Equal(-201.33455131845915, rect1R.TopLeft.Y, 6);

            Assert.Equal(123.95226520870021, rect1R.BottomLeft.X, 6);
            Assert.Equal(-236.45950727688313, rect1R.BottomLeft.Y, 6);

            // 3
            var rect2 = new PdfRectangle(78.14, 48.49, -741.115482, -245.18796);
            var tm2 = TransformationMatrix.GetRotationMatrix(178.215);
            var rect2R = tm2.Transform(rect2);

            Assert.Equal(739.2454360229797, rect2R.BottomRight.X, 6);
            Assert.Equal(-71.55154141796652, rect2R.BottomRight.Y, 6);

            Assert.Equal(748.3932365836752, rect2R.TopRight.X, 6);
            Assert.Equal(221.98391118471883, rect2R.TopRight.Y, 6);

            Assert.Equal(-70.46470122718794, rect2R.TopLeft.X, 6);
            Assert.Equal(247.50297212341658, rect2R.TopLeft.Y, 6);

            Assert.Equal(-79.61250178788342, rect2R.BottomLeft.X, 6);
            Assert.Equal(-46.03248047926875, rect2R.BottomLeft.Y, 6);
        }
    }
}
