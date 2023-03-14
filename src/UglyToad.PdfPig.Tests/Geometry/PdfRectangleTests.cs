namespace UglyToad.PdfPig.Tests.Geometry
{
    using Content;
    using PdfPig.Geometry;
    using PdfPig.Core;
    using Xunit;
    using System.Collections.Generic;
    using System.Drawing;

    public class PdfRectangleTests
    {
        private static readonly DoubleComparer DoubleComparer = new DoubleComparer(3);
        private static readonly DoubleComparer PreciseDoubleComparer = new DoubleComparer(6);
        private static readonly PointComparer PointComparer = new PointComparer(DoubleComparer);
        private static readonly PdfRectangle UnitRectangle = new PdfRectangle(new PdfPoint(0, 0), new PdfPoint(1, 1));

        #region data
        public static IEnumerable<object[]> AreaData => new[]
        {
            new object[]
            {
                new double[] { 10, 10, 20, 20 },
                new double[] { 100 }
            },
            new object[]
            {
                new double[] { 149.95376, 687.13456, 451.73539, 1478.4997 },
                new double[] { 238819.4618743782 }
            },
            new object[]
            {
                new double[] { 558.1943459048198, 730.6304475255059, 571.3017547365034, 962.7770981990296 },
                new double[] { 3042.841059283922 }
            },
            new object[]
            {
                new double[] { 523.0784251391958, 417.882005884581, 248.94510082667455, 897.0802138593754 },
                new double[] { 131364.1977567333 }
            },
        };

        public static IEnumerable<object[]> CentroidData => new[]
        {
            new object[]
            {
                new PdfRectangle(10, 10, 20, 20),
                new PdfPoint(15, 15)
            },
            new object[]
            {
                new PdfRectangle(149.95376d, 687.13456d, 451.73539d, 1478.4997d),
                new PdfPoint(300.844575d, 1082.81713d)
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(592.6200870732596, 372.2269617072379),new PdfPoint(159.2856344622129, 372.2269617072379),new PdfPoint(592.6200870732596, 621.0227464469322),new PdfPoint(159.2856344622129, 621.0227464469322)),
                new PdfPoint(375.95286076773624, 496.6248540770851)
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(-417.6256801952062, -561.5516626865337),new PdfPoint(-13.696336517754986, -404.64457828299595),new PdfPoint(-327.53863320373307, -793.4647206815428),new PdfPoint(76.39071047371814, -636.5576362780049)),
                new PdfPoint(-170.61748486074404, -599.0546494822693)
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(584.4052014709364, 774.3259224843123),new PdfPoint(331.3359128603333, 774.3259224843123),new PdfPoint(584.4052014709364, 444.2908850171682),new PdfPoint(331.3359128603333, 444.2908850171682)),
                new PdfPoint(457.8705571656348, 609.3084037507404)
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(-916.2546401117772, -318.72795329419165),new PdfPoint(-705.9000590212588, -459.4227117328386),new PdfPoint(-732.7705037013028, -44.39841124368331),new PdfPoint(-522.4159226107844, -185.09316968233023)),
                new PdfPoint(-719.3352813612807, -251.91056148826095)
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(581.0189617667493, 26.86276818200073),new PdfPoint(60.67300054255931, 26.86276818200073),new PdfPoint(581.0189617667493, 468.153039406668),new PdfPoint(60.67300054255931, 468.153039406668)),
                new PdfPoint(320.8459811546543, 247.50790379433437)
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(-226.60727661627826, 535.680673938742),new PdfPoint(-46.229419003141246, 47.59897192029799),new PdfPoint(-640.5351682766459, 382.70746065241724),new PdfPoint(-460.15731066350884, -105.3742413660267)),
                new PdfPoint(-343.3822936398935, 215.15321628635763)
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(672.3361009294622, 71.31800246859233),new PdfPoint(255.73239267233106, 71.31800246859233),new PdfPoint(672.3361009294622, 36.01512756554415),new PdfPoint(255.73239267233106, 36.01512756554415)),
                new PdfPoint(464.0342468008967, 53.66656501706822)
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(-522.0658022024916, 429.61539574346153),new PdfPoint(-230.0978813627802, 132.43971885497714),new PdfPoint(-496.8832233212023, 454.35667091892316),new PdfPoint(-204.91530248149095, 157.18099403043874)),
                new PdfPoint(-363.49055234199125, 293.39819488695014)
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(408.7699900328854, 602.0071590035183),new PdfPoint(49.12912892317167, 602.0071590035183),new PdfPoint(408.7699900328854, 160.01073952544075),new PdfPoint(49.12912892317167, 160.01073952544075)),
                new PdfPoint(228.9495594780285, 381.0089492644795)
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(725.7830495944597, -52.38787230207646),new PdfPoint(545.699296988983, 258.9180721115596),new PdfPoint(343.1899472186625, -273.70970323138954),new PdfPoint(163.10619461318575, 37.5962411822465)),
                new PdfPoint(444.4446221038227, -7.3958155599149755)
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(932.2645941433747, 742.6191999724293),new PdfPoint(718.0199306431579, 742.6191999724293),new PdfPoint(932.2645941433747, 69.40004835396985),new PdfPoint(718.0199306431579, 69.40004835396985)),
                new PdfPoint(825.1422623932663, 406.0096241631996)
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(-1049.2664924484716, -565.3674711954019),new PdfPoint(-838.423230833022, -603.3923955172233),new PdfPoint(-929.7811018951297, 97.16348472303186),new PdfPoint(-718.9378402796801, 59.13856040121048)),
                new PdfPoint(-884.102166364076, -253.1144553970957)
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(945.0913480238154, 430.7156049514413),new PdfPoint(882.58313147243, 430.7156049514413),new PdfPoint(945.0913480238154, 311.26805989696203),new PdfPoint(882.58313147243, 311.26805989696203)),
                new PdfPoint(913.8372397481228, 370.9918324242017)
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(927.4639133931082, 467.4658038955358),new PdfPoint(865.0038306210123, 465.0132141341165),new PdfPoint(932.1505904763757, 348.11023813733993),new PdfPoint(869.6905077042798, 345.65764837592053)),
                new PdfPoint(898.5772105486939, 406.56172613572824)
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(767.254356003645, 185.41819358137778),new PdfPoint(417.81866424075787, 185.41819358137778),new PdfPoint(767.254356003645, 531.3182639304341),new PdfPoint(417.81866424075787, 531.3182639304341)),
                new PdfPoint(592.5365101222014, 358.3682287559059)
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(411.4202959883427, 673.6412200618471),new PdfPoint(164.33194875419298, 426.5528728276974),new PdfPoint(166.83201063162107, 918.2295054185687),new PdfPoint(-80.25633660252862, 671.141158184419)),
                new PdfPoint(165.58197969290703, 672.3911891231331)
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(2.6992354070100033, 557.5131796520894),new PdfPoint(818.0873603314451, 557.5131796520894),new PdfPoint(2.6992354070100033, 641.8615039266299),new PdfPoint(818.0873603314451, 641.8615039266299)),
                new PdfPoint(410.3932978692276, 599.6873417893597)
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(396.129997593182, 392.31270227255084),new PdfPoint(972.6964700262338, -184.253770160501),new PdfPoint(455.7732696694314, 451.9559743488002),new PdfPoint(1032.3397421024833, -124.61049808425162)),
                new PdfPoint(714.2348698478329, 133.85110209414952)
            }
        };

        public static IEnumerable<object[]> ContainsPointData => new[]
        {
            new object[]
            {
                new PdfRectangle(10, 10, 20, 20),
                new PdfPoint(15, 15),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(213.88521378702453, 603.860851692054),new PdfPoint(241.96276069530677, 603.860851692054),new PdfPoint(213.88521378702453, 35.34856158649746),new PdfPoint(241.96276069530677, 35.34856158649746)),
                new PdfPoint(208.57724948646694, 270.83131823652997),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(213.88521378702453, 603.860851692054),new PdfPoint(241.96276069530677, 603.860851692054),new PdfPoint(213.88521378702453, 35.34856158649746),new PdfPoint(241.96276069530677, 35.34856158649746)),
                new PdfPoint(971.5947140753032, 870.9854034519929),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(213.88521378702453, 603.860851692054),new PdfPoint(241.96276069530677, 603.860851692054),new PdfPoint(213.88521378702453, 35.34856158649746),new PdfPoint(241.96276069530677, 35.34856158649746)),
                new PdfPoint(568.4302748689614, 350.4244675595452),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(213.88521378702453, 603.860851692054),new PdfPoint(241.96276069530677, 603.860851692054),new PdfPoint(213.88521378702453, 35.34856158649746),new PdfPoint(241.96276069530677, 35.34856158649746)),
                new PdfPoint(771.4355819680759, 181.4047192115471),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(213.88521378702453, 603.860851692054),new PdfPoint(241.96276069530677, 603.860851692054),new PdfPoint(213.88521378702453, 35.34856158649746),new PdfPoint(241.96276069530677, 35.34856158649746)),
                new PdfPoint(72.00946640375017, 85.28903337567873),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(191.23542608425032, 483.5160904425162),new PdfPoint(456.45856859692503, 1114.9863470068417),new PdfPoint(389.2840191643495, 400.33391411531306),new PdfPoint(654.5071616770242, 1031.8041706796387)),
                new PdfPoint(895.945655910052, 556.9394970090755),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(191.23542608425032, 483.5160904425162),new PdfPoint(456.45856859692503, 1114.9863470068417),new PdfPoint(389.2840191643495, 400.33391411531306),new PdfPoint(654.5071616770242, 1031.8041706796387)),
                new PdfPoint(511.77044993252053, 692.9402388750799),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(191.23542608425032, 483.5160904425162),new PdfPoint(456.45856859692503, 1114.9863470068417),new PdfPoint(389.2840191643495, 400.33391411531306),new PdfPoint(654.5071616770242, 1031.8041706796387)),
                new PdfPoint(77.2008910976577, 640.3350732586033),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(191.23542608425032, 483.5160904425162),new PdfPoint(456.45856859692503, 1114.9863470068417),new PdfPoint(389.2840191643495, 400.33391411531306),new PdfPoint(654.5071616770242, 1031.8041706796387)),
                new PdfPoint(312.4950045194459, 449.64884940855995),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(191.23542608425032, 483.5160904425162),new PdfPoint(456.45856859692503, 1114.9863470068417),new PdfPoint(389.2840191643495, 400.33391411531306),new PdfPoint(654.5071616770242, 1031.8041706796387)),
                new PdfPoint(378.64478888962594, 821.4562402496276),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(191.23542608425032, 483.5160904425162),new PdfPoint(456.45856859692503, 1114.9863470068417),new PdfPoint(389.2840191643495, 400.33391411531306),new PdfPoint(654.5071616770242, 1031.8041706796387)),
                new PdfPoint(545.9037673562783, 668.6886522945939),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(191.23542608425032, 483.5160904425162),new PdfPoint(456.45856859692503, 1114.9863470068417),new PdfPoint(389.2840191643495, 400.33391411531306),new PdfPoint(654.5071616770242, 1031.8041706796387)),
                new PdfPoint(419.06785990120267, 410.26519575444667),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(191.23542608425032, 483.5160904425162),new PdfPoint(456.45856859692503, 1114.9863470068417),new PdfPoint(389.2840191643495, 400.33391411531306),new PdfPoint(654.5071616770242, 1031.8041706796387)),
                new PdfPoint(406.6101614394935, 781.4382755058142),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(191.23542608425032, 483.5160904425162),new PdfPoint(456.45856859692503, 1114.9863470068417),new PdfPoint(389.2840191643495, 400.33391411531306),new PdfPoint(654.5071616770242, 1031.8041706796387)),
                new PdfPoint(791.3234274319645, 960.0698980698595),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(191.23542608425032, 483.5160904425162),new PdfPoint(456.45856859692503, 1114.9863470068417),new PdfPoint(389.2840191643495, 400.33391411531306),new PdfPoint(654.5071616770242, 1031.8041706796387)),
                new PdfPoint(162.95502619898394, 137.20530855807135),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(929.5515633307455, 91.89679953571533),new PdfPoint(675.8520733683064, 343.8331492504317),new PdfPoint(751.4752168483437, -87.42578860258897),new PdfPoint(497.77572688590465, 164.51056111212733)),
                new PdfPoint(535.8191915976803, 194.8410071221891),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(929.5515633307455, 91.89679953571533),new PdfPoint(675.8520733683064, 343.8331492504317),new PdfPoint(751.4752168483437, -87.42578860258897),new PdfPoint(497.77572688590465, 164.51056111212733)),
                new PdfPoint(736.9890030162604, 714.3256237329208),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(929.5515633307455, 91.89679953571533),new PdfPoint(675.8520733683064, 343.8331492504317),new PdfPoint(751.4752168483437, -87.42578860258897),new PdfPoint(497.77572688590465, 164.51056111212733)),
                new PdfPoint(253.3629775906685, 988.7849400080352),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(929.5515633307455, 91.89679953571533),new PdfPoint(675.8520733683064, 343.8331492504317),new PdfPoint(751.4752168483437, -87.42578860258897),new PdfPoint(497.77572688590465, 164.51056111212733)),
                new PdfPoint(97.85750906716939, 948.4917976826289),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(929.5515633307455, 91.89679953571533),new PdfPoint(675.8520733683064, 343.8331492504317),new PdfPoint(751.4752168483437, -87.42578860258897),new PdfPoint(497.77572688590465, 164.51056111212733)),
                new PdfPoint(259.64877651804363, 218.8415178009987),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(929.5515633307455, 91.89679953571533),new PdfPoint(675.8520733683064, 343.8331492504317),new PdfPoint(751.4752168483437, -87.42578860258897),new PdfPoint(497.77572688590465, 164.51056111212733)),
                new PdfPoint(810.6516797331832, 209.7658061521339),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(929.5515633307455, 91.89679953571533),new PdfPoint(675.8520733683064, 343.8331492504317),new PdfPoint(751.4752168483437, -87.42578860258897),new PdfPoint(497.77572688590465, 164.51056111212733)),
                new PdfPoint(729.7486361016389, 242.39990620079587),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(929.5515633307455, 91.89679953571533),new PdfPoint(675.8520733683064, 343.8331492504317),new PdfPoint(751.4752168483437, -87.42578860258897),new PdfPoint(497.77572688590465, 164.51056111212733)),
                new PdfPoint(132.8526942231918, 407.7275248445794),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(929.5515633307455, 91.89679953571533),new PdfPoint(675.8520733683064, 343.8331492504317),new PdfPoint(751.4752168483437, -87.42578860258897),new PdfPoint(497.77572688590465, 164.51056111212733)),
                new PdfPoint(808.6821547281769, 508.7206490497369),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(929.5515633307455, 91.89679953571533),new PdfPoint(675.8520733683064, 343.8331492504317),new PdfPoint(751.4752168483437, -87.42578860258897),new PdfPoint(497.77572688590465, 164.51056111212733)),
                new PdfPoint(634.0941576639729, 739.4743635907182),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(463.3021786709886, 569.3442947257327),new PdfPoint(937.8572273684033, -9.588405237358614),new PdfPoint(239.53538448431482, 385.9211404991856),new PdfPoint(714.0904331817294, -193.01155946390563)),
                new PdfPoint(630.4258586676584, 194.75092909995516),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(463.3021786709886, 569.3442947257327),new PdfPoint(937.8572273684033, -9.588405237358614),new PdfPoint(239.53538448431482, 385.9211404991856),new PdfPoint(714.0904331817294, -193.01155946390563)),
                new PdfPoint(285.96373242624816, 106.23261355535318),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(463.3021786709886, 569.3442947257327),new PdfPoint(937.8572273684033, -9.588405237358614),new PdfPoint(239.53538448431482, 385.9211404991856),new PdfPoint(714.0904331817294, -193.01155946390563)),
                new PdfPoint(567.2768928079599, 711.0961120097492),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(463.3021786709886, 569.3442947257327),new PdfPoint(937.8572273684033, -9.588405237358614),new PdfPoint(239.53538448431482, 385.9211404991856),new PdfPoint(714.0904331817294, -193.01155946390563)),
                new PdfPoint(389.8437756190869, 11.131952307614878),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(463.3021786709886, 569.3442947257327),new PdfPoint(937.8572273684033, -9.588405237358614),new PdfPoint(239.53538448431482, 385.9211404991856),new PdfPoint(714.0904331817294, -193.01155946390563)),
                new PdfPoint(858.0642478752313, 391.80071350371514),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(463.3021786709886, 569.3442947257327),new PdfPoint(937.8572273684033, -9.588405237358614),new PdfPoint(239.53538448431482, 385.9211404991856),new PdfPoint(714.0904331817294, -193.01155946390563)),
                new PdfPoint(737.6859931857093, 324.61256822831365),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(463.3021786709886, 569.3442947257327),new PdfPoint(937.8572273684033, -9.588405237358614),new PdfPoint(239.53538448431482, 385.9211404991856),new PdfPoint(714.0904331817294, -193.01155946390563)),
                new PdfPoint(170.8020133249012, 992.387982573134),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(463.3021786709886, 569.3442947257327),new PdfPoint(937.8572273684033, -9.588405237358614),new PdfPoint(239.53538448431482, 385.9211404991856),new PdfPoint(714.0904331817294, -193.01155946390563)),
                new PdfPoint(810.884181359447, 643.9886938230205),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(463.3021786709886, 569.3442947257327),new PdfPoint(937.8572273684033, -9.588405237358614),new PdfPoint(239.53538448431482, 385.9211404991856),new PdfPoint(714.0904331817294, -193.01155946390563)),
                new PdfPoint(730.8618205162832, 259.75583157719484),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(463.3021786709886, 569.3442947257327),new PdfPoint(937.8572273684033, -9.588405237358614),new PdfPoint(239.53538448431482, 385.9211404991856),new PdfPoint(714.0904331817294, -193.01155946390563)),
                new PdfPoint(538.148744391793, 911.6976397515474),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(728.9178455075204, 11.677989018639892),new PdfPoint(220.79461531833186, 168.04564293794076),new PdfPoint(812.9967549244197, 284.895921070927),new PdfPoint(304.87352473523123, 441.2635749902279)),
                new PdfPoint(299.58547302448744, 901.1681504320834),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(728.9178455075204, 11.677989018639892),new PdfPoint(220.79461531833186, 168.04564293794076),new PdfPoint(812.9967549244197, 284.895921070927),new PdfPoint(304.87352473523123, 441.2635749902279)),
                new PdfPoint(493.63594622226003, 362.79978963246117),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(728.9178455075204, 11.677989018639892),new PdfPoint(220.79461531833186, 168.04564293794076),new PdfPoint(812.9967549244197, 284.895921070927),new PdfPoint(304.87352473523123, 441.2635749902279)),
                new PdfPoint(950.6460859747472, 265.98693980610165),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(728.9178455075204, 11.677989018639892),new PdfPoint(220.79461531833186, 168.04564293794076),new PdfPoint(812.9967549244197, 284.895921070927),new PdfPoint(304.87352473523123, 441.2635749902279)),
                new PdfPoint(717.804272137585, 561.8643879709465),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(728.9178455075204, 11.677989018639892),new PdfPoint(220.79461531833186, 168.04564293794076),new PdfPoint(812.9967549244197, 284.895921070927),new PdfPoint(304.87352473523123, 441.2635749902279)),
                new PdfPoint(298.0962509777869, 500.0389774876396),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(728.9178455075204, 11.677989018639892),new PdfPoint(220.79461531833186, 168.04564293794076),new PdfPoint(812.9967549244197, 284.895921070927),new PdfPoint(304.87352473523123, 441.2635749902279)),
                new PdfPoint(431.42450534948074, 330.27535360136983),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(728.9178455075204, 11.677989018639892),new PdfPoint(220.79461531833186, 168.04564293794076),new PdfPoint(812.9967549244197, 284.895921070927),new PdfPoint(304.87352473523123, 441.2635749902279)),
                new PdfPoint(175.4845158655851, 524.9753939326548),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(728.9178455075204, 11.677989018639892),new PdfPoint(220.79461531833186, 168.04564293794076),new PdfPoint(812.9967549244197, 284.895921070927),new PdfPoint(304.87352473523123, 441.2635749902279)),
                new PdfPoint(306.26023178142844, 441.4370751156109),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(728.9178455075204, 11.677989018639892),new PdfPoint(220.79461531833186, 168.04564293794076),new PdfPoint(812.9967549244197, 284.895921070927),new PdfPoint(304.87352473523123, 441.2635749902279)),
                new PdfPoint(899.3362429001232, 993.212562197227),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(728.9178455075204, 11.677989018639892),new PdfPoint(220.79461531833186, 168.04564293794076),new PdfPoint(812.9967549244197, 284.895921070927),new PdfPoint(304.87352473523123, 441.2635749902279)),
                new PdfPoint(868.5056515455499, 124.50598711255789),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(632.3048566943746, 697.5675116477437),new PdfPoint(653.7914180538546, 605.8879721127291),new PdfPoint(1022.9155026615887, 789.1133424275242),new PdfPoint(1044.4020640210683, 697.4338028925096)),
                new PdfPoint(595.040199991622, 838.3984524069945),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(632.3048566943746, 697.5675116477437),new PdfPoint(653.7914180538546, 605.8879721127291),new PdfPoint(1022.9155026615887, 789.1133424275242),new PdfPoint(1044.4020640210683, 697.4338028925096)),
                new PdfPoint(874.3846046175529, 296.2125747305683),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(632.3048566943746, 697.5675116477437),new PdfPoint(653.7914180538546, 605.8879721127291),new PdfPoint(1022.9155026615887, 789.1133424275242),new PdfPoint(1044.4020640210683, 697.4338028925096)),
                new PdfPoint(113.06514184563542, 357.38451786138023),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(632.3048566943746, 697.5675116477437),new PdfPoint(653.7914180538546, 605.8879721127291),new PdfPoint(1022.9155026615887, 789.1133424275242),new PdfPoint(1044.4020640210683, 697.4338028925096)),
                new PdfPoint(776.4317211123572, 773.6886303752727),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(632.3048566943746, 697.5675116477437),new PdfPoint(653.7914180538546, 605.8879721127291),new PdfPoint(1022.9155026615887, 789.1133424275242),new PdfPoint(1044.4020640210683, 697.4338028925096)),
                new PdfPoint(777.7784744439956, 790.8799692967996),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(632.3048566943746, 697.5675116477437),new PdfPoint(653.7914180538546, 605.8879721127291),new PdfPoint(1022.9155026615887, 789.1133424275242),new PdfPoint(1044.4020640210683, 697.4338028925096)),
                new PdfPoint(363.86926458892276, 48.06857758740812),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(632.3048566943746, 697.5675116477437),new PdfPoint(653.7914180538546, 605.8879721127291),new PdfPoint(1022.9155026615887, 789.1133424275242),new PdfPoint(1044.4020640210683, 697.4338028925096)),
                new PdfPoint(108.45986205339719, 575.9689474208977),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(632.3048566943746, 697.5675116477437),new PdfPoint(653.7914180538546, 605.8879721127291),new PdfPoint(1022.9155026615887, 789.1133424275242),new PdfPoint(1044.4020640210683, 697.4338028925096)),
                new PdfPoint(762.5874569148565, 983.0109589584148),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(632.3048566943746, 697.5675116477437),new PdfPoint(653.7914180538546, 605.8879721127291),new PdfPoint(1022.9155026615887, 789.1133424275242),new PdfPoint(1044.4020640210683, 697.4338028925096)),
                new PdfPoint(964.044700762544, 999.7371547211853),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(632.3048566943746, 697.5675116477437),new PdfPoint(653.7914180538546, 605.8879721127291),new PdfPoint(1022.9155026615887, 789.1133424275242),new PdfPoint(1044.4020640210683, 697.4338028925096)),
                new PdfPoint(314.0181205534643, 811.8272366437633),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(286.1900584013865, 971.0125767475948),new PdfPoint(132.66516303511207, 640.7380642946706),new PdfPoint(340.10815606609424, 945.9492733735487),new PdfPoint(186.5832606998198, 615.6747609206245)),
                new PdfPoint(675.9599516095979, 529.9073129185338),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(286.1900584013865, 971.0125767475948),new PdfPoint(132.66516303511207, 640.7380642946706),new PdfPoint(340.10815606609424, 945.9492733735487),new PdfPoint(186.5832606998198, 615.6747609206245)),
                new PdfPoint(278.12040266418205, 269.7344601358416),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(286.1900584013865, 971.0125767475948),new PdfPoint(132.66516303511207, 640.7380642946706),new PdfPoint(340.10815606609424, 945.9492733735487),new PdfPoint(186.5832606998198, 615.6747609206245)),
                new PdfPoint(834.1502437432001, 894.0079244298258),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(286.1900584013865, 971.0125767475948),new PdfPoint(132.66516303511207, 640.7380642946706),new PdfPoint(340.10815606609424, 945.9492733735487),new PdfPoint(186.5832606998198, 615.6747609206245)),
                new PdfPoint(6.323717481052982, 314.9392105513358),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(286.1900584013865, 971.0125767475948),new PdfPoint(132.66516303511207, 640.7380642946706),new PdfPoint(340.10815606609424, 945.9492733735487),new PdfPoint(186.5832606998198, 615.6747609206245)),
                new PdfPoint(790.3438956848015, 97.8466749454916),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(286.1900584013865, 971.0125767475948),new PdfPoint(132.66516303511207, 640.7380642946706),new PdfPoint(340.10815606609424, 945.9492733735487),new PdfPoint(186.5832606998198, 615.6747609206245)),
                new PdfPoint(657.5362645723387, 990.6433674976283),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(286.1900584013865, 971.0125767475948),new PdfPoint(132.66516303511207, 640.7380642946706),new PdfPoint(340.10815606609424, 945.9492733735487),new PdfPoint(186.5832606998198, 615.6747609206245)),
                new PdfPoint(618.3638207473555, 824.711921861931),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(286.1900584013865, 971.0125767475948),new PdfPoint(132.66516303511207, 640.7380642946706),new PdfPoint(340.10815606609424, 945.9492733735487),new PdfPoint(186.5832606998198, 615.6747609206245)),
                new PdfPoint(413.944649218694, 210.09342281638567),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(286.1900584013865, 971.0125767475948),new PdfPoint(132.66516303511207, 640.7380642946706),new PdfPoint(340.10815606609424, 945.9492733735487),new PdfPoint(186.5832606998198, 615.6747609206245)),
                new PdfPoint(770.2417014353105, 193.652981248717),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(286.1900584013865, 971.0125767475948),new PdfPoint(132.66516303511207, 640.7380642946706),new PdfPoint(340.10815606609424, 945.9492733735487),new PdfPoint(186.5832606998198, 615.6747609206245)),
                new PdfPoint(172.64880401177106, 143.05895678058556),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(461.667508963458, 733.7624399217582),new PdfPoint(781.2509306058209, 857.2579423172539),new PdfPoint(523.4024069573737, 574.0039908046879),new PdfPoint(842.9858285997368, 697.4994932001836)),
                new PdfPoint(737.2771198889629, 147.18255415581672),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(461.667508963458, 733.7624399217582),new PdfPoint(781.2509306058209, 857.2579423172539),new PdfPoint(523.4024069573737, 574.0039908046879),new PdfPoint(842.9858285997368, 697.4994932001836)),
                new PdfPoint(92.73408485526468, 491.4585810695519),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(461.667508963458, 733.7624399217582),new PdfPoint(781.2509306058209, 857.2579423172539),new PdfPoint(523.4024069573737, 574.0039908046879),new PdfPoint(842.9858285997368, 697.4994932001836)),
                new PdfPoint(564.382015519254, 519.9452998210951),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(461.667508963458, 733.7624399217582),new PdfPoint(781.2509306058209, 857.2579423172539),new PdfPoint(523.4024069573737, 574.0039908046879),new PdfPoint(842.9858285997368, 697.4994932001836)),
                new PdfPoint(836.4756151541413, 286.03540414744276),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(461.667508963458, 733.7624399217582),new PdfPoint(781.2509306058209, 857.2579423172539),new PdfPoint(523.4024069573737, 574.0039908046879),new PdfPoint(842.9858285997368, 697.4994932001836)),
                new PdfPoint(202.4478685402723, 456.84876852482216),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(461.667508963458, 733.7624399217582),new PdfPoint(781.2509306058209, 857.2579423172539),new PdfPoint(523.4024069573737, 574.0039908046879),new PdfPoint(842.9858285997368, 697.4994932001836)),
                new PdfPoint(981.0642239223686, 823.9192903632229),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(461.667508963458, 733.7624399217582),new PdfPoint(781.2509306058209, 857.2579423172539),new PdfPoint(523.4024069573737, 574.0039908046879),new PdfPoint(842.9858285997368, 697.4994932001836)),
                new PdfPoint(100.17478667767676, 795.3438030024145),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(461.667508963458, 733.7624399217582),new PdfPoint(781.2509306058209, 857.2579423172539),new PdfPoint(523.4024069573737, 574.0039908046879),new PdfPoint(842.9858285997368, 697.4994932001836)),
                new PdfPoint(54.961291211024665, 370.17803171870736),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(461.667508963458, 733.7624399217582),new PdfPoint(781.2509306058209, 857.2579423172539),new PdfPoint(523.4024069573737, 574.0039908046879),new PdfPoint(842.9858285997368, 697.4994932001836)),
                new PdfPoint(683.785773260759, 204.25666639966843),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(461.667508963458, 733.7624399217582),new PdfPoint(781.2509306058209, 857.2579423172539),new PdfPoint(523.4024069573737, 574.0039908046879),new PdfPoint(842.9858285997368, 697.4994932001836)),
                new PdfPoint(878.9271679900206, 714.2861610955317),
                false
            }
        };

        public static IEnumerable<object[]> ContainsRectangleData => new[]
        {
            new object[]
            {
                new PdfRectangle(new PdfPoint(839.1046803732955, 810.757367808191),new PdfPoint(895.4266449123043, 511.57035593138903),new PdfPoint(602.1120254124551, 766.1434929445111),new PdfPoint(658.4339899514637, 466.95648106770903)),
                new PdfRectangle(new PdfPoint(839.1046803732955, 810.757367808191),new PdfPoint(895.4266449123043, 511.57035593138903),new PdfPoint(602.1120254124551, 766.1434929445111),new PdfPoint(658.4339899514637, 466.95648106770903)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(369.92427264664883, 246.08869240919273),new PdfPoint(811.9614880217224, 257.0837364279697),new PdfPoint(375.3394031464051, 28.382496516114173),new PdfPoint(817.3766185214787, 39.37754053489114)),
                new PdfRectangle(new PdfPoint(369.92427264664883, 246.08869240919273),new PdfPoint(811.9614880217224, 257.0837364279697),new PdfPoint(375.3394031464051, 28.382496516114173),new PdfPoint(817.3766185214787, 39.37754053489114)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(763.7211043674602, 907.9508951176988),new PdfPoint(627.6855729103308, 775.4472008683426),new PdfPoint(989.9021602319854, 675.7410664455838),new PdfPoint(853.866628774856, 543.2373721962276)),
                new PdfRectangle(new PdfPoint(763.7211043674602, 907.9508951176988),new PdfPoint(627.6855729103308, 775.4472008683426),new PdfPoint(989.9021602319854, 675.7410664455838),new PdfPoint(853.866628774856, 543.2373721962276)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(787.2830652212485, 909.5937859675068),new PdfPoint(1051.649076209865, 697.0164728885809),new PdfPoint(460.468733614018, 503.16000463150783),new PdfPoint(724.8347446026344, 290.5826915525819)),
                new PdfRectangle(new PdfPoint(839.1046803732955, 810.757367808191),new PdfPoint(895.4266449123043, 511.57035593138903),new PdfPoint(602.1120254124551, 766.1434929445111),new PdfPoint(658.4339899514637, 466.95648106770903)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(787.2830652212485, 909.5937859675068),new PdfPoint(1051.649076209865, 697.0164728885809),new PdfPoint(460.468733614018, 503.16000463150783),new PdfPoint(724.8347446026344, 290.5826915525819)),
                new PdfRectangle(new PdfPoint(369.92427264664883, 246.08869240919273),new PdfPoint(811.9614880217224, 257.0837364279697),new PdfPoint(375.3394031464051, 28.382496516114173),new PdfPoint(817.3766185214787, 39.37754053489114)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(787.2830652212485, 909.5937859675068),new PdfPoint(1051.649076209865, 697.0164728885809),new PdfPoint(460.468733614018, 503.16000463150783),new PdfPoint(724.8347446026344, 290.5826915525819)),
                new PdfRectangle(new PdfPoint(763.7211043674602, 907.9508951176988),new PdfPoint(627.6855729103308, 775.4472008683426),new PdfPoint(989.9021602319854, 675.7410664455838),new PdfPoint(853.866628774856, 543.2373721962276)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(787.2830652212485, 909.5937859675068),new PdfPoint(1051.649076209865, 697.0164728885809),new PdfPoint(460.468733614018, 503.16000463150783),new PdfPoint(724.8347446026344, 290.5826915525819)),
                new PdfRectangle(new PdfPoint(158.31484478516745, 909.576007295144),new PdfPoint(123.03438413402273, 419.7929422949627),new PdfPoint(429.4717411745032, 890.0438084565685),new PdfPoint(394.1912805233585, 400.2607434563872)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(787.2830652212485, 909.5937859675068),new PdfPoint(1051.649076209865, 697.0164728885809),new PdfPoint(460.468733614018, 503.16000463150783),new PdfPoint(724.8347446026344, 290.5826915525819)),
                new PdfRectangle(new PdfPoint(436.22126857125795, 916.9378461199556),new PdfPoint(174.2045236299664, 523.2004610446594),new PdfPoint(336.5550218902624, 983.2618136316419),new PdfPoint(74.53827694897086, 589.5244285563457)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(787.2830652212485, 909.5937859675068),new PdfPoint(1051.649076209865, 697.0164728885809),new PdfPoint(460.468733614018, 503.16000463150783),new PdfPoint(724.8347446026344, 290.5826915525819)),
                new PdfRectangle(new PdfPoint(529.4498442035258, 836.6241444415786),new PdfPoint(116.51478612130325, 550.8610450621246),new PdfPoint(463.8301638486734, 931.4462771014963),new PdfPoint(50.8951057664508, 645.6831777220424)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(787.2830652212485, 909.5937859675068),new PdfPoint(1051.649076209865, 697.0164728885809),new PdfPoint(460.468733614018, 503.16000463150783),new PdfPoint(724.8347446026344, 290.5826915525819)),
                new PdfRectangle(new PdfPoint(404.6106878480057, 268.94016429833255),new PdfPoint(871.4883160602285, 432.6622946256348),new PdfPoint(344.49181956525456, 440.37792032155244),new PdfPoint(811.3694477774774, 604.1000506488547)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(787.2830652212485, 909.5937859675068),new PdfPoint(1051.649076209865, 697.0164728885809),new PdfPoint(460.468733614018, 503.16000463150783),new PdfPoint(724.8347446026344, 290.5826915525819)),
                new PdfRectangle(new PdfPoint(44.49589410457207, 163.40101431813866),new PdfPoint(493.20444193887965, 889.8884547476357),new PdfPoint(589.833791913485, -173.42211927958294),new PdfPoint(1038.5423397477925, 553.065321149914)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(787.2830652212485, 909.5937859675068),new PdfPoint(1051.649076209865, 697.0164728885809),new PdfPoint(460.468733614018, 503.16000463150783),new PdfPoint(724.8347446026344, 290.5826915525819)),
                new PdfRectangle(new PdfPoint(714.4435095316294, 467.95015931630155),new PdfPoint(413.15032203154664, 872.1677560263385),new PdfPoint(755.7756974798385, 498.75808740905507),new PdfPoint(454.4825099797558, 902.975684119092)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(787.2830652212485, 909.5937859675068),new PdfPoint(1051.649076209865, 697.0164728885809),new PdfPoint(460.468733614018, 503.16000463150783),new PdfPoint(724.8347446026344, 290.5826915525819)),
                new PdfRectangle(new PdfPoint(652.2373583756914, 379.0301504466386),new PdfPoint(776.4569236365121, 420.89771256432414),new PdfPoint(632.338937164153, 438.06805729505027),new PdfPoint(756.5585024249737, 479.93561941273583)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(572.9627332489183, 974.8758596169033),new PdfPoint(10.483364571368327, 678.6610218498724),new PdfPoint(662.9328178802512, 804.0325730861354),new PdfPoint(100.45344920270111, 507.8177353191045)),
                new PdfRectangle(new PdfPoint(455.6489464687223, 752.925119019483),new PdfPoint(405.734557426197, 827.1696960220337),new PdfPoint(255.4084478259054, 618.304079276965),new PdfPoint(205.49405878338013, 692.5486562795157)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(572.9627332489183, 974.8758596169033),new PdfPoint(10.483364571368327, 678.6610218498724),new PdfPoint(662.9328178802512, 804.0325730861354),new PdfPoint(100.45344920270111, 507.8177353191045)),
                new PdfRectangle(new PdfPoint(-20.339774483571432, 525.8477549082425),new PdfPoint(300.50252266486063, 703.9275820749795),new PdfPoint(214.49725446834498, 102.74732279753971),new PdfPoint(535.339551616777, 280.8271499642768)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(572.9627332489183, 974.8758596169033),new PdfPoint(10.483364571368327, 678.6610218498724),new PdfPoint(662.9328178802512, 804.0325730861354),new PdfPoint(100.45344920270111, 507.8177353191045)),
                new PdfRectangle(new PdfPoint(298.0376972233562, 597.4469289423054),new PdfPoint(189.56785288341564, 175.395857096829),new PdfPoint(834.9463589773915, 459.45794407710764),new PdfPoint(726.4765146374509, 37.406872231631155)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(905.6087845218851, 646.7010396530226),new PdfPoint(490.3202373014576, 1067.725146921232),new PdfPoint(437.0773221891502, 184.55232427014914),new PdfPoint(21.78877496872269, 605.5764315383584)),
                new PdfRectangle(new PdfPoint(787.6271852024784, 649.6839251239702),new PdfPoint(628.4301638321272, 592.041787026625),new PdfPoint(772.6253471362169, 691.116252481362),new PdfPoint(613.4283257658656, 633.4741143840166)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(905.6087845218851, 646.7010396530226),new PdfPoint(490.3202373014576, 1067.725146921232),new PdfPoint(437.0773221891502, 184.55232427014914),new PdfPoint(21.78877496872269, 605.5764315383584)),
                new PdfRectangle(new PdfPoint(370.1293777048335, 329.2213549390493),new PdfPoint(51.687578384536096, -35.98826327472881),new PdfPoint(493.2279711058647, 221.88645568818617),new PdfPoint(174.78617178556732, -143.32316252559195)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(905.6087845218851, 646.7010396530226),new PdfPoint(490.3202373014576, 1067.725146921232),new PdfPoint(437.0773221891502, 184.55232427014914),new PdfPoint(21.78877496872269, 605.5764315383584)),
                new PdfRectangle(new PdfPoint(422.3374713118757, 960.8690589319286),new PdfPoint(443.74510911015454, 936.9784565223983),new PdfPoint(276.07922870436926, 829.8115230405238),new PdfPoint(297.4868665026481, 805.9209206309936)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(905.6087845218851, 646.7010396530226),new PdfPoint(490.3202373014576, 1067.725146921232),new PdfPoint(437.0773221891502, 184.55232427014914),new PdfPoint(21.78877496872269, 605.5764315383584)),
                new PdfRectangle(new PdfPoint(223.18539609387392, 774.655693299439),new PdfPoint(640.6224338840819, 723.7317767378834),new PdfPoint(143.89399187125105, 124.68273097027077),new PdfPoint(561.3310296614591, 73.75881440871512)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(359.0233503669928, 563.5953047499611),new PdfPoint(506.709919167241, 194.29876316327773),new PdfPoint(1091.4420582449245, 856.4992280492622),new PdfPoint(1239.1286270451728, 487.20268646257887)),
                new PdfRectangle(new PdfPoint(907.2902159074554, 556.4481946858417),new PdfPoint(360.12654776115266, 385.2540698805592),new PdfPoint(892.9060254243084, 602.4223566906765),new PdfPoint(345.74235727800556, 431.22823188539394)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(359.0233503669928, 563.5953047499611),new PdfPoint(506.709919167241, 194.29876316327773),new PdfPoint(1091.4420582449245, 856.4992280492622),new PdfPoint(1239.1286270451728, 487.20268646257887)),
                new PdfRectangle(new PdfPoint(542.0693812152003, 416.33132381695253),new PdfPoint(543.8355380124348, 368.108122339854),new PdfPoint(438.0823056795748, 412.52283589710754),new PdfPoint(439.84846247680923, 364.299634420009)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(359.0233503669928, 563.5953047499611),new PdfPoint(506.709919167241, 194.29876316327773),new PdfPoint(1091.4420582449245, 856.4992280492622),new PdfPoint(1239.1286270451728, 487.20268646257887)),
                new PdfRectangle(new PdfPoint(471.5224046742694, 577.0594378769499),new PdfPoint(969.9867566457817, 449.4826546371001),new PdfPoint(409.9649833963749, 336.5440452867836),new PdfPoint(908.4293353678872, 208.96726204693374)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(612.1601956159619, 474.31575903059564),new PdfPoint(559.9230432915692, 854.8055590386881),new PdfPoint(-97.46314694192017, 376.89211551281306),new PdfPoint(-149.70029926631287, 757.3819155209055)),
                new PdfRectangle(new PdfPoint(-31.318099072923133, 457.84330750036077),new PdfPoint(-58.418152585646226, 535.8315264412157),new PdfPoint(414.2576578033333, 612.6760119691762),new PdfPoint(387.1576042906101, 690.6642309100312)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(612.1601956159619, 474.31575903059564),new PdfPoint(559.9230432915692, 854.8055590386881),new PdfPoint(-97.46314694192017, 376.89211551281306),new PdfPoint(-149.70029926631287, 757.3819155209055)),
                new PdfRectangle(new PdfPoint(391.5634265541123, 431.4175728138414),new PdfPoint(609.8263066802767, 127.52752778793774),new PdfPoint(1015.6199493379099, 879.6335517634005),new PdfPoint(1233.8828294640743, 575.743506737497)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(612.1601956159619, 474.31575903059564),new PdfPoint(559.9230432915692, 854.8055590386881),new PdfPoint(-97.46314694192017, 376.89211551281306),new PdfPoint(-149.70029926631287, 757.3819155209055)),
                new PdfRectangle(new PdfPoint(471.6322386742746, 527.5252278061023),new PdfPoint(303.6810993275918, 355.5392530459328),new PdfPoint(105.33806332362872, 885.2260471431568),new PdfPoint(-62.61307602305419, 713.2400723829875)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(612.1601956159619, 474.31575903059564),new PdfPoint(559.9230432915692, 854.8055590386881),new PdfPoint(-97.46314694192017, 376.89211551281306),new PdfPoint(-149.70029926631287, 757.3819155209055)),
                new PdfRectangle(new PdfPoint(132.91373573893168, 528.7303421120419),new PdfPoint(203.5604627234547, 669.1654571036024),new PdfPoint(495.0700439317585, 346.54543891202),new PdfPoint(565.7167709162816, 486.98055390358047)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(509.8351060955906, -108.08382080227386),new PdfPoint(999.3063197163258, 302.7004484138747),new PdfPoint(246.14571604767286, 206.11605353067398),new PdfPoint(735.616929668408, 616.9003227468226)),
                new PdfRectangle(new PdfPoint(481.73242416296057, 118.04352619589656),new PdfPoint(652.5145037399169, 97.79823366099453),new PdfPoint(495.42116675916543, 233.51688492080933),new PdfPoint(666.2032463361218, 213.27159238590727)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(509.8351060955906, -108.08382080227386),new PdfPoint(999.3063197163258, 302.7004484138747),new PdfPoint(246.14571604767286, 206.11605353067398),new PdfPoint(735.616929668408, 616.9003227468226)),
                new PdfRectangle(new PdfPoint(95.63941469885889, 449.54951782694116),new PdfPoint(819.6631431016111, 302.1958749319754),new PdfPoint(149.8348520629902, 715.8393892674846),new PdfPoint(873.8585804657424, 568.4857463725189)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(509.8351060955906, -108.08382080227386),new PdfPoint(999.3063197163258, 302.7004484138747),new PdfPoint(246.14571604767286, 206.11605353067398),new PdfPoint(735.616929668408, 616.9003227468226)),
                new PdfRectangle(new PdfPoint(239.54605173705193, 527.8808821796056),new PdfPoint(340.6974218271811, 671.0968331592596),new PdfPoint(135.16680558828546, 601.6024452087297),new PdfPoint(236.3181756784146, 744.818396188384)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(805.8752490981071, 431.29076732181863),new PdfPoint(752.978742023579, 225.79468929913097),new PdfPoint(-21.711011367131277, 644.3187866529158),new PdfPoint(-74.60751844165941, 438.82270863022825)),
                new PdfRectangle(new PdfPoint(128.03331043929256, 980.6552395398046),new PdfPoint(-84.95781685349823, 452.3072021027416),new PdfPoint(909.5045471419946, 665.6234274418424),new PdfPoint(696.5134198492038, 137.27539000477952)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(805.8752490981071, 431.29076732181863),new PdfPoint(752.978742023579, 225.79468929913097),new PdfPoint(-21.711011367131277, 644.3187866529158),new PdfPoint(-74.60751844165941, 438.82270863022825)),
                new PdfRectangle(new PdfPoint(596.1608150901543, 314.79863769294656),new PdfPoint(75.63388525011317, 457.0086806007713),new PdfPoint(624.9874099963235, 420.3117128417707),new PdfPoint(104.4604801562823, 562.5217557495955)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(805.8752490981071, 431.29076732181863),new PdfPoint(752.978742023579, 225.79468929913097),new PdfPoint(-21.711011367131277, 644.3187866529158),new PdfPoint(-74.60751844165941, 438.82270863022825)),
                new PdfRectangle(new PdfPoint(428.91248047884187, 762.2972337204317),new PdfPoint(410.59663237805603, 665.5537015011863),new PdfPoint(539.3375096946768, 741.3911533319412),new PdfPoint(521.0216615938909, 644.6476211126959)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(-36.505394349814196, 287.2936359576752),new PdfPoint(218.90973523968216, 134.87730731892998),new PdfPoint(348.6147899743132, 932.6675448111166),new PdfPoint(604.0299195638096, 780.2512161723714)),
                new PdfRectangle(new PdfPoint(959.1086329905719, 415.9131571214781),new PdfPoint(162.84465179122276, 56.91321425516503),new PdfPoint(863.2847587012342, 628.4510729394412),new PdfPoint(67.02077750188505, 269.4511300731282)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(-36.505394349814196, 287.2936359576752),new PdfPoint(218.90973523968216, 134.87730731892998),new PdfPoint(348.6147899743132, 932.6675448111166),new PdfPoint(604.0299195638096, 780.2512161723714)),
                new PdfRectangle(new PdfPoint(726.8718058244704, 553.6306484645293),new PdfPoint(439.23819566717094, 1247.4222273863022),new PdfPoint(672.3433940342361, 531.0241418191994),new PdfPoint(384.70978387693657, 1224.8157207409724)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(-36.505394349814196, 287.2936359576752),new PdfPoint(218.90973523968216, 134.87730731892998),new PdfPoint(348.6147899743132, 932.6675448111166),new PdfPoint(604.0299195638096, 780.2512161723714)),
                new PdfRectangle(new PdfPoint(320.7263053892063, 806.0548844918478),new PdfPoint(174.76170727705238, 599.112878302303),new PdfPoint(507.2714260811541, 674.4770377658259),new PdfPoint(361.3068279690002, 467.5350315762811)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(-36.505394349814196, 287.2936359576752),new PdfPoint(218.90973523968216, 134.87730731892998),new PdfPoint(348.6147899743132, 932.6675448111166),new PdfPoint(604.0299195638096, 780.2512161723714)),
                new PdfRectangle(new PdfPoint(70.31817979709007, 182.21524815561474),new PdfPoint(331.1222537428601, 733.4988521679364),new PdfPoint(625.4316522777149, -80.40067960176899),new PdfPoint(886.2357262234849, 470.88292441055273)),
                false
            }
        };

        public static IEnumerable<object[]> IntersectsWithData => new[]
        {
            new object[]
            {
                new PdfRectangle(10, 10, 20, 20),
                new PdfRectangle(149.95376d, 687.13456d, 451.73539d, 1478.4997d),
                false
            },
            new object[]
            {
                new PdfRectangle(10, 10, 20, 20),
                new PdfRectangle(10, 10, 20, 20),
                true
            },

            new object[]
            {
                new PdfRectangle(new PdfPoint(278.4751591606737, 751.7695200399908),new PdfPoint(513.3590761230906, 751.7695200399908),new PdfPoint(278.4751591606737, 983.6673235356695),new PdfPoint(513.3590761230906, 983.6673235356695)),
                new PdfRectangle(new PdfPoint(52.911541685483826, 443.1911433224352),new PdfPoint(110.4040196948311, 452.8829054665473),new PdfPoint(28.953842263239466, 585.310552229382),new PdfPoint(86.44632027258675, 595.0023143734941)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(278.4751591606737, 751.7695200399908),new PdfPoint(513.3590761230906, 751.7695200399908),new PdfPoint(278.4751591606737, 983.6673235356695),new PdfPoint(513.3590761230906, 983.6673235356695)),
                new PdfRectangle(new PdfPoint(617.8672639974703, 547.110219873183),new PdfPoint(598.0516573328221, 627.7083323901318),new PdfPoint(480.6508231107979, 513.3746032982729),new PdfPoint(460.83521644614973, 593.9727158152216)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(278.4751591606737, 751.7695200399908),new PdfPoint(513.3590761230906, 751.7695200399908),new PdfPoint(278.4751591606737, 983.6673235356695),new PdfPoint(513.3590761230906, 983.6673235356695)),
                new PdfRectangle(new PdfPoint(754.6501125175419, 466.72819229907105),new PdfPoint(411.2711685052823, 928.494736627362),new PdfPoint(767.5001442246239, 476.2837358357307),new PdfPoint(424.1212002123641, 938.0502801640216)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(278.4751591606737, 751.7695200399908),new PdfPoint(513.3590761230906, 751.7695200399908),new PdfPoint(278.4751591606737, 983.6673235356695),new PdfPoint(513.3590761230906, 983.6673235356695)),
                new PdfRectangle(new PdfPoint(629.7053976677668, 346.6896046744197),new PdfPoint(542.4584746046635, -144.25473247886862),new PdfPoint(534.9825215329824, 363.523039467389),new PdfPoint(447.73559846987916, -127.42129768589939)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(278.4751591606737, 751.7695200399908),new PdfPoint(513.3590761230906, 751.7695200399908),new PdfPoint(278.4751591606737, 983.6673235356695),new PdfPoint(513.3590761230906, 983.6673235356695)),
                new PdfRectangle(new PdfPoint(601.9332573735312, 130.3194754666647),new PdfPoint(465.9009867167505, 136.94696435293554),new PdfPoint(642.3627502856798, 960.1534920479302),new PdfPoint(506.3304796288992, 966.780980934201)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(278.4751591606737, 751.7695200399908),new PdfPoint(513.3590761230906, 751.7695200399908),new PdfPoint(278.4751591606737, 983.6673235356695),new PdfPoint(513.3590761230906, 983.6673235356695)),
                new PdfRectangle(new PdfPoint(439.19503746758755, 566.6518096554503),new PdfPoint(228.0909262963638, 646.6118322374917),new PdfPoint(459.00609322202183, 618.9553881357133),new PdfPoint(247.90198205079818, 698.9154107177548)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(278.4751591606737, 751.7695200399908),new PdfPoint(513.3590761230906, 751.7695200399908),new PdfPoint(278.4751591606737, 983.6673235356695),new PdfPoint(513.3590761230906, 983.6673235356695)),
                new PdfRectangle(new PdfPoint(857.6023625898431, 466.23582875474733),new PdfPoint(890.7808601707017, 192.16221433941587),new PdfPoint(621.3732965352235, 437.6386742517809),new PdfPoint(654.5517941160821, 163.56505983644945)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(278.4751591606737, 751.7695200399908),new PdfPoint(513.3590761230906, 751.7695200399908),new PdfPoint(278.4751591606737, 983.6673235356695),new PdfPoint(513.3590761230906, 983.6673235356695)),
                new PdfRectangle(new PdfPoint(244.78071032174194, 846.7316998366954),new PdfPoint(118.03961626734423, 733.0200294622443),new PdfPoint(644.9723225305553, 400.6849344016648),new PdfPoint(518.2312284761576, 286.97326402721376)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(278.4751591606737, 751.7695200399908),new PdfPoint(513.3590761230906, 751.7695200399908),new PdfPoint(278.4751591606737, 983.6673235356695),new PdfPoint(513.3590761230906, 983.6673235356695)),
                new PdfRectangle(new PdfPoint(227.22814709143435, 570.4273345679334),new PdfPoint(520.7740311717042, 755.9392026276408),new PdfPoint(335.03737090579153, 399.8347237145538),new PdfPoint(628.5832549860614, 585.3465917742614)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(278.4751591606737, 751.7695200399908),new PdfPoint(513.3590761230906, 751.7695200399908),new PdfPoint(278.4751591606737, 983.6673235356695),new PdfPoint(513.3590761230906, 983.6673235356695)),
                new PdfRectangle(new PdfPoint(66.81757815760339, 926.9123827764113),new PdfPoint(134.29377923561253, 925.3918352487667),new PdfPoint(56.14922040995988, 453.49064523475846),new PdfPoint(123.62542148796904, 451.97009770711395)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(272.45385505559125, 938.5803575680796),new PdfPoint(240.63320003951048, 931.4795462283105),new PdfPoint(366.60475992899114, 516.6647031344695),new PdfPoint(334.7841049129104, 509.56389179470034)),
                new PdfRectangle(new PdfPoint(170.4336069830797, 693.3339544808484),new PdfPoint(382.43662279587215, 70.24371723667798),new PdfPoint(789.9360063984293, 904.1162282654534),new PdfPoint(1001.9390222112218, 281.025991021283)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(272.45385505559125, 938.5803575680796),new PdfPoint(240.63320003951048, 931.4795462283105),new PdfPoint(366.60475992899114, 516.6647031344695),new PdfPoint(334.7841049129104, 509.56389179470034)),
                new PdfRectangle(new PdfPoint(323.02083632824673, 726.3037071929716),new PdfPoint(433.19605049173293, 552.1399016634668),new PdfPoint(-155.39537428635458, 423.6598352156921),new PdfPoint(-45.22016012286838, 249.49602968618734)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(272.45385505559125, 938.5803575680796),new PdfPoint(240.63320003951048, 931.4795462283105),new PdfPoint(366.60475992899114, 516.6647031344695),new PdfPoint(334.7841049129104, 509.56389179470034)),
                new PdfRectangle(new PdfPoint(374.0707898208983, 590.2169160341493),new PdfPoint(263.19502952126504, 654.5572855749946),new PdfPoint(559.3601954381106, 909.5203933048775),new PdfPoint(448.48443513847735, 973.8607628457228)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(272.45385505559125, 938.5803575680796),new PdfPoint(240.63320003951048, 931.4795462283105),new PdfPoint(366.60475992899114, 516.6647031344695),new PdfPoint(334.7841049129104, 509.56389179470034)),
                new PdfRectangle(new PdfPoint(105.22866107327295, 512.151639532266),new PdfPoint(350.3072700471073, 300.2396286810407),new PdfPoint(8.144944530904581, 399.8732362995305),new PdfPoint(253.22355350473887, 187.96122544830527)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(272.45385505559125, 938.5803575680796),new PdfPoint(240.63320003951048, 931.4795462283105),new PdfPoint(366.60475992899114, 516.6647031344695),new PdfPoint(334.7841049129104, 509.56389179470034)),
                new PdfRectangle(new PdfPoint(622.4948391095778, 728.4134977324734),new PdfPoint(626.601591132655, 587.6504589956758),new PdfPoint(178.74124315009738, 715.4670169353237),new PdfPoint(182.84799517317464, 574.7039781985261)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(272.45385505559125, 938.5803575680796),new PdfPoint(240.63320003951048, 931.4795462283105),new PdfPoint(366.60475992899114, 516.6647031344695),new PdfPoint(334.7841049129104, 509.56389179470034)),
                new PdfRectangle(new PdfPoint(299.39659089023917, 191.5669300326303),new PdfPoint(92.44657528076573, 726.7891762135442),new PdfPoint(764.7644545831745, 371.50692195674526),new PdfPoint(557.8144389737009, 906.7291681376591)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(272.45385505559125, 938.5803575680796),new PdfPoint(240.63320003951048, 931.4795462283105),new PdfPoint(366.60475992899114, 516.6647031344695),new PdfPoint(334.7841049129104, 509.56389179470034)),
                new PdfRectangle(new PdfPoint(731.9465546205961, 508.08191945136195),new PdfPoint(339.1635828835399, 531.0427544603979),new PdfPoint(736.8673373245153, 592.2600276498263),new PdfPoint(344.08436558745916, 615.2208626588622)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(272.45385505559125, 938.5803575680796),new PdfPoint(240.63320003951048, 931.4795462283105),new PdfPoint(366.60475992899114, 516.6647031344695),new PdfPoint(334.7841049129104, 509.56389179470034)),
                new PdfRectangle(new PdfPoint(674.6830843946341, 920.5217117131647),new PdfPoint(874.3845838752602, 449.18629575975376),new PdfPoint(200.64432141445513, 719.6748231177187),new PdfPoint(400.34582089508115, 248.3394071643079)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(272.45385505559125, 938.5803575680796),new PdfPoint(240.63320003951048, 931.4795462283105),new PdfPoint(366.60475992899114, 516.6647031344695),new PdfPoint(334.7841049129104, 509.56389179470034)),
                new PdfRectangle(new PdfPoint(144.42029095311636, 399.3131139729534),new PdfPoint(115.27171413791643, 468.41641089070646),new PdfPoint(325.97601465249005, 475.89543630106255),new PdfPoint(296.8274378372902, 544.9987332188157)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(272.45385505559125, 938.5803575680796),new PdfPoint(240.63320003951048, 931.4795462283105),new PdfPoint(366.60475992899114, 516.6647031344695),new PdfPoint(334.7841049129104, 509.56389179470034)),
                new PdfRectangle(new PdfPoint(253.85784562136809, 487.91553115900524),new PdfPoint(291.8137246739912, 606.1887144943576),new PdfPoint(287.8717530992766, 476.99988889817234),new PdfPoint(325.8276321518997, 595.2730722335247)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(-31.41839780491216, 938.8706264719088),new PdfPoint(-46.29299132338491, 930.0601194916267),new PdfPoint(386.32511239570675, 233.60304957531747),new PdfPoint(371.450518877234, 224.79254259503523)),
                new PdfRectangle(new PdfPoint(346.88450412384634, 11.915858985366611),new PdfPoint(976.452186775741, 311.09427417037256),new PdfPoint(223.62620527168224, 271.29099314397797),new PdfPoint(853.193887923577, 570.469408328984)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(-31.41839780491216, 938.8706264719088),new PdfPoint(-46.29299132338491, 930.0601194916267),new PdfPoint(386.32511239570675, 233.60304957531747),new PdfPoint(371.450518877234, 224.79254259503523)),
                new PdfRectangle(new PdfPoint(225.90294331782871, -103.6678676060817),new PdfPoint(375.22485086481714, 210.854611711162),new PdfPoint(208.1344285078289, -95.23213184733521),new PdfPoint(357.4563360548173, 219.2903474699085)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(-31.41839780491216, 938.8706264719088),new PdfPoint(-46.29299132338491, 930.0601194916267),new PdfPoint(386.32511239570675, 233.60304957531747),new PdfPoint(371.450518877234, 224.79254259503523)),
                new PdfRectangle(new PdfPoint(29.413369384701696, 717.1982066205832),new PdfPoint(781.5876939491975, 758.8802894535697),new PdfPoint(67.66123002739914, 26.996163209487236),new PdfPoint(819.835554591895, 68.67824604247392)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(-31.41839780491216, 938.8706264719088),new PdfPoint(-46.29299132338491, 930.0601194916267),new PdfPoint(386.32511239570675, 233.60304957531747),new PdfPoint(371.450518877234, 224.79254259503523)),
                new PdfRectangle(new PdfPoint(574.8333484520039, 854.2582481974034),new PdfPoint(520.5909836686976, 867.8173429376104),new PdfPoint(585.5007814042674, 896.9326892706946),new PdfPoint(531.2584166209612, 910.4917840109017)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(-31.41839780491216, 938.8706264719088),new PdfPoint(-46.29299132338491, 930.0601194916267),new PdfPoint(386.32511239570675, 233.60304957531747),new PdfPoint(371.450518877234, 224.79254259503523)),
                new PdfRectangle(new PdfPoint(764.9178210328682, 559.7258650483744),new PdfPoint(691.876454318417, 728.9210990712251),new PdfPoint(-24.44558909283242, 218.95868775203064),new PdfPoint(-97.48695580728369, 388.15392177488127)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(-31.41839780491216, 938.8706264719088),new PdfPoint(-46.29299132338491, 930.0601194916267),new PdfPoint(386.32511239570675, 233.60304957531747),new PdfPoint(371.450518877234, 224.79254259503523)),
                new PdfRectangle(new PdfPoint(130.3267681005483, 119.82727470096557),new PdfPoint(686.7238714286069, 358.43009126284505),new PdfPoint(227.70013620541795, -107.23735402187106),new PdfPoint(784.0972395334766, 131.36546254000842)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(-31.41839780491216, 938.8706264719088),new PdfPoint(-46.29299132338491, 930.0601194916267),new PdfPoint(386.32511239570675, 233.60304957531747),new PdfPoint(371.450518877234, 224.79254259503523)),
                new PdfRectangle(new PdfPoint(903.5832867098978, 694.7101517897204),new PdfPoint(609.4222692242329, 712.3269503272847),new PdfPoint(919.8965952552483, 967.1058104007618),new PdfPoint(625.7355777695833, 984.7226089383262)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(-31.41839780491216, 938.8706264719088),new PdfPoint(-46.29299132338491, 930.0601194916267),new PdfPoint(386.32511239570675, 233.60304957531747),new PdfPoint(371.450518877234, 224.79254259503523)),
                new PdfRectangle(new PdfPoint(205.55119249734025, 784.1227476204415),new PdfPoint(641.3651338717216, 367.7182837679101),new PdfPoint(382.65271709848935, 969.4793423738852),new PdfPoint(818.4666584728706, 553.0748785213539)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(-31.41839780491216, 938.8706264719088),new PdfPoint(-46.29299132338491, 930.0601194916267),new PdfPoint(386.32511239570675, 233.60304957531747),new PdfPoint(371.450518877234, 224.79254259503523)),
                new PdfRectangle(new PdfPoint(310.62356965982315, 145.02526405356275),new PdfPoint(216.47502249241566, 187.7177771123034),new PdfPoint(485.59874769604323, 530.8928933449671),new PdfPoint(391.4502005286357, 573.5854064037078)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(-31.41839780491216, 938.8706264719088),new PdfPoint(-46.29299132338491, 930.0601194916267),new PdfPoint(386.32511239570675, 233.60304957531747),new PdfPoint(371.450518877234, 224.79254259503523)),
                new PdfRectangle(new PdfPoint(345.9399996198303, 821.5799878860864),new PdfPoint(203.52572583973995, 710.9479362881532),new PdfPoint(346.4577541219132, 820.9134935894925),new PdfPoint(204.04348034182283, 710.2814419915593)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(167.44224904909316, 684.7421839979284),new PdfPoint(358.9809865333775, -71.052091306541),new PdfPoint(851.7477764623309, 858.1637367365322),new PdfPoint(1043.2865139466153, 102.36946143206285)),
                new PdfRectangle(new PdfPoint(44.9992706861475, 583.7214643157546),new PdfPoint(33.7687128767825, 591.6790811926548),new PdfPoint(186.18806697345792, 782.9807354352455),new PdfPoint(174.95750916409293, 790.9383523121458)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(167.44224904909316, 684.7421839979284),new PdfPoint(358.9809865333775, -71.052091306541),new PdfPoint(851.7477764623309, 858.1637367365322),new PdfPoint(1043.2865139466153, 102.36946143206285)),
                new PdfRectangle(new PdfPoint(766.2048928333022, 477.78695155528123),new PdfPoint(912.4544023112817, 368.6480895994374),new PdfPoint(497.1409624785399, 117.23281779015295),new PdfPoint(643.3904719565194, 8.093955834309128)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(167.44224904909316, 684.7421839979284),new PdfPoint(358.9809865333775, -71.052091306541),new PdfPoint(851.7477764623309, 858.1637367365322),new PdfPoint(1043.2865139466153, 102.36946143206285)),
                new PdfRectangle(new PdfPoint(316.66756392074683, 512.5888271731849),new PdfPoint(357.1398256760049, 457.3395885970586),new PdfPoint(314.3351755919397, 510.88026007993307),new PdfPoint(354.8074373471976, 455.6310215038067)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(167.44224904909316, 684.7421839979284),new PdfPoint(358.9809865333775, -71.052091306541),new PdfPoint(851.7477764623309, 858.1637367365322),new PdfPoint(1043.2865139466153, 102.36946143206285)),
                new PdfRectangle(new PdfPoint(879.6191543264763, 352.20541107375595),new PdfPoint(710.9416174251113, 89.03027908721435),new PdfPoint(367.37922106742843, 680.5167131046572),new PdfPoint(198.70168416606344, 417.34158111811564)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(167.44224904909316, 684.7421839979284),new PdfPoint(358.9809865333775, -71.052091306541),new PdfPoint(851.7477764623309, 858.1637367365322),new PdfPoint(1043.2865139466153, 102.36946143206285)),
                new PdfRectangle(new PdfPoint(561.5731149332372, 366.9237886926279),new PdfPoint(449.16691604472163, 234.54480770779415),new PdfPoint(216.41637197159184, 660.0047386170262),new PdfPoint(104.01017308307627, 527.6257576321924)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(167.44224904909316, 684.7421839979284),new PdfPoint(358.9809865333775, -71.052091306541),new PdfPoint(851.7477764623309, 858.1637367365322),new PdfPoint(1043.2865139466153, 102.36946143206285)),
                new PdfRectangle(new PdfPoint(215.81473893777815, 844.171440329625),new PdfPoint(481.42526744969194, 480.7299726538062),new PdfPoint(483.1935359953157, 1039.577385758723),new PdfPoint(748.8040645072294, 676.1359180829041)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(167.44224904909316, 684.7421839979284),new PdfPoint(358.9809865333775, -71.052091306541),new PdfPoint(851.7477764623309, 858.1637367365322),new PdfPoint(1043.2865139466153, 102.36946143206285)),
                new PdfRectangle(new PdfPoint(402.516298498063, 330.7719843232733),new PdfPoint(373.6429671625649, 316.90554990663554),new PdfPoint(235.42046602065804, 678.7066581506083),new PdfPoint(206.54713468515993, 664.8402237339706)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(167.44224904909316, 684.7421839979284),new PdfPoint(358.9809865333775, -71.052091306541),new PdfPoint(851.7477764623309, 858.1637367365322),new PdfPoint(1043.2865139466153, 102.36946143206285)),
                new PdfRectangle(new PdfPoint(881.2507998122196, 865.6686499531249),new PdfPoint(943.1144129541476, 859.6696035024056),new PdfPoint(867.2847755579728, 721.6476411656519),new PdfPoint(929.1483886999006, 715.6485947149325)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(167.44224904909316, 684.7421839979284),new PdfPoint(358.9809865333775, -71.052091306541),new PdfPoint(851.7477764623309, 858.1637367365322),new PdfPoint(1043.2865139466153, 102.36946143206285)),
                new PdfRectangle(new PdfPoint(802.0953856661424, 861.9791656375016),new PdfPoint(942.1055488416723, 485.3622461228457),new PdfPoint(330.8918736506737, 686.8057276220563),new PdfPoint(470.9020368262036, 310.1888081074004)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(167.44224904909316, 684.7421839979284),new PdfPoint(358.9809865333775, -71.052091306541),new PdfPoint(851.7477764623309, 858.1637367365322),new PdfPoint(1043.2865139466153, 102.36946143206285)),
                new PdfRectangle(new PdfPoint(818.2683142448838, 185.91741552285282),new PdfPoint(254.8850283818316, 17.378733267441703),new PdfPoint(589.8716696124986, 949.391120260198),new PdfPoint(26.488383749446484, 780.852438004787)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(301.50695200507005, 912.2906661340567),new PdfPoint(179.4052004809356, 629.2826917660379),new PdfPoint(435.0364465567038, 854.6803277635724),new PdfPoint(312.9346950325694, 571.672353395554)),
                new PdfRectangle(new PdfPoint(315.75739875434033, -58.28120989076348),new PdfPoint(141.39274292082297, 174.04550128814105),new PdfPoint(434.4733480161647, 30.816877582472728),new PdfPoint(260.1086921826474, 263.14358876137726)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(301.50695200507005, 912.2906661340567),new PdfPoint(179.4052004809356, 629.2826917660379),new PdfPoint(435.0364465567038, 854.6803277635724),new PdfPoint(312.9346950325694, 571.672353395554)),
                new PdfRectangle(new PdfPoint(332.77640607953697, 955.8663199103812),new PdfPoint(717.3135301062317, 907.0549048185981),new PdfPoint(228.11588756928654, 131.3490484607098),new PdfPoint(612.6530115959814, 82.53763336892678)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(301.50695200507005, 912.2906661340567),new PdfPoint(179.4052004809356, 629.2826917660379),new PdfPoint(435.0364465567038, 854.6803277635724),new PdfPoint(312.9346950325694, 571.672353395554)),
                new PdfRectangle(new PdfPoint(526.2985466195976, 757.014936648728),new PdfPoint(262.1969020131111, 469.3781980866071),new PdfPoint(242.51170071613984, 1017.5816955942446),new PdfPoint(-21.589943890346603, 729.9449570321235)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(301.50695200507005, 912.2906661340567),new PdfPoint(179.4052004809356, 629.2826917660379),new PdfPoint(435.0364465567038, 854.6803277635724),new PdfPoint(312.9346950325694, 571.672353395554)),
                new PdfRectangle(new PdfPoint(-5.2347435395797675, 827.2690588773614),new PdfPoint(-5.584033606049047, 812.3342274758772),new PdfPoint(56.66668219165683, 825.821332249121),new PdfPoint(56.31739212518755, 810.8865008476369)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(301.50695200507005, 912.2906661340567),new PdfPoint(179.4052004809356, 629.2826917660379),new PdfPoint(435.0364465567038, 854.6803277635724),new PdfPoint(312.9346950325694, 571.672353395554)),
                new PdfRectangle(new PdfPoint(981.6969798958472, 810.8088418381831),new PdfPoint(99.10559446575289, 1014.2895017683117),new PdfPoint(929.2896555345579, 583.4936118276383),new PdfPoint(46.6982701044636, 786.9742717577669)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(301.50695200507005, 912.2906661340567),new PdfPoint(179.4052004809356, 629.2826917660379),new PdfPoint(435.0364465567038, 854.6803277635724),new PdfPoint(312.9346950325694, 571.672353395554)),
                new PdfRectangle(new PdfPoint(352.9252782519285, 245.63964016746536),new PdfPoint(513.9177578715913, 504.6493389303606),new PdfPoint(-61.654781155598215, 503.32988143516553),new PdfPoint(99.33769846406466, 762.3395801980608)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(301.50695200507005, 912.2906661340567),new PdfPoint(179.4052004809356, 629.2826917660379),new PdfPoint(435.0364465567038, 854.6803277635724),new PdfPoint(312.9346950325694, 571.672353395554)),
                new PdfRectangle(new PdfPoint(-142.26397296088564, 757.7679544501307),new PdfPoint(302.65790317885075, 976.5522947790631),new PdfPoint(205.67879305906834, 50.18829854742148),new PdfPoint(650.6006691988047, 268.9726388763539)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(301.50695200507005, 912.2906661340567),new PdfPoint(179.4052004809356, 629.2826917660379),new PdfPoint(435.0364465567038, 854.6803277635724),new PdfPoint(312.9346950325694, 571.672353395554)),
                new PdfRectangle(new PdfPoint(191.16522368234752, 427.6618179254885),new PdfPoint(24.427540985047415, 301.28727786503066),new PdfPoint(231.5780618578995, 374.34140239409356),new PdfPoint(64.84037916059941, 247.9668623336358)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(301.50695200507005, 912.2906661340567),new PdfPoint(179.4052004809356, 629.2826917660379),new PdfPoint(435.0364465567038, 854.6803277635724),new PdfPoint(312.9346950325694, 571.672353395554)),
                new PdfRectangle(new PdfPoint(595.7552149830123, 871.4760089737084),new PdfPoint(333.26214334865506, 580.5032428213245),new PdfPoint(811.2866483317056, 677.0402591213482),new PdfPoint(548.7935766973484, 386.0674929689643)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(301.50695200507005, 912.2906661340567),new PdfPoint(179.4052004809356, 629.2826917660379),new PdfPoint(435.0364465567038, 854.6803277635724),new PdfPoint(312.9346950325694, 571.672353395554)),
                new PdfRectangle(new PdfPoint(409.71518033752943, 307.53188427844947),new PdfPoint(402.14387918579456, 325.9062480999194),new PdfPoint(441.67970289425045, 320.7031172419315),new PdfPoint(434.1084017425155, 339.0774810634015)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(183.32425464039136, 611.8842056138516),new PdfPoint(33.554303311546505, 680.9399395734799),new PdfPoint(268.71383684240607, 797.0794506294613),new PdfPoint(118.94388551356121, 866.1351845890897)),
                new PdfRectangle(new PdfPoint(319.3085393227572, 630.6664832123291),new PdfPoint(978.1787549578631, 638.8782519236798),new PdfPoint(319.36193427342585, 626.3823461527654),new PdfPoint(978.2321499085318, 634.5941148641161)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(183.32425464039136, 611.8842056138516),new PdfPoint(33.554303311546505, 680.9399395734799),new PdfPoint(268.71383684240607, 797.0794506294613),new PdfPoint(118.94388551356121, 866.1351845890897)),
                new PdfRectangle(new PdfPoint(437.9511840965953, 348.97258698780615),new PdfPoint(532.6767016870654, 620.6109158322917),new PdfPoint(1101.447565912705, 117.59861832038291),new PdfPoint(1196.1730835031751, 389.2369471648683)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(183.32425464039136, 611.8842056138516),new PdfPoint(33.554303311546505, 680.9399395734799),new PdfPoint(268.71383684240607, 797.0794506294613),new PdfPoint(118.94388551356121, 866.1351845890897)),
                new PdfRectangle(new PdfPoint(-51.380746252036204, 465.1452111716656),new PdfPoint(186.75559035486867, 255.88485399342582),new PdfPoint(417.92901658203346, 999.215348306948),new PdfPoint(656.0653531889384, 789.9549911287083)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(183.32425464039136, 611.8842056138516),new PdfPoint(33.554303311546505, 680.9399395734799),new PdfPoint(268.71383684240607, 797.0794506294613),new PdfPoint(118.94388551356121, 866.1351845890897)),
                new PdfRectangle(new PdfPoint(858.4344291160303, 698.6174393940338),new PdfPoint(295.83926182126044, 152.24168236751802),new PdfPoint(596.4299927243316, 968.3995954068711),new PdfPoint(33.83482542956176, 422.0238383803555)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(183.32425464039136, 611.8842056138516),new PdfPoint(33.554303311546505, 680.9399395734799),new PdfPoint(268.71383684240607, 797.0794506294613),new PdfPoint(118.94388551356121, 866.1351845890897)),
                new PdfRectangle(new PdfPoint(598.3175637679432, 238.50725255112434),new PdfPoint(538.0009962582885, 288.5140722464922),new PdfPoint(1046.0684716743344, 778.569548629246),new PdfPoint(985.7519041646797, 828.5763683246139)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(183.32425464039136, 611.8842056138516),new PdfPoint(33.554303311546505, 680.9399395734799),new PdfPoint(268.71383684240607, 797.0794506294613),new PdfPoint(118.94388551356121, 866.1351845890897)),
                new PdfRectangle(new PdfPoint(431.07625962887096, 429.72308770628325),new PdfPoint(713.6617265526179, 754.4994301139907),new PdfPoint(141.39890094016323, 681.7691913585163),new PdfPoint(423.9843678639102, 1006.5455337662238)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(183.32425464039136, 611.8842056138516),new PdfPoint(33.554303311546505, 680.9399395734799),new PdfPoint(268.71383684240607, 797.0794506294613),new PdfPoint(118.94388551356121, 866.1351845890897)),
                new PdfRectangle(new PdfPoint(575.7716991193154, 1133.8309879871103),new PdfPoint(129.60790585006595, 580.1875762214386),new PdfPoint(797.87641057677, 954.8437943152891),new PdfPoint(351.71261730752065, 401.2003825496174)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(183.32425464039136, 611.8842056138516),new PdfPoint(33.554303311546505, 680.9399395734799),new PdfPoint(268.71383684240607, 797.0794506294613),new PdfPoint(118.94388551356121, 866.1351845890897)),
                new PdfRectangle(new PdfPoint(14.894825697908743, 674.263329542578),new PdfPoint(130.52033129589375, 747.4621068684207),new PdfPoint(400.5503031440386, 65.07816012584593),new PdfPoint(516.1758087420236, 138.27693745168853)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(183.32425464039136, 611.8842056138516),new PdfPoint(33.554303311546505, 680.9399395734799),new PdfPoint(268.71383684240607, 797.0794506294613),new PdfPoint(118.94388551356121, 866.1351845890897)),
                new PdfRectangle(new PdfPoint(570.1918386483248, 445.2353170405328),new PdfPoint(516.4558217581173, 463.64053464839964),new PdfPoint(441.25562368226133, 68.79213811205335),new PdfPoint(387.51960679205365, 87.19735571992027)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(183.32425464039136, 611.8842056138516),new PdfPoint(33.554303311546505, 680.9399395734799),new PdfPoint(268.71383684240607, 797.0794506294613),new PdfPoint(118.94388551356121, 866.1351845890897)),
                new PdfRectangle(new PdfPoint(1356.6037021284822, 351.97115066736933),new PdfPoint(1348.1606993020184, 328.9801742520631),new PdfPoint(611.8952238245657, 625.4513053869159),new PdfPoint(603.4522209981021, 602.4603289716097)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(723.3650246379636, 287.5903666026356),new PdfPoint(512.9602325004732, 42.98879718045066),new PdfPoint(607.4509856162864, 387.29892261797954),new PdfPoint(397.0461934787959, 142.69735319579468)),
                new PdfRectangle(new PdfPoint(254.82620812917128, 336.6777955767443),new PdfPoint(229.23357031932244, 895.4484087575149),new PdfPoint(503.46309930363657, 348.06578512360375),new PdfPoint(477.87046149378773, 906.8363983043744)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(723.3650246379636, 287.5903666026356),new PdfPoint(512.9602325004732, 42.98879718045066),new PdfPoint(607.4509856162864, 387.29892261797954),new PdfPoint(397.0461934787959, 142.69735319579468)),
                new PdfRectangle(new PdfPoint(755.1456384652381, 175.89087719009524),new PdfPoint(504.2670377794924, 365.47668720043566),new PdfPoint(1052.8506837904645, 569.843489910455),new PdfPoint(801.9720831047189, 759.4292999207953)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(723.3650246379636, 287.5903666026356),new PdfPoint(512.9602325004732, 42.98879718045066),new PdfPoint(607.4509856162864, 387.29892261797954),new PdfPoint(397.0461934787959, 142.69735319579468)),
                new PdfRectangle(new PdfPoint(913.7371438381878, 317.54116412351175),new PdfPoint(983.0964934947326, 243.20757146867084),new PdfPoint(731.3102351027164, 147.32186894563864),new PdfPoint(800.6695847592612, 72.98827629079773)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(723.3650246379636, 287.5903666026356),new PdfPoint(512.9602325004732, 42.98879718045066),new PdfPoint(607.4509856162864, 387.29892261797954),new PdfPoint(397.0461934787959, 142.69735319579468)),
                new PdfRectangle(new PdfPoint(692.0595194612272, 400.06864180336345),new PdfPoint(649.9232822696889, 457.51812231746885),new PdfPoint(903.6698917157496, 555.2739588526103),new PdfPoint(861.5336545242114, 612.7234393667156)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(723.3650246379636, 287.5903666026356),new PdfPoint(512.9602325004732, 42.98879718045066),new PdfPoint(607.4509856162864, 387.29892261797954),new PdfPoint(397.0461934787959, 142.69735319579468)),
                new PdfRectangle(new PdfPoint(849.6200490264532, 803.9172931371792),new PdfPoint(1044.5795783954034, 721.6629120174118),new PdfPoint(634.9450949103652, 295.0942073390737),new PdfPoint(829.9046242793154, 212.8398262193063)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(723.3650246379636, 287.5903666026356),new PdfPoint(512.9602325004732, 42.98879718045066),new PdfPoint(607.4509856162864, 387.29892261797954),new PdfPoint(397.0461934787959, 142.69735319579468)),
                new PdfRectangle(new PdfPoint(307.857932220019, 420.7935750685365),new PdfPoint(426.9092919359583, 329.9888011861878),new PdfPoint(353.5892053497065, 480.75044754183074),new PdfPoint(472.6405650656459, 389.94567365948205)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(723.3650246379636, 287.5903666026356),new PdfPoint(512.9602325004732, 42.98879718045066),new PdfPoint(607.4509856162864, 387.29892261797954),new PdfPoint(397.0461934787959, 142.69735319579468)),
                new PdfRectangle(new PdfPoint(87.8657378732827, -100.54501885178692),new PdfPoint(712.6761574242222, 318.0354579384104),new PdfPoint(131.5832110424592, -165.80160454350593),new PdfPoint(756.3936305933987, 252.77887224669132)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(723.3650246379636, 287.5903666026356),new PdfPoint(512.9602325004732, 42.98879718045066),new PdfPoint(607.4509856162864, 387.29892261797954),new PdfPoint(397.0461934787959, 142.69735319579468)),
                new PdfRectangle(new PdfPoint(175.8422517586344, 491.92811839401867),new PdfPoint(-37.241708820518426, 369.1897944113293),new PdfPoint(362.1909646425062, 168.41122080526648),new PdfPoint(149.10700406335334, 45.67289682257709)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(723.3650246379636, 287.5903666026356),new PdfPoint(512.9602325004732, 42.98879718045066),new PdfPoint(607.4509856162864, 387.29892261797954),new PdfPoint(397.0461934787959, 142.69735319579468)),
                new PdfRectangle(new PdfPoint(955.9951742149402, 702.2700578017287),new PdfPoint(765.9308244103361, 409.3124487205771),new PdfPoint(719.4033266731361, 855.7655611721383),new PdfPoint(529.338976868532, 562.8079520909866)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(723.3650246379636, 287.5903666026356),new PdfPoint(512.9602325004732, 42.98879718045066),new PdfPoint(607.4509856162864, 387.29892261797954),new PdfPoint(397.0461934787959, 142.69735319579468)),
                new PdfRectangle(new PdfPoint(171.51040255977102, 449.99624471718926),new PdfPoint(311.0029557088304, 371.56922362523255),new PdfPoint(461.8887218538156, 966.4714668216438),new PdfPoint(601.381275002875, 888.0444457296873)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(772.5076834444708, 308.91350729885073),new PdfPoint(503.4101782807511, 332.90619550356894),new PdfPoint(811.0980660123346, 741.7360232136574),new PdfPoint(542.0005608486149, 765.7287114183756)),
                new PdfRectangle(new PdfPoint(244.363058122771, 210.34661882194305),new PdfPoint(564.9846770203867, 828.6881990462387),new PdfPoint(245.33686740857019, 209.84168058117783),new PdfPoint(565.9584863061859, 828.1832608054733)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(772.5076834444708, 308.91350729885073),new PdfPoint(503.4101782807511, 332.90619550356894),new PdfPoint(811.0980660123346, 741.7360232136574),new PdfPoint(542.0005608486149, 765.7287114183756)),
                new PdfRectangle(new PdfPoint(580.55104195998, 530.2121667198934),new PdfPoint(535.9082496177875, 767.5691259334048),new PdfPoint(594.7849163810208, 532.8893155261004),new PdfPoint(550.1421240388285, 770.2462747396119)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(772.5076834444708, 308.91350729885073),new PdfPoint(503.4101782807511, 332.90619550356894),new PdfPoint(811.0980660123346, 741.7360232136574),new PdfPoint(542.0005608486149, 765.7287114183756)),
                new PdfRectangle(new PdfPoint(210.5850529645066, 461.3379408760464),new PdfPoint(203.9572962341971, 441.1862276518762),new PdfPoint(300.0132343465445, 431.92564106606113),new PdfPoint(293.385477616235, 411.77392784189095)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(772.5076834444708, 308.91350729885073),new PdfPoint(503.4101782807511, 332.90619550356894),new PdfPoint(811.0980660123346, 741.7360232136574),new PdfPoint(542.0005608486149, 765.7287114183756)),
                new PdfRectangle(new PdfPoint(579.208444309769, 296.4897762101177),new PdfPoint(476.04435513628124, -44.12900650922268),new PdfPoint(646.7287250141156, 276.0397346592921),new PdfPoint(543.564635840628, -64.57904806004831)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(772.5076834444708, 308.91350729885073),new PdfPoint(503.4101782807511, 332.90619550356894),new PdfPoint(811.0980660123346, 741.7360232136574),new PdfPoint(542.0005608486149, 765.7287114183756)),
                new PdfRectangle(new PdfPoint(353.1061180186714, 672.2227449084635),new PdfPoint(75.02654644357133, 165.46736090530658),new PdfPoint(687.9665051764547, 488.46972335649434),new PdfPoint(409.88693360135466, -18.285660646662677)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(772.5076834444708, 308.91350729885073),new PdfPoint(503.4101782807511, 332.90619550356894),new PdfPoint(811.0980660123346, 741.7360232136574),new PdfPoint(542.0005608486149, 765.7287114183756)),
                new PdfRectangle(new PdfPoint(549.0571767481631, 867.6252716139468),new PdfPoint(679.7176230151732, 984.8937301967728),new PdfPoint(718.6237966712662, 678.6942453270042),new PdfPoint(849.2842429382763, 795.9627039098302)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(772.5076834444708, 308.91350729885073),new PdfPoint(503.4101782807511, 332.90619550356894),new PdfPoint(811.0980660123346, 741.7360232136574),new PdfPoint(542.0005608486149, 765.7287114183756)),
                new PdfRectangle(new PdfPoint(-248.7304687596478, 247.1474790098713),new PdfPoint(-247.96709695848517, 245.9369390186863),new PdfPoint(337.5409934273866, 616.8528215172801),new PdfPoint(338.3043652285492, 615.6422815260951)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(772.5076834444708, 308.91350729885073),new PdfPoint(503.4101782807511, 332.90619550356894),new PdfPoint(811.0980660123346, 741.7360232136574),new PdfPoint(542.0005608486149, 765.7287114183756)),
                new PdfRectangle(new PdfPoint(454.59917081714934, 422.2937331229763),new PdfPoint(434.24487698775135, 216.27621805619458),new PdfPoint(14.117597091710621, 465.81280661115466),new PdfPoint(-6.236696737687254, 259.79529154437296)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(772.5076834444708, 308.91350729885073),new PdfPoint(503.4101782807511, 332.90619550356894),new PdfPoint(811.0980660123346, 741.7360232136574),new PdfPoint(542.0005608486149, 765.7287114183756)),
                new PdfRectangle(new PdfPoint(564.9111008147394, 502.50169448339113),new PdfPoint(538.1097429603419, 831.7638495653706),new PdfPoint(941.2811878249518, 533.1375547909322),new PdfPoint(914.4798299705542, 862.3997098729117)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(772.5076834444708, 308.91350729885073),new PdfPoint(503.4101782807511, 332.90619550356894),new PdfPoint(811.0980660123346, 741.7360232136574),new PdfPoint(542.0005608486149, 765.7287114183756)),
                new PdfRectangle(new PdfPoint(108.10171020910246, 396.29559315961126),new PdfPoint(151.2918405497648, 330.04231760357635),new PdfPoint(779.9306594781885, 834.2570319268295),new PdfPoint(823.1207898188509, 768.0037563707947)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(417.0694002760241, 657.7660995748632),new PdfPoint(859.9717962207591, 913.5731678162075),new PdfPoint(531.9281311900845, 458.90057382240866),new PdfPoint(974.8305271348195, 714.707642063753)),
                new PdfRectangle(new PdfPoint(127.81562246457167, 452.89342321965273),new PdfPoint(306.2910797602115, 322.56589497273774),new PdfPoint(-14.967204368365145, 257.3612079175049),new PdfPoint(163.50825292727473, 127.03367967058983)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(417.0694002760241, 657.7660995748632),new PdfPoint(859.9717962207591, 913.5731678162075),new PdfPoint(531.9281311900845, 458.90057382240866),new PdfPoint(974.8305271348195, 714.707642063753)),
                new PdfRectangle(new PdfPoint(442.59511410046105, 7.42376038954405),new PdfPoint(884.3103759572513, 35.23594931480268),new PdfPoint(402.8010154260708, 639.4366078339051),new PdfPoint(844.516277282861, 667.2487967591637)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(417.0694002760241, 657.7660995748632),new PdfPoint(859.9717962207591, 913.5731678162075),new PdfPoint(531.9281311900845, 458.90057382240866),new PdfPoint(974.8305271348195, 714.707642063753)),
                new PdfRectangle(new PdfPoint(801.2119098196629, 707.7888825869748),new PdfPoint(787.5427116147556, 730.4541634357847),new PdfPoint(930.8540858865242, 785.9747481050031),new PdfPoint(917.1848876816168, 808.640028953813)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(417.0694002760241, 657.7660995748632),new PdfPoint(859.9717962207591, 913.5731678162075),new PdfPoint(531.9281311900845, 458.90057382240866),new PdfPoint(974.8305271348195, 714.707642063753)),
                new PdfRectangle(new PdfPoint(558.1108741008619, 482.1526487619336),new PdfPoint(552.692261136648, 793.8738042914648),new PdfPoint(894.1902255817276, 487.9946774424874),new PdfPoint(888.7716126175137, 799.7158329720186)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(417.0694002760241, 657.7660995748632),new PdfPoint(859.9717962207591, 913.5731678162075),new PdfPoint(531.9281311900845, 458.90057382240866),new PdfPoint(974.8305271348195, 714.707642063753)),
                new PdfRectangle(new PdfPoint(482.6433027844688, 1078.6935053557615),new PdfPoint(656.7960045523818, 325.3932500242454),new PdfPoint(167.53165593930407, 1005.8440084012291),new PdfPoint(341.68435770721715, 252.5437530697129)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(417.0694002760241, 657.7660995748632),new PdfPoint(859.9717962207591, 913.5731678162075),new PdfPoint(531.9281311900845, 458.90057382240866),new PdfPoint(974.8305271348195, 714.707642063753)),
                new PdfRectangle(new PdfPoint(549.3761713793539, 417.60175999879675),new PdfPoint(565.2023981464638, 794.9445139104314),new PdfPoint(1037.4978314072423, 397.1293274385878),new PdfPoint(1053.324058174352, 774.4720813502222)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(417.0694002760241, 657.7660995748632),new PdfPoint(859.9717962207591, 913.5731678162075),new PdfPoint(531.9281311900845, 458.90057382240866),new PdfPoint(974.8305271348195, 714.707642063753)),
                new PdfRectangle(new PdfPoint(343.0993830454707, 166.10143467737794),new PdfPoint(233.9128231600722, 119.83433154463962),new PdfPoint(311.2339077030112, 241.30133690108602),new PdfPoint(202.04734781761272, 195.0342337683477)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(417.0694002760241, 657.7660995748632),new PdfPoint(859.9717962207591, 913.5731678162075),new PdfPoint(531.9281311900845, 458.90057382240866),new PdfPoint(974.8305271348195, 714.707642063753)),
                new PdfRectangle(new PdfPoint(788.9441519563835, 76.02659693382037),new PdfPoint(889.558100446751, 634.9977239331928),new PdfPoint(380.2487719073182, 149.5911406648135),new PdfPoint(480.86272039768573, 708.5622676641859)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(417.0694002760241, 657.7660995748632),new PdfPoint(859.9717962207591, 913.5731678162075),new PdfPoint(531.9281311900845, 458.90057382240866),new PdfPoint(974.8305271348195, 714.707642063753)),
                new PdfRectangle(new PdfPoint(591.1926841219878, 956.7003626962485),new PdfPoint(319.50175927767003, 523.7940768559506),new PdfPoint(940.7108934300119, 737.3435904735909),new PdfPoint(669.0199685856942, 304.4373046332931)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(417.0694002760241, 657.7660995748632),new PdfPoint(859.9717962207591, 913.5731678162075),new PdfPoint(531.9281311900845, 458.90057382240866),new PdfPoint(974.8305271348195, 714.707642063753)),
                new PdfRectangle(new PdfPoint(230.90140944673294, 852.4159089620722),new PdfPoint(856.9438766691358, 979.2138476156674),new PdfPoint(267.0081560592838, 674.1452132968273),new PdfPoint(893.0506232816867, 800.9431519504224)),
                true
            },
                new object[]
            {
                new PdfRectangle(new PdfPoint(469.97027460636644, 532.6048074981176),new PdfPoint(376.1246989196788, 463.1989354882081),new PdfPoint(781.5950183905622, 111.24847795774326),new PdfPoint(687.7494427038746, 41.842605947833874)),
                new PdfRectangle(new PdfPoint(962.8541775715657, 705.8766989814462),new PdfPoint(919.3397530381919, 805.8055268572101),new PdfPoint(396.1203414483523, 459.0900882833521),new PdfPoint(352.6059169149785, 559.018916159116)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(469.97027460636644, 532.6048074981176),new PdfPoint(376.1246989196788, 463.1989354882081),new PdfPoint(781.5950183905622, 111.24847795774326),new PdfPoint(687.7494427038746, 41.842605947833874)),
                new PdfRectangle(new PdfPoint(425.5378502159567, 719.9020952476875),new PdfPoint(347.50405435503717, 737.2344867527751),new PdfPoint(463.77713808168244, 892.0628026898829),new PdfPoint(385.743342220763, 909.3951941949705)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(469.97027460636644, 532.6048074981176),new PdfPoint(376.1246989196788, 463.1989354882081),new PdfPoint(781.5950183905622, 111.24847795774326),new PdfPoint(687.7494427038746, 41.842605947833874)),
                new PdfRectangle(new PdfPoint(547.1317401025856, 169.0188083562877),new PdfPoint(500.32520588681984, 126.95500706273566),new PdfPoint(331.71208504006523, 408.7272309276441),new PdfPoint(284.90555082429944, 366.6634296340919)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(469.97027460636644, 532.6048074981176),new PdfPoint(376.1246989196788, 463.1989354882081),new PdfPoint(781.5950183905622, 111.24847795774326),new PdfPoint(687.7494427038746, 41.842605947833874)),
                new PdfRectangle(new PdfPoint(116.96227638886671, 757.7074040077403),new PdfPoint(769.6000159038648, 866.4837207678485),new PdfPoint(179.65839880062163, 381.54228989193257),new PdfPoint(832.2961383156196, 490.3186066520408)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(469.97027460636644, 532.6048074981176),new PdfPoint(376.1246989196788, 463.1989354882081),new PdfPoint(781.5950183905622, 111.24847795774326),new PdfPoint(687.7494427038746, 41.842605947833874)),
                new PdfRectangle(new PdfPoint(52.76255814557737, 842.853694161234),new PdfPoint(318.24141192655827, 1090.6594469329102),new PdfPoint(109.07140404649431, 782.5289934619775),new PdfPoint(374.5502578274752, 1030.3347462336537)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(469.97027460636644, 532.6048074981176),new PdfPoint(376.1246989196788, 463.1989354882081),new PdfPoint(781.5950183905622, 111.24847795774326),new PdfPoint(687.7494427038746, 41.842605947833874)),
                new PdfRectangle(new PdfPoint(342.94420568770863, 635.4315714275659),new PdfPoint(332.5456770465351, 576.5838167057161),new PdfPoint(998.0982321979317, 519.6644033980061),new PdfPoint(987.6997035567581, 460.8166486761563)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(469.97027460636644, 532.6048074981176),new PdfPoint(376.1246989196788, 463.1989354882081),new PdfPoint(781.5950183905622, 111.24847795774326),new PdfPoint(687.7494427038746, 41.842605947833874)),
                new PdfRectangle(new PdfPoint(322.69425587804983, 599.818566085786),new PdfPoint(298.5281713244782, 305.39954588230967),new PdfPoint(591.7722294033604, 577.7324893940614),new PdfPoint(567.6061448497887, 283.3134691905851)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(469.97027460636644, 532.6048074981176),new PdfPoint(376.1246989196788, 463.1989354882081),new PdfPoint(781.5950183905622, 111.24847795774326),new PdfPoint(687.7494427038746, 41.842605947833874)),
                new PdfRectangle(new PdfPoint(411.1290124479265, 253.9940517784239),new PdfPoint(730.5160525449623, 555.0599217729599),new PdfPoint(180.8898736272251, 498.2442454777927),new PdfPoint(500.2769137242609, 799.3101154723286)),
                true
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(469.97027460636644, 532.6048074981176),new PdfPoint(376.1246989196788, 463.1989354882081),new PdfPoint(781.5950183905622, 111.24847795774326),new PdfPoint(687.7494427038746, 41.842605947833874)),
                new PdfRectangle(new PdfPoint(890.8364774185975, 776.4350991353115),new PdfPoint(777.5942368557554, 581.5834552552633),new PdfPoint(757.0855116786995, 854.1673611589981),new PdfPoint(643.8432711158574, 659.3157172789499)),
                false
            },
            new object[]
            {
                new PdfRectangle(new PdfPoint(469.97027460636644, 532.6048074981176),new PdfPoint(376.1246989196788, 463.1989354882081),new PdfPoint(781.5950183905622, 111.24847795774326),new PdfPoint(687.7494427038746, 41.842605947833874)),
                new PdfRectangle(new PdfPoint(474.35954969243755, 668.4738921584808),new PdfPoint(390.4519290279624, 254.00673089439942),new PdfPoint(382.2334383885094, 687.1245441836189),new PdfPoint(298.32581772403427, 272.65738291953767)),
                true
            }
        };

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
        #endregion

        [Theory]
        [MemberData(nameof(AreaData))]
        public void Area(double[] data, double[] expected)
        {
            double area = expected[0];
            PdfRectangle rectangle = new PdfRectangle(data[0], data[1], data[2], data[3]);
            Assert.Equal(area, rectangle.Area, DoubleComparer);
            Assert.Equal(area, TransformationMatrix.GetRotationMatrix(72.657952132).Transform(rectangle).Area, DoubleComparer);
            Assert.Equal(area, TransformationMatrix.GetRotationMatrix(114.182147).Transform(rectangle).Area, DoubleComparer);
            Assert.Equal(area, TransformationMatrix.GetRotationMatrix(194.045).Transform(rectangle).Area, DoubleComparer);
            Assert.Equal(area, TransformationMatrix.GetRotationMatrix(-74.4657).Transform(rectangle).Area, DoubleComparer);
            Assert.Equal(area, TransformationMatrix.GetRotationMatrix(45).Transform(rectangle).Area, DoubleComparer);
            Assert.Equal(area, TransformationMatrix.GetRotationMatrix(-45).Transform(rectangle).Area, DoubleComparer);
            Assert.Equal(area, TransformationMatrix.GetRotationMatrix(180).Transform(rectangle).Area, DoubleComparer);
            Assert.Equal(area, TransformationMatrix.GetRotationMatrix(-180).Transform(rectangle).Area, DoubleComparer);
        }

        [Theory]
        [MemberData(nameof(CentroidData))]
        public void Centroid(PdfRectangle source, PdfPoint expected)
        {
            Assert.Equal(expected, source.Centroid, PointComparer);
        }

        [Theory]
        [MemberData(nameof(IntersectsWithData))]
        public void IntersectsWith(PdfRectangle source, PdfRectangle other, bool expected)
        {
            Assert.Equal(expected, source.IntersectsWith(other));
        }

        [Fact]
        public void IntersectAxisAligned()
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

        [Theory]
        [MemberData(nameof(ContainsPointData))]
        public void ContainsPoint(PdfRectangle source, PdfPoint other, bool expected)
        {
            Assert.Equal(expected, source.Contains(other));
        }

        [Theory]
        [MemberData(nameof(ContainsRectangleData))]
        public void ContainsRectangle(PdfRectangle source, PdfRectangle other, bool expected)
        {
            Assert.Equal(expected, source.Contains(other));
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

        [Fact]
        public void Issue261()
        {
            // Some points have negative coordinates after rotation, resulting in negative Height
            PdfRectangle rect = new PdfRectangle(new PdfPoint(401.51, 461.803424),
                new PdfPoint(300.17322363281249, 461.803424),
                new PdfPoint(401.51, 291.45),
                new PdfPoint(300.17322363281249, 291.45));

            Assert.True(rect.Height > 0);
        }

        [Theory]
        [InlineData(595, 842, PageSize.A4)]
        [InlineData(594, 843, PageSize.Custom)]
        [InlineData(596, 841, PageSize.Custom)]
        [InlineData(842, 595, PageSize.A4)]
        [InlineData(595.3, 841.5, PageSize.A4)]
        [InlineData(841.5, 595.3, PageSize.A4)]
        [InlineData(1224, 792, PageSize.Ledger)]
        [InlineData(792, 1224, PageSize.Tabloid)]
        public void Parse(double w, double h, PageSize expectedPageSize)
        {
            var r = new PdfRectangle(0, 0, w, h);
            Assert.Equal(expectedPageSize, r.GetPageSize());
        }
    }
}
