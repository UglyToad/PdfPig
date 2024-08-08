namespace UglyToad.PdfPig.Tests.Geometry
{
    using PdfPig.Core;
    using UglyToad.PdfPig.Geometry;

    public class PdfPointTests
    {
        private static readonly DoubleComparer DoubleComparer = new DoubleComparer(0.001);
        private static readonly DoubleComparer PreciseDoubleComparer = new DoubleComparer(0.000001);
        private static readonly PointComparer PointComparer = new PointComparer(DoubleComparer);

        #region data
        public static IEnumerable<object[]> GrahamScanData => new[]
        {
            new object[]
            {
                new PdfPoint[]
                {
                    new PdfPoint(374.54011885, 950.71430641),
                    new PdfPoint(731.99394181, 598.6584842),
                    new PdfPoint(156.01864044, 155.99452034),
                    new PdfPoint(58.08361217, 866.17614577),
                    new PdfPoint(601.11501174, 708.0725778),
                    new PdfPoint(20.5844943, 969.90985216),
                    new PdfPoint(832.4426408, 212.33911068),
                    new PdfPoint(181.82496721, 183.40450985),
                    new PdfPoint(304.24224296, 524.75643163),
                    new PdfPoint(431.94501864, 291.2291402),
                    new PdfPoint(611.85289472, 139.49386065),
                    new PdfPoint(292.14464854, 366.36184329),
                    new PdfPoint(456.06998422, 785.17596139),
                    new PdfPoint(199.67378216, 514.23443841),
                    new PdfPoint(592.41456886, 46.45041272),
                    new PdfPoint(607.5448519, 170.52412369),
                    new PdfPoint(65.05159299, 948.88553725),
                    new PdfPoint(965.63203307, 808.39734812),
                    new PdfPoint(304.61376917, 97.67211401),
                    new PdfPoint(684.23302651, 440.15249374)
                },
                new PdfPoint[]
                {
                    new PdfPoint(374.54011885, 950.71430641),
                    new PdfPoint(156.01864044, 155.99452034),
                    new PdfPoint(20.5844943, 969.90985216),
                    new PdfPoint(832.4426408, 212.33911068),
                    new PdfPoint(592.41456886, 46.45041272),
                    new PdfPoint(965.63203307, 808.39734812),
                    new PdfPoint(304.61376917, 97.67211401)
                }
            },
            new object[]
            {
                new PdfPoint[]
                {
                    new PdfPoint(15.45661653, 928.31856259),
                    new PdfPoint(428.18414832, 966.65481904),
                    new PdfPoint(963.61997709, 853.00945547)
                },
                new PdfPoint[]
                {
                    new PdfPoint(15.45661653, 928.31856259),
                    new PdfPoint(428.18414832, 966.65481904),
                    new PdfPoint(963.61997709, 853.00945547)
                }
            },
            new object[]
            {
                new PdfPoint[]
                {
                    new PdfPoint(511.34239886, 501.51629469),
                    new PdfPoint(798.29517897, 649.96393078),
                    new PdfPoint(701.96687726, 795.79266944),
                    new PdfPoint(890.00534182, 337.99515685),
                    new PdfPoint(375.58295264, 93.98193984),
                    new PdfPoint(578.280141, 35.9422738),
                    new PdfPoint(465.59801813, 542.64463471),
                    new PdfPoint(286.54125213, 590.83326057),
                    new PdfPoint(30.50024994, 37.34818875),
                    new PdfPoint(822.60056066, 360.19064141)
                },
                new PdfPoint[]
                {
                    new PdfPoint(798.29517897, 649.96393078),
                    new PdfPoint(701.96687726, 795.79266944),
                    new PdfPoint(890.00534182, 337.99515685),
                    new PdfPoint(578.280141, 35.9422738),
                    new PdfPoint(286.54125213, 590.83326057),
                    new PdfPoint(30.50024994, 37.34818875)
                }
            },
            new object[]
            {
                new PdfPoint[]
                {
                    new PdfPoint(744.3155504888274, 444.2270145755189),
                    new PdfPoint(569.808503219366, 795.0780697464061),
                    new PdfPoint(979.4238467088927, 747.7769460740175),
                    new PdfPoint(263.9656530143659, 534.1164778047929),
                    new PdfPoint(700.0199185779105, 59.67088755550021),
                    new PdfPoint(350.4405052982569, 201.5075034147189),
                    new PdfPoint(951.4434324059339, 276.4851544966993),
                    new PdfPoint(221.2620795357345, 889.4493759697666),
                    new PdfPoint(26.40411497910822, 836.0708485933704),
                    new PdfPoint(967.4534816241033, 692.8854748787957),
                },
                new PdfPoint[]
                {
                    new PdfPoint(979.4238467088927, 747.7769460740175),
                    new PdfPoint(700.0199185779105, 59.67088755550021),
                    new PdfPoint(350.4405052982569, 201.5075034147189),
                    new PdfPoint(951.4434324059339, 276.4851544966993),
                    new PdfPoint(221.2620795357345, 889.4493759697666),
                    new PdfPoint(26.40411497910822, 836.0708485933704),
                }
            },
            new object[]
            {
                new PdfPoint[]
                {
                    new PdfPoint(120.21183721137064, 840.2513067067979),
                    new PdfPoint(415.52861888639114, 204.0116313873851),
                    new PdfPoint(415.9664683980775, 832.443368995516),
                    new PdfPoint(277.74879682552734, 502.12519702578516),
                    new PdfPoint(395.35090532250103, 384.0997616867551),
                    new PdfPoint(26.98104432607229, 654.2675428223525),
                    new PdfPoint(507.0471750863688, 822.9947002774292),
                },
                new PdfPoint[]
                {
                    new PdfPoint(120.21183721137064, 840.2513067067979),
                    new PdfPoint(415.52861888639114, 204.0116313873851),
                    new PdfPoint(415.9664683980775, 832.443368995516),
                    new PdfPoint(26.98104432607229, 654.2675428223525),
                    new PdfPoint(507.0471750863688, 822.9947002774292),
                }
            },
            new object[]
            {
                new PdfPoint[]
                {
                    new PdfPoint(101.52924170788602, 157.72180559576165),
                    new PdfPoint(89.13969305519842, 437.7399878683488),
                    new PdfPoint(963.8109736953475, 34.8258382181843),
                    new PdfPoint(323.44164253397423, 760.0469522615875),
                    new PdfPoint(553.8798287152455, 601.37868940649),
                    new PdfPoint(24.169651959285442, 205.03651103426347),
                    new PdfPoint(598.6426557134453, 973.1839362109574),
                    new PdfPoint(404.2148279309453, 642.4272597428419),
                    new PdfPoint(425.99787946258795, 235.338843056625),
                    new PdfPoint(733.837562543066, 304.97834592908157),
                    new PdfPoint(253.06825516770635, 639.1969849161718),
                    new PdfPoint(702.043917830561, 241.6302720665372),
                    new PdfPoint(43.233323888316356, 214.65896998517496),
                    new PdfPoint(192.04610854054195, 609.086536570487),
                    new PdfPoint(93.436372304046, 130.4501167748684),
                },
                new PdfPoint[]
                {
                    new PdfPoint(89.13969305519842, 437.7399878683488),
                    new PdfPoint(963.8109736953475, 34.8258382181843),
                    new PdfPoint(323.44164253397423, 760.0469522615875),
                    new PdfPoint(24.169651959285442, 205.03651103426347),
                    new PdfPoint(598.6426557134453, 973.1839362109574),
                    new PdfPoint(192.04610854054195, 609.086536570487),
                    new PdfPoint(93.436372304046, 130.4501167748684),
                }
            },
            new object[]
            {
                new PdfPoint[]
                {
                    new PdfPoint(440.61717693569, 109.0846453611043),
                    new PdfPoint(306.3112668458863, 236.31457456363236),
                    new PdfPoint(520.2265439015014, 849.1406402603602),
                    new PdfPoint(991.7951837792907, 716.5936071305995),
                    new PdfPoint(210.02069783227006, 396.9897189605061),
                    new PdfPoint(844.0889978638492, 40.8614368982706),
                    new PdfPoint(105.18279221346272, 598.2338764791806),
                    new PdfPoint(481.07852218200696, 791.2951914667244),
                    new PdfPoint(306.45291569755994, 923.936778153581),
                    new PdfPoint(959.6247632028106, 131.810977239897),
                    new PdfPoint(800.8517955224195, 350.74228914407433),
                    new PdfPoint(663.4680772483941, 352.304508375042),
                    new PdfPoint(222.58588801803648, 629.9145459539977),
                    new PdfPoint(158.01097092542204, 266.66678727838047),
                    new PdfPoint(63.28771038884462, 52.73381563522583),
                    new PdfPoint(931.6304534780719, 325.3140887623456),
                    new PdfPoint(781.7202794605689, 932.8040026798695),
                    new PdfPoint(691.9750284379987, 896.4394191971841),
                    new PdfPoint(354.5191195854491, 319.9839791539775),
                    new PdfPoint(177.99172848582566, 831.9975645935383),
                    new PdfPoint(237.7316548429551, 731.2752736686494),
                    new PdfPoint(989.397359729622, 217.92540678980444),
                    new PdfPoint(509.249200783335, 621.5474147516464),
                    new PdfPoint(744.8852601187607, 917.8174495950592),
                    new PdfPoint(308.86394317440534, 345.03042856046795),
                    new PdfPoint(716.4931552373788, 116.87982985630296),
                    new PdfPoint(8.999928995218953, 752.2282844071755),
                    new PdfPoint(294.01152638529305, 979.8820875548174),
                    new PdfPoint(919.862114089302, 218.09062242740873),
                    new PdfPoint(58.10245428844474, 679.4146645720332),
                    new PdfPoint(298.5501112825898, 76.27703572384658),
                    new PdfPoint(642.7813627195759, 506.9457773647057),
                    new PdfPoint(95.40371527671387, 849.060165476576),
                    new PdfPoint(462.81907479511983, 59.56530610812505),
                    new PdfPoint(883.4162795690498, 4.108824339342565),
                    new PdfPoint(255.30517910829244, 890.9400020559517),
                    new PdfPoint(127.06302493344879, 246.8462701575741),
                    new PdfPoint(995.1831247109899, 371.7538001202371),
                    new PdfPoint(189.37270784653683, 888.4231305357099),
                    new PdfPoint(924.0107481155084, 775.4673044677166),
                    new PdfPoint(865.7452979418968, 373.2043310431542),
                    new PdfPoint(409.76929412279594, 192.26266847186992),
                    new PdfPoint(438.8872219529338, 819.6826378850956),
                },
                new PdfPoint[]
                {
                    new PdfPoint(991.7951837792907, 716.5936071305995),
                    new PdfPoint(959.6247632028106, 131.810977239897),
                    new PdfPoint(63.28771038884462, 52.73381563522583),
                    new PdfPoint(781.7202794605689, 932.8040026798695),
                    new PdfPoint(989.397359729622, 217.92540678980444),
                    new PdfPoint(8.999928995218953, 752.2282844071755),
                    new PdfPoint(294.01152638529305, 979.8820875548174),
                    new PdfPoint(95.40371527671387, 849.060165476576),
                    new PdfPoint(883.4162795690498, 4.108824339342565),
                    new PdfPoint(995.1831247109899, 371.7538001202371),
                }
            }
        };

        public static IEnumerable<object[]> MinimumAreaRectangleData => new[]
        {
            new object[]
            {
                new PdfPoint[]
                {
                    // collinear points case: y = 15.7894 + 1.572431x
                    new PdfPoint(16, 40.948296),
                    new PdfPoint(18, 44.093158),
                    new PdfPoint(21, 48.810451),
                    new PdfPoint(30, 62.96233),
                    new PdfPoint(49, 92.838519),
                    new PdfPoint(55, 102.273105),
                    new PdfPoint(60, 110.13526),
                    new PdfPoint(64, 116.424984),
                    new PdfPoint(65, 117.997415),
                    new PdfPoint(68, 122.714708),
                    new PdfPoint(75, 133.721725),
                    new PdfPoint(84, 147.873604),
                    new PdfPoint(86, 151.018466),
                    new PdfPoint(90, 157.30819),
                    new PdfPoint(97, 168.315207),
                    new PdfPoint(99, 171.460069),
                    new PdfPoint(105, 180.894655),
                    new PdfPoint(106, 182.467086),
                    new PdfPoint(110, 188.75681),
                    new PdfPoint(113, 193.474103),
                    new PdfPoint(119, 202.908689),
                    new PdfPoint(121, 206.053551),
                    new PdfPoint(122, 207.625982),
                    new PdfPoint(123, 209.198413)
                },
                new PdfPoint[]
                {
                    new PdfPoint(16, 40.948296),
                    new PdfPoint(16, 40.948296),
                    new PdfPoint(123, 209.198413),
                    new PdfPoint(123, 209.198413)
                }
            },
            new object[]
            {
                new PdfPoint[]
                {
                    // collinear points case: y = 87.483 + 99.2934520998549x
                    new PdfPoint(10.5114328889726, 1131.19945806204),
                    new PdfPoint(11.1542565096881, 1195.02763445421),
                    new PdfPoint(15.3153242359356, 1608.19441341462),
                    new PdfPoint(15.795577716642, 1655.88043939693),
                    new PdfPoint(16.3319701583886, 1709.14069661821),
                    new PdfPoint(17.1302715938114, 1788.40680195762),
                    new PdfPoint(17.9770489556469, 1872.48624937427),
                    new PdfPoint(22.4037700355884, 2312.03066688486),
                    new PdfPoint(23.7647419889928, 2447.16627034947),
                    new PdfPoint(26.9327398551415, 2761.72771472434),
                    new PdfPoint(28.7600875228236, 2943.17137283512),
                    new PdfPoint(32.8221934632313, 3346.51189445353),
                    new PdfPoint(34.4943972831614, 3512.55078434895),
                    new PdfPoint(34.5363374306664, 3516.7151663763),
                    new PdfPoint(36.0282475627216, 3664.85207361081)
                },
                new PdfPoint[]
                {
                    new PdfPoint(10.5114328889726, 1131.19945806204),
                    new PdfPoint(10.5114328889726, 1131.19945806204),
                    new PdfPoint(36.0282475627216, 3664.85207361081),
                    new PdfPoint(36.0282475627216, 3664.85207361081)
                }
            },
            new object[]
            {
                new PdfPoint[]
                {
                    // collinear points case: vertical
                    new PdfPoint(446.78, 217.9),
                    new PdfPoint(446.78, 228.82),
                    new PdfPoint(446.78, 247.52),
                    new PdfPoint(446.78, 256.84),
                    new PdfPoint(446.78, 301.4),
                    new PdfPoint(446.78, 321.39),
                    new PdfPoint(446.78, 369.08),
                    new PdfPoint(446.78, 387.05),
                    new PdfPoint(446.78, 393.22),
                    new PdfPoint(446.78, 397.29),
                    new PdfPoint(446.78, 463.16),
                    new PdfPoint(446.78, 471.88),
                    new PdfPoint(446.78, 480.13),
                    new PdfPoint(446.78, 495.82),
                    new PdfPoint(446.78, 498.99)
                },
                new PdfPoint[]
                {
                    new PdfPoint(446.78, 217.9),
                    new PdfPoint(446.78, 217.9),
                    new PdfPoint(446.78, 498.99),
                    new PdfPoint(446.78, 498.99)
                }
            },
            new object[]
            {
                new PdfPoint[]
                {
                    // collinear points case: horizontal
                    new PdfPoint(220, 208.821),
                    new PdfPoint(258.92, 208.821),
                    new PdfPoint(268.61, 208.821),
                    new PdfPoint(283.56, 208.821),
                    new PdfPoint(312.49, 208.821),
                    new PdfPoint(344.93, 208.821),
                    new PdfPoint(356, 208.821),
                    new PdfPoint(356.06, 208.821),
                    new PdfPoint(366.71, 208.821),
                    new PdfPoint(371.07, 208.821),
                    new PdfPoint(430.95, 208.821),
                    new PdfPoint(445.84, 208.821),
                    new PdfPoint(464.95, 208.821),
                    new PdfPoint(470.19, 208.821),
                    new PdfPoint(498.19, 208.821)
                },
                new PdfPoint[]
                {
                    new PdfPoint(220, 208.821),
                    new PdfPoint(220, 208.821),
                    new PdfPoint(498.19, 208.821),
                    new PdfPoint(498.19, 208.821)
                }
            },
            new object[]
            {
                new PdfPoint[]
                {
                    new PdfPoint(433.8664544437276, 532.7739491464265),
                    new PdfPoint(653.8805470338659, 817.2121262831644),
                    new PdfPoint(531.2551432261636, 360.68316491741035),
                    new PdfPoint(418.79076902856593, 111.73491145462933),
                },
                new PdfPoint[]
                {
                    new PdfPoint(653.880547033866, 817.2121262831644),
                    new PdfPoint(461.3184174727883, 100.31182441133716),
                    new PdfPoint(327.3696296297328, 136.2909748899142),
                    new PdfPoint(519.9317591908105, 853.1912767617414),
                }
            },
            new object[]
            {
                new PdfPoint[]
                {
                    new PdfPoint(556.0000554986003, 83.08490003544556),
                    new PdfPoint(413.249212383334, 618.580307645359),
                    new PdfPoint(526.8827917872168, 111.78528983363456),
                    new PdfPoint(220.19687765118178, 377.83114567978316),
                },
                new PdfPoint[]
                {
                    new PdfPoint(556.0000554986004, 83.08490003544566),
                    new PdfPoint(315.83633925060445, 19.06273944752293),
                    new PdfPoint(173.0854961353382, 554.5581470574363),
                    new PdfPoint(413.24921238333417, 618.580307645359),
                }
            },
            new object[]
            {
                new PdfPoint[]
                {
                    new PdfPoint(14.004896521066401, 809.4941990011544),
                    new PdfPoint(703.95616092419, 970.7474069029789),
                    new PdfPoint(835.551079058811, 661.2654117428186),
                    new PdfPoint(200.4833132016346, 14.581989889114745),
                    new PdfPoint(73.40355670360321, 345.2226372321663),
                },
                new PdfPoint[]
                {
                    new PdfPoint(945.970699704068, 188.81492303383516),
                    new PdfPoint(153.25937285270737, 3.544961240987477),
                    new PdfPoint(-32.56092111592515, 798.6109846932103),
                    new PdfPoint(760.1504057354355, 983.8809464860581),
                }
            },
            new object[]
            {
                // duplicate points case 
                new PdfPoint[]
                {
                    new PdfPoint(14.004896521066401, 809.4941990011544),
                    new PdfPoint(703.95616092419, 970.7474069029789),
                    new PdfPoint(835.551079058811, 661.2654117428186),
                    new PdfPoint(703.95616092419, 970.7474069029789),
                    new PdfPoint(703.95616092419, 970.7474069029789),
                    new PdfPoint(200.4833132016346, 14.581989889114745),
                    new PdfPoint(200.4833132016346, 14.581989889114745),
                    new PdfPoint(73.40355670360321, 345.2226372321663),
                    new PdfPoint(73.40355670360321, 345.2226372321663),
                    new PdfPoint(73.40355670360321, 345.2226372321663),
                },
                new PdfPoint[]
                {
                    new PdfPoint(945.970699704068, 188.81492303383516),
                    new PdfPoint(153.25937285270737, 3.544961240987477),
                    new PdfPoint(-32.56092111592515, 798.6109846932103),
                    new PdfPoint(760.1504057354355, 983.8809464860581),
                }
            },
            new object[]
            {
                new PdfPoint[]
                {
                    new PdfPoint(737.1041856902102, 648.0900313433699),
                    new PdfPoint(258.83885597639045, 32.15501719959235),
                    new PdfPoint(354.5500618726748, 908.7838113897652),
                    new PdfPoint(867.6475306924474, 47.361938752654595),
                    new PdfPoint(352.7960490248145, 283.67860449564785),
                    new PdfPoint(955.4087841797756, 833.327418435315),
                    new PdfPoint(578.2403790703082, 67.4511148622331),
                    new PdfPoint(722.9995401934759, 407.36102955779796),
                    new PdfPoint(404.3710508165602, 736.1127320695537),
                    new PdfPoint(56.25949705397548, 45.503737933916824),
                },
                new PdfPoint[]
                {
                    new PdfPoint(956.3312379371301, 841.5886588013605),
                    new PdfPoint(857.4507470077488, -43.95763180386355),
                    new PdfPoint(56.25949705397548, 45.503737933916824),
                    new PdfPoint(155.13998798335692, 931.0500285391407),
                }
            },
            new object[]
            {
                new PdfPoint[]
                {
                    new PdfPoint(37.79696601832361, 984.5348223984452),
                    new PdfPoint(881.8169100214427, 818.3604232045343),
                    new PdfPoint(732.8834668881201, 907.4453370173243),
                    new PdfPoint(142.89010125285918, 183.62301422208304),
                    new PdfPoint(292.4319539617013, 383.1685740348906),
                    new PdfPoint(658.9302664366852, 781.9569855570855),
                    new PdfPoint(501.7748878713084, 321.0551716869758),
                    new PdfPoint(104.96397346166219, 658.8420562657931),
                    new PdfPoint(931.1420702804029, 235.94015835854032),
                    new PdfPoint(489.5915692144058, 835.989512769871),
                },
                new PdfPoint[]
                {
                    new PdfPoint(931.1420702804029, 235.9401583585403),
                    new PdfPoint(91.18213738971586, 180.19110018351975),
                    new PdfPoint(37.79696601832373, 984.5348223984452),
                    new PdfPoint(877.7568989090107, 1040.2838805734657),
                }
            },
            new object[]
            {
                new PdfPoint[]
                {
                    new PdfPoint(360.50496116131137, 475.0257075944493),
                    new PdfPoint(463.6183053562707, 550.6502767074434),
                    new PdfPoint(719.7530742800635, 986.4011287537438),
                    new PdfPoint(746.9948030700444, 387.8044519192034),
                    new PdfPoint(868.5846874204865, 248.33194807842352),
                    new PdfPoint(485.3109455640756, 39.94793327837021),
                    new PdfPoint(504.8344865133781, 708.8088010613369),
                    new PdfPoint(352.0102119724019, 820.2239288583693),
                    new PdfPoint(28.306241810454267, 579.9713087166957),
                    new PdfPoint(801.671925406638, 886.8351079919669),
                },
                new PdfPoint[]
                {
                    new PdfPoint(1131.0092157743165, 443.1030548242194),
                    new PdfPoint(485.483254766563, -36.005383122939776),
                    new PdfPoint(28.306241810454186, 579.9713087166958),
                    new PdfPoint(673.8322028182079, 1059.079746663855),
                }
            },
            new object[]
            {
                new PdfPoint[]
                {
                    new PdfPoint(384.5196462100093, 457.53938224300975),
                    new PdfPoint(388.62152246674617, 197.75832374394065),
                    new PdfPoint(40.7654267847265, 320.51104165848284),
                    new PdfPoint(443.45264559634137, 22.11792681360786),
                    new PdfPoint(345.74164027022573, 484.01784165724456),
                    new PdfPoint(453.33094307272717, 441.7802101118389),
                    new PdfPoint(470.9897811308254, 63.67713677117809),
                    new PdfPoint(277.98105707671505, 321.72593673466497),
                    new PdfPoint(447.8012370058249, 358.25102431521026),
                    new PdfPoint(345.94253780510235, 111.25057954480089),
                },
                new PdfPoint[]
                {
                    new PdfPoint(647.6991334648283, 297.75247084308063),
                    new PdfPoint(443.4526455963414, 22.117926813607856),
                    new PdfPoint(40.7654267847265, 320.5110416584829),
                    new PdfPoint(245.0119146532134, 596.1455856879556),
                }
            }
        };

        public static IEnumerable<object[]> Issue458Data => new[]
        {
            new object[]
            {
                new PdfPoint[]
                {
                    new PdfPoint(134.74199999999985, 1611.657),
                    new PdfPoint(277.58043749999985, 1611.657),
                    new PdfPoint(314.74199999999985, 1611.657),
                    new PdfPoint(507.0248828124999, 1611.657),
                    new PdfPoint(545.1419999999998, 1611.657),
                    new PdfPoint(632.9658281249997, 1611.657),
                    new PdfPoint(668.5709999999998, 1611.657),
                    new PdfPoint(831.3395078125002, 1611.657),
                    new PdfPoint(868.1139999999998, 1611.657),
                    new PdfPoint(892.8907187499999, 1611.657),
                    new PdfPoint(1010.0569999999999, 1611.6569999999997),
                    new PdfPoint(1046.2174003906248, 1611.7654812011715),
                    new PdfPoint(1010.0569999999999, 1611.657),
                    new PdfPoint(1046.2174003906248, 1611.7654812011717),
                    new PdfPoint(1085.145, 1611.8822639999996),
                    new PdfPoint(1255.484941406251, 1612.3932838242179),
                    new PdfPoint(1301.144, 1612.5302609999994),
                    new PdfPoint(1359.0006406250002, 1612.7038309218747),
                    new PdfPoint(1301.144, 1612.5302609999997),
                    new PdfPoint(1359.0006406250002, 1612.703830921875),
                    new PdfPoint(1400.915, 1612.8295739999996),
                    new PdfPoint(1505.379062499999, 1613.1429661874993),
                    new PdfPoint(1400.915, 1612.8295739999999),
                    new PdfPoint(1505.379062499999, 1613.1429661874995),
                    new PdfPoint(1543.886, 1613.2584869999996),
                    new PdfPoint(1600.9401015625003, 1613.4296493046872),
                    new PdfPoint(1641.597, 1613.5516199999997),
                    new PdfPoint(1764.5577421874998, 1613.9205022265612),
                    new PdfPoint(1641.597, 1613.55162),
                    new PdfPoint(1764.5577421874998, 1613.9205022265614)
                },
                new PdfPoint[]
                {
                    new PdfPoint(134.74199999999985, 1611.657),
                    new PdfPoint(1010.0569999999999, 1611.6569999999997),
                    new PdfPoint(1764.5577421874998, 1613.9205022265614),
                }
            }
        };
        #endregion

        [Fact]
        public void OriginIsZero()
        {
            var origin = PdfPoint.Origin;

            Assert.Equal(0, origin.X);
            Assert.Equal(0, origin.Y);
        }

        [Fact]
        public void IntsSetValue()
        {
            var origin = new PdfPoint(256, 372);

            Assert.Equal(256, origin.X);
            Assert.Equal(372, origin.Y);
        }

        [Fact]
        public void DoublesSetValue()
        {
            var origin = new PdfPoint(0.534436, 0.32552);

            Assert.Equal(0.534436, origin.X);
            Assert.Equal(0.32552, origin.Y);
        }

        [Theory]
        [MemberData(nameof(GrahamScanData))]
        public void GrahamScan(PdfPoint[] points, PdfPoint[] expected)
        {
            expected = expected.OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();
            var convexHull = GeometryExtensions.GrahamScan(points).OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], convexHull[i], PointComparer);
            }
        }

        [Theory]
        [MemberData(nameof(MinimumAreaRectangleData))]
        public void MinimumAreaRectangle(PdfPoint[] points, PdfPoint[] expected)
        {
            expected = expected.OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();

            var marRectangle = GeometryExtensions.MinimumAreaRectangle(points);
            var mar = new[] { marRectangle.BottomLeft, marRectangle.BottomRight, marRectangle.TopLeft, marRectangle.TopRight }.OrderBy(p => p.X).ThenBy(p => p.Y).ToArray();

            for (var i = 0; i < expected.Length; i++)
            {
                Assert.Equal(expected[i], mar[i], PointComparer);
            }
        }

        [Theory]
        [MemberData(nameof(Issue458Data))]
        public void Issue458(PdfPoint[] points, PdfPoint[] expected)
        {
            /*
             * https://github.com/UglyToad/PdfPig/issues/458
             * An unhandled exception of type 'System.ArgumentOutOfRangeException' occurred in System.Linq.dll: 'Specified argument was out of the range of valid values.'
             *  at System.Linq.ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument argument)
             *  at System.Linq.Enumerable.ElementAt[TSource](IEnumerable`1 source, Int32 index)
             */

            var result = GeometryExtensions.GrahamScan(points);

            // Data is noisy so we just check it does not throw an exception
            // and that key points are present (other points might be present,
            // e.g. 'not enough equal dupplicates' or 'not collinear enough' points)
            foreach (var point in expected)
            {
                Assert.Contains(point, result);
            }
        }

        [Theory]
        [MemberData(nameof(Issue458Data))]
        public void Issue458_Inv(PdfPoint[] points, PdfPoint[] expected)
        {
            /*
             * https://github.com/UglyToad/PdfPig/issues/458
             * An unhandled exception of type 'System.ArgumentOutOfRangeException' occurred in System.Linq.dll: 'Specified argument was out of the range of valid values.'
             *  at System.Linq.ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument argument)
             *  at System.Linq.Enumerable.ElementAt[TSource](IEnumerable`1 source, Int32 index)
             */
            var pointsInv = points.Select(p => new PdfPoint(p.Y, p.X)).ToArray();
            var expectedInv = expected.Select(p => new PdfPoint(p.Y, p.X)).ToArray();

            var result = GeometryExtensions.GrahamScan(pointsInv);

            // Data is noisy so we just check it does not throw an exception
            // and that key points are present (other points might be present,
            // e.g. 'not enough equal dupplicates' or 'not collinear enough' points)
            foreach (var point in expectedInv)
            {
                Assert.Contains(point, result);
            }
        }
    }
}
