namespace UglyToad.PdfPig.Tests.Dla
{
    using UglyToad.PdfPig.Core;
    using UglyToad.PdfPig.DocumentLayoutAnalysis;

    public class KdTreeTests
    {
        private static readonly DoubleComparer DoubleComparer = new DoubleComparer(3);
        private static readonly DoubleComparer PreciseDoubleComparer = new DoubleComparer(6);
        private static readonly PointComparer PointComparer = new PointComparer(DoubleComparer);

        #region data
        public static PdfPoint[] Tree1 = new PdfPoint[]
        {
            new PdfPoint(51, 75),
            new PdfPoint(25, 40),
            new PdfPoint(10, 30),
            new PdfPoint(1, 10),
            new PdfPoint(35, 90),
            new PdfPoint(50, 50),
            new PdfPoint(70, 70),
            new PdfPoint(55, 1),
            new PdfPoint(60, 80)
        };

        public static IEnumerable<object[]> DataTree1 => new[]
        {
            new object[]
            {
                new PdfPoint(51, 49),
                new object[]
                {
                    1.4142135623730951,
                    5,
                    new PdfPoint(50, 50)
                }
            },
            new object[]
            {
                new PdfPoint(28.189524796700038, 75.60283789175995),
                new object[]
                {
                    15.926733791512522,
                    4,
                    new PdfPoint(35, 90),
                }
            },
            new object[]
            {
                new PdfPoint(43.26688589899484, 8.035369191312736),
                new object[]
                {
                    13.680730468994646,
                    7,
                    new PdfPoint(55, 1),
                }
            },
            new object[]
            {
                new PdfPoint(82.22662843535518, 70.12992266643707),
                new object[]
                {
                    12.227318708346896,
                    6,
                    new PdfPoint(70, 70),
                }
            },
            new object[]
            {
                new PdfPoint(76.29751404813953, 74.63310544916789),
                new object[]
                {
                    7.8182062705983855,
                    6,
                    new PdfPoint(70, 70),
                }
            },
            new object[]
            {
                new PdfPoint(50.895502833937776, 61.76937358125091),
                new object[]
                {
                    11.80339272500231,
                    5,
                    new PdfPoint(50, 50),
                }
            },
            new object[]
            {
                new PdfPoint(42.9552821543992, 74.01081889822333),
                new object[]
                {
                    8.105304711572543,
                    0,
                    new PdfPoint(51, 75),
                }
            },
            new object[]
            {
                new PdfPoint(55.51285821663918, 76.33782834155515),
                new object[]
                {
                    4.706981405843449,
                    0,
                    new PdfPoint(51, 75),
                }
            },
            new object[]
            {
                new PdfPoint(4.3199936890310315, 98.54112120917016),
                new object[]
                {
                    31.846719434673833,
                    4,
                    new PdfPoint(35, 90),
                }
            },
            new object[]
            {
                new PdfPoint(21.569550153382444, 37.56786718125442),
                new object[]
                {
                    4.205146394381262,
                    1,
                    new PdfPoint(25, 40),
                }
            },
            new object[]
            {
                new PdfPoint(95.70493339772732, 65.77875848107642),
                new object[]
                {
                    26.04923186857304,
                    6,
                    new PdfPoint(70, 70),
                }
            },
            new object[]
            {
                new PdfPoint(87.23320341806003, 56.082576505219414),
                new object[]
                {
                    22.151252262147768,
                    6,
                    new PdfPoint(70, 70),
                }
            },
            new object[]
            {
                new PdfPoint(95.64105363103229, 87.41037179209023),
                new object[]
                {
                    30.99330052202065,
                    6,
                    new PdfPoint(70, 70),
                }
            },
            new object[]
            {
                new PdfPoint(31.9581372373153, 85.00443296498887),
                new object[]
                {
                    5.848813475252715,
                    4,
                    new PdfPoint(35, 90),
                }
            },
            new object[]
            {
                new PdfPoint(36.17227123238111, 79.38086715887763),
                new object[]
                {
                    10.68364180135556,
                    4,
                    new PdfPoint(35, 90),
                }
            },
            new object[]
            {
                new PdfPoint(97.45057198438961, 0.1038719192212767),
                new object[]
                {
                    42.46002952588474,
                    7,
                    new PdfPoint(55, 1),
                }
            },
            new object[]
            {
                new PdfPoint(49.936342420193036, 26.674269408896535),
                new object[]
                {
                    23.3258174539759,
                    5,
                    new PdfPoint(50, 50),
                }
            },
            new object[]
            {
                new PdfPoint(96.58603550736572, 5.478651527669465),
                new object[]
                {
                    41.826506771737215,
                    7,
                    new PdfPoint(55, 1),
                }
            },
            new object[]
            {
                new PdfPoint(33.82105876943279, 99.91438503275373),
                new object[]
                {
                    9.984234222153567,
                    4,
                    new PdfPoint(35, 90),
                }
            },
            new object[]
            {
                new PdfPoint(40.95742737577155, 81.48724148079593),
                new object[]
                {
                    10.390283852901893,
                    4,
                    new PdfPoint(35, 90),
                }
            },
            new object[]
            {
                new PdfPoint(65.32548187684739, 91.29488761267328),
                new object[]
                {
                    12.487403389157823,
                    8,
                    new PdfPoint(60, 80),
                }
            },
            new object[]
            {
                new PdfPoint(95.72487295050016, 17.00011169070058),
                new object[]
                {
                    43.75521512859093,
                    7,
                    new PdfPoint(55, 1),
                }
            },
            new object[]
            {
                new PdfPoint(76.84590618203656, 90.69206464298559),
                new object[]
                {
                    19.952563780721515,
                    8,
                    new PdfPoint(60, 80),
                }
            },
            new object[]
            {
                new PdfPoint(27.727148118788257, 46.59561305641292),
                new object[]
                {
                    7.137187713079637,
                    1,
                    new PdfPoint(25, 40),
                }
            },
            new object[]
            {
                new PdfPoint(51.92233547390256, 48.53466193663921),
                new object[]
                {
                    2.4171448682605097,
                    5,
                    new PdfPoint(50, 50),
                }
            },
            new object[]
            {
                new PdfPoint(74.85516050981272, 96.58650922810423),
                new object[]
                {
                    22.26629924676048,
                    8,
                    new PdfPoint(60, 80),
                }
            },
            new object[]
            {
                new PdfPoint(83.16610264969573, 97.29274249477987),
                new object[]
                {
                    28.908601747006117,
                    8,
                    new PdfPoint(60, 80),
                }
            },
            new object[]
            {
                new PdfPoint(71.98380721011306, 96.58511553469276),
                new object[]
                {
                    20.46161510116601,
                    8,
                    new PdfPoint(60, 80),
                }
            },
            new object[]
            {
                new PdfPoint(98.51219383024967, 32.70778133798633),
                new object[]
                {
                    46.943101407439705,
                    6,
                    new PdfPoint(70, 70),
                }
            },
            new object[]
            {
                new PdfPoint(8.09276417067899, 51.21666984962398),
                new object[]
                {
                    20.28961078738919,
                    1,
                    new PdfPoint(25, 40),
                }
            },
            new object[]
            {
                new PdfPoint(61.41570675706768, 78.62680876706933),
                new object[]
                {
                    1.9722778161822772,
                    8,
                    new PdfPoint(60, 80),
                }
            },
            new object[]
            {
                new PdfPoint(54.00356297008202, 36.88821236457238),
                new object[]
                {
                    13.709394277354654,
                    5,
                    new PdfPoint(50, 50),
                }
            },
            new object[]
            {
                new PdfPoint(21.85770655063177, 16.016459205926083),
                new object[]
                {
                    18.334247128814017,
                    2,
                    new PdfPoint(10, 30),
                }
            },
            new object[]
            {
                new PdfPoint(27.683193240146863, 55.85996630126125),
                new object[]
                {
                    16.085336708975426,
                    1,
                    new PdfPoint(25, 40),
                }
            },
            new object[]
            {
                new PdfPoint(65.54133757456142, 92.08898521546233),
                new object[]
                {
                    13.298495616230923,
                    8,
                    new PdfPoint(60, 80),
                }
            },
            new object[]
            {
                new PdfPoint(24.861338264089227, 20.46892423497547),
                new object[]
                {
                    17.65504970931321,
                    2,
                    new PdfPoint(10, 30),
                }
            },
            new object[]
            {
                new PdfPoint(54.88661497017837, 4.925311272555266),
                new object[]
                {
                    3.92694852925743,
                    7,
                    new PdfPoint(55, 1),
                }
            },
            new object[]
            {
                new PdfPoint(27.79457376217088, 54.31806138769485),
                new object[]
                {
                    14.58823239511943,
                    1,
                    new PdfPoint(25, 40),
                }
            },
            new object[]
            {
                new PdfPoint(0.7501079718312487, 80.59920809402051),
                new object[]
                {
                    35.51661572279581,
                    4,
                    new PdfPoint(35, 90),
                }
            },
            new object[]
            {
                new PdfPoint(89.56921087453362, 16.96508157736404),
                new object[]
                {
                    38.07773851294037,
                    7,
                    new PdfPoint(55, 1),
                }
            },
            new object[]
            {
                new PdfPoint(5.3978925878381485, 59.8520804896729),
                new object[]
                {
                    27.898883754845496,
                    1,
                    new PdfPoint(25, 40),
                }
            },
            new object[]
            {
                new PdfPoint(25.97829935047318, 1.9752285215369203),
                new object[]
                {
                    26.23570840902535,
                    3,
                    new PdfPoint(1, 10),
                }
            },
            new object[]
            {
                new PdfPoint(62.572023839684796, 39.52157530571068),
                new object[]
                {
                    16.366220318066574,
                    5,
                    new PdfPoint(50, 50),
                }
            },
            new object[]
            {
                new PdfPoint(81.17046822810447, 92.91856347287415),
                new object[]
                {
                    24.800766262352845,
                    8,
                    new PdfPoint(60, 80),
                }
            },
            new object[]
            {
                new PdfPoint(72.71721419976534, 53.943166291078306),
                new object[]
                {
                    16.285120870394863,
                    6,
                    new PdfPoint(70, 70),
                }
            },
            new object[]
            {
                new PdfPoint(77.0888492617333, 55.48826101878681),
                new object[]
                {
                    16.150614604851395,
                    6,
                    new PdfPoint(70, 70),
                }
            },
            new object[]
            {
                new PdfPoint(58.21541870080057, 11.40938709274073),
                new object[]
                {
                    10.894689397498917,
                    7,
                    new PdfPoint(55, 1),
                }
            },
            new object[]
            {
                new PdfPoint(92.47344710876337, 10.739546752843566),
                new object[]
                {
                    38.718445335061055,
                    7,
                    new PdfPoint(55, 1),
                }
            },
            new object[]
            {
                new PdfPoint(98.99910001507719, 94.33068443488959),
                new object[]
                {
                    37.854061958456036,
                    6,
                    new PdfPoint(70, 70),
                }
            },
            new object[]
            {
                new PdfPoint(0.29975539175036703, 35.105628826864496),
                new object[]
                {
                    10.961851630887265,
                    2,
                    new PdfPoint(10, 30),
                }
            },
            new object[]
            {
                new PdfPoint(69.86806543596909, 86.99952741106358),
                new object[]
                {
                    12.09843375924331,
                    8,
                    new PdfPoint(60, 80),
                }
            }
        };

        public static IEnumerable<object[]> DataTreeK1 => new[]
        {
            new object[]
            {
                new PdfPoint(57.28490719962775, 71.58006710449683),
                new object[]
                {
                    new object[]
                    {
                        7.155137980338146,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        8.846863787773017,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        12.812891827249272,
                        6,
                        new PdfPoint(70, 70),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(57.6366179124611, 99.23965351255829),
                new object[]
                {
                    new object[]
                    {
                        19.38426790402455,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        24.449696675968916,
                        4,
                        new PdfPoint(35, 90),
                    },
                    new object[]
                    {
                        25.13176276596767,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(73.90499795029336, 90.88238489014321),
                new object[]
                {
                    new object[]
                    {
                        17.657159139988515,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        21.24436413950479,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        27.87273005827726,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(23.565146028832896, 75.26741657423868),
                new object[]
                {
                    new object[]
                    {
                        18.64952813716565,
                        4,
                        new PdfPoint(35, 90),
                    },
                    new object[]
                    {
                        27.43615723900563,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        35.29659300470031,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(26.939537622638365, 12.273123896672733),
                new object[]
                {
                    new object[]
                    {
                        24.51917762184319,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        26.038945914262655,
                        3,
                        new PdfPoint(1, 10),
                    },
                    new object[]
                    {
                        27.79463014035068,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(83.86699657545552, 96.41621467187889),
                new object[]
                {
                    new object[]
                    {
                        28.967665243958084,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        29.83471118704662,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        39.228735829274505,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(66.43635368261755, 7.7006684896940625),
                new object[]
                {
                    new object[]
                    {
                        13.254778148377245,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        45.380471224953766,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        52.537779002576954,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(37.3870248673188, 57.9719767606922),
                new object[]
                {
                    new object[]
                    {
                        14.921111056843282,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        21.800611629066815,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        21.827284158832835,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(28.21563125325024, 51.82289295570881),
                new object[]
                {
                    new object[]
                    {
                        12.252390876846393,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        21.860504578402132,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        28.42618298172852,
                        2,
                        new PdfPoint(10, 30),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(40.64586336533863, 54.586928359658906),
                new object[]
                {
                    new object[]
                    {
                        10.418242843999996,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        21.39092142514358,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        22.888897728875776,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(22.971155983334967, 41.71022847164334),
                new object[]
                {
                    new object[]
                    {
                        2.6535051289147757,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        17.475134860769824,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        28.271517838092137,
                        5,
                        new PdfPoint(50, 50),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(46.16350096686003, 0.026010364849260448),
                new object[]
                {
                    new object[]
                    {
                        8.890015240260544,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        45.230671236732974,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        46.25172741450487,
                        3,
                        new PdfPoint(1, 10),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(30.810311156595372, 57.2429846701653),
                new object[]
                {
                    new object[]
                    {
                        18.195610351730775,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        20.51109418921715,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        26.88745300353832,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(8.49831703999272, 7.098709173582418),
                new object[]
                {
                    new object[]
                    {
                        8.040040229482686,
                        3,
                        new PdfPoint(1, 10),
                    },
                    new object[]
                    {
                        22.95047217877084,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        36.80761441002533,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(45.59952054686074, 21.5904374552803),
                new object[]
                {
                    new object[]
                    {
                        22.634821151241802,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        27.62702010439208,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        28.74834714205047,
                        5,
                        new PdfPoint(50, 50),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(75.87075134690805, 58.59787314401566),
                new object[]
                {
                    new object[]
                    {
                        12.824750220459736,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        26.644545075400142,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        27.262046839042196,
                        5,
                        new PdfPoint(50, 50),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(8.963229075823397, 94.18880408311563),
                new object[]
                {
                    new object[]
                    {
                        26.37156650267053,
                        4,
                        new PdfPoint(35, 90),
                    },
                    new object[]
                    {
                        46.20930979653228,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        52.972390428183594,
                        8,
                        new PdfPoint(60, 80),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(57.181014731701026, 9.603539673394279),
                new object[]
                {
                    new object[]
                    {
                        8.875681391958942,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        41.02975724393135,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        44.26694601560936,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(54.645113285069115, 27.785970730398834),
                new object[]
                {
                    new object[]
                    {
                        22.694496553610147,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        26.788321570233126,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        32.062676941940694,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(11.149446927742158, 88.54384340073094),
                new object[]
                {
                    new object[]
                    {
                        23.89496335829337,
                        4,
                        new PdfPoint(35, 90),
                    },
                    new object[]
                    {
                        42.089218028235706,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        49.59207391833591,
                        8,
                        new PdfPoint(60, 80),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(99.50678603018885, 74.5426364401791),
                new object[]
                {
                    new object[]
                    {
                        29.854412867430348,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        39.881937759582165,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        48.50894219011971,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(24.209348883557468, 41.95277654538159),
                new object[]
                {
                    new object[]
                    {
                        2.106766580360595,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        18.568103372140087,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        27.016948205499066,
                        5,
                        new PdfPoint(50, 50),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(71.52714336370258, 2.423670062161676),
                new object[]
                {
                    new object[]
                    {
                        16.58834844733717,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        52.21996813246304,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        59.805983322605925,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(64.29355652488779, 17.6151418970531),
                new object[]
                {
                    new object[]
                    {
                        19.037676673914117,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        35.39893773092871,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        45.222399943652036,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(2.3569761133599876, 34.0132529249903),
                new object[]
                {
                    new object[]
                    {
                        8.632613345429817,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        23.42109457884254,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        24.051563363153438,
                        3,
                        new PdfPoint(1, 10),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(47.10705238689328, 14.985277112050543),
                new object[]
                {
                    new object[]
                    {
                        16.058847963789056,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        33.383500810995635,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        35.134028587852995,
                        5,
                        new PdfPoint(50, 50),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(73.44870263360212, 44.499915917034315),
                new object[]
                {
                    new object[]
                    {
                        24.08511117098677,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        25.73223344549272,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        37.87082490519413,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(37.15926087979008, 21.563350867442598),
                new object[]
                {
                    new object[]
                    {
                        22.08523616309826,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        27.223948487553027,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        28.439453231776056,
                        2,
                        new PdfPoint(10, 30),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(19.784956200369862, 49.74041414042939),
                new object[]
                {
                    new object[]
                    {
                        11.048635637902876,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        22.032460558884953,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        30.216158866276444,
                        5,
                        new PdfPoint(50, 50),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(91.46918827031537, 10.458352581834374),
                new object[]
                {
                    new object[]
                    {
                        37.675749848649346,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        57.29952404986787,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        63.29402675020287,
                        6,
                        new PdfPoint(70, 70),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(19.900141743444067, 33.68929824559639),
                new object[]
                {
                    new object[]
                    {
                        8.113785236866608,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        10.565213111208138,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        30.305085535122533,
                        3,
                        new PdfPoint(1, 10),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(99.56220935020619, 63.18918949351682),
                new object[]
                {
                    new object[]
                    {
                        30.3366339830351,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        42.985715750170165,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        49.97782930253481,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(0.9575689831946121, 56.0195244568354),
                new object[]
                {
                    new object[]
                    {
                        27.545983584790353,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        28.890546083814222,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        46.01954401799804,
                        3,
                        new PdfPoint(1, 10),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(13.763956851598868, 20.74365587257857),
                new object[]
                {
                    new object[]
                    {
                        9.992360971559586,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        16.68366674379076,
                        3,
                        new PdfPoint(1, 10),
                    },
                    new object[]
                    {
                        22.294740518481255,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(89.66047561920547, 70.74342301744882),
                new object[]
                {
                    new object[]
                    {
                        19.67452615328373,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        31.071337779236003,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        38.894097530493816,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(51.22041417582528, 90.11643810630143),
                new object[]
                {
                    new object[]
                    {
                        13.39490377728326,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        15.118044960594169,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        16.220832095423244,
                        4,
                        new PdfPoint(35, 90),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(13.94537026257473, 3.2534322160761575),
                new object[]
                {
                    new object[]
                    {
                        14.597903551477287,
                        3,
                        new PdfPoint(1, 10),
                    },
                    new object[]
                    {
                        27.035991469314418,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        38.37336423262986,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(35.907000931452906, 67.29871186609165),
                new object[]
                {
                    new object[]
                    {
                        16.94427513364443,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        22.31273302336873,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        22.719399939883633,
                        4,
                        new PdfPoint(35, 90),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(53.72385478058551, 34.1117586202502),
                new object[]
                {
                    new object[]
                    {
                        16.318802301887334,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        29.321173578190265,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        33.13634116112924,
                        7,
                        new PdfPoint(55, 1),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(56.01158219636303, 50.713431740553794),
                new object[]
                {
                    new object[]
                    {
                        6.053767864070984,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        23.825355146888903,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        24.79825304193105,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(85.73666685032894, 4.105238234975095),
                new object[]
                {
                    new object[]
                    {
                        30.89312534471159,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        58.167332026144976,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        67.74778455143482,
                        6,
                        new PdfPoint(70, 70),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(70.07227672522, 85.08222783289746),
                new object[]
                {
                    new object[]
                    {
                        11.281834876246238,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        15.08240101338097,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        21.573202301879537,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(1.845180225020826, 95.15703279645201),
                new object[]
                {
                    new object[]
                    {
                        33.55349551946908,
                        4,
                        new PdfPoint(35, 90),
                    },
                    new object[]
                    {
                        53.12722727818502,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        59.82009650377246,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(77.65420415473436, 0.6457892186903513),
                new object[]
                {
                    new object[]
                    {
                        22.656973124448452,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        56.573784823694346,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        65.73598041703032,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(6.219304325291253, 47.10473454603812),
                new object[]
                {
                    new object[]
                    {
                        17.517579846405475,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        20.0796360274705,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        37.47002086164296,
                        3,
                        new PdfPoint(1, 10),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(27.466544481255152, 5.23970743977279),
                new object[]
                {
                    new object[]
                    {
                        26.891232066181203,
                        3,
                        new PdfPoint(1, 10),
                    },
                    new object[]
                    {
                        27.857966400610902,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        30.301027437757085,
                        2,
                        new PdfPoint(10, 30),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(40.721106407631716, 91.17894863211862),
                new object[]
                {
                    new object[]
                    {
                        5.841316495843984,
                        4,
                        new PdfPoint(35, 90),
                    },
                    new object[]
                    {
                        19.168047170329135,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        22.285525137752657,
                        8,
                        new PdfPoint(60, 80),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(93.71967627569863, 69.92495107460142),
                new object[]
                {
                    new object[]
                    {
                        23.71979500259528,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        35.1926580267403,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        43.02007511262245,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(34.80623433831922, 74.52400508825515),
                new object[]
                {
                    new object[]
                    {
                        15.477207876099586,
                        4,
                        new PdfPoint(35, 90),
                    },
                    new object[]
                    {
                        16.200759780375687,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        25.78201598962232,
                        8,
                        new PdfPoint(60, 80),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(54.410107977828574, 53.02534528663537),
                new object[]
                {
                    new object[]
                    {
                        5.348061936764951,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        22.23767717618116,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        23.04742145882954,
                        6,
                        new PdfPoint(70, 70),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(75.35946634265616, 43.898478778156736),
                new object[]
                {
                    new object[]
                    {
                        26.083157293642856,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        26.646074562163907,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        39.23306055946167,
                        8,
                        new PdfPoint(60, 80),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(28.68967491589185, 76.26518874260796),
                new object[]
                {
                    new object[]
                    {
                        15.115066752856489,
                        4,
                        new PdfPoint(35, 90),
                    },
                    new object[]
                    {
                        22.346169871210755,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        31.53228935553006,
                        8,
                        new PdfPoint(60, 80),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(23.173766410901152, 5.938703009992585),
                new object[]
                {
                    new object[]
                    {
                        22.54262739980084,
                        3,
                        new PdfPoint(1, 10),
                    },
                    new object[]
                    {
                        27.43162653380815,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        32.20714100768327,
                        7,
                        new PdfPoint(55, 1),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(45.15121045936782, 84.92310113698109),
                new object[]
                {
                    new object[]
                    {
                        11.3499769099193,
                        4,
                        new PdfPoint(35, 90),
                    },
                    new object[]
                    {
                        11.518518796501736,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        15.64364010155348,
                        8,
                        new PdfPoint(60, 80),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(79.34435328880231, 64.36582210296166),
                new object[]
                {
                    new object[]
                    {
                        10.91150305693152,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        24.872304329881437,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        30.27355451390367,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(32.71323906831667, 16.467670083297026),
                new object[]
                {
                    new object[]
                    {
                        24.764179942682542,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        26.43889524827013,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        27.128371322873924,
                        7,
                        new PdfPoint(55, 1),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(7.574147517126595, 2.618810847471864),
                new object[]
                {
                    new object[]
                    {
                        9.884400279346279,
                        3,
                        new PdfPoint(1, 10),
                    },
                    new object[]
                    {
                        27.488439018525362,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        41.24334658113903,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(20.43354407406556, 85.31844765600553),
                new object[]
                {
                    new object[]
                    {
                        15.300280082134142,
                        4,
                        new PdfPoint(35, 90),
                    },
                    new object[]
                    {
                        32.261100258698846,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        39.922303540860256,
                        8,
                        new PdfPoint(60, 80),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(22.34600206444981, 74.86048890604329),
                new object[]
                {
                    new object[]
                    {
                        19.731407955768056,
                        4,
                        new PdfPoint(35, 90),
                    },
                    new object[]
                    {
                        28.654337560583244,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        34.96136999332652,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(63.55251564207386, 87.87501150557617),
                new object[]
                {
                    new object[]
                    {
                        8.639222974326827,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        17.981422919591974,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        19.002265414160036,
                        6,
                        new PdfPoint(70, 70),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(66.56676934427253, 22.618060969910626),
                new object[]
                {
                    new object[]
                    {
                        24.51796714987554,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        32.003569043996634,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        45.05472359437686,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(37.37365602876168, 82.29816768422216),
                new object[]
                {
                    new object[]
                    {
                        8.059309149253208,
                        4,
                        new PdfPoint(35, 90),
                    },
                    new object[]
                    {
                        15.457700397197755,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        22.74275744516473,
                        8,
                        new PdfPoint(60, 80),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(49.32206363274456, 34.6288799996447),
                new object[]
                {
                    new object[]
                    {
                        15.386062777181502,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        24.908065147929335,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        34.104846157417356,
                        7,
                        new PdfPoint(55, 1),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(47.912171617799714, 26.464204062460027),
                new object[]
                {
                    new object[]
                    {
                        23.628218675284096,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        26.432234103649463,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        26.611752665058166,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(62.98919203656388, 8.505042849821665),
                new object[]
                {
                    new object[]
                    {
                        10.961425891495823,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        43.48046203362921,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        49.34684425049385,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(22.813112421361513, 91.33114676735282),
                new object[]
                {
                    new object[]
                    {
                        12.259371132754197,
                        4,
                        new PdfPoint(35, 90),
                    },
                    new object[]
                    {
                        32.576172060382156,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        38.874921155541756,
                        8,
                        new PdfPoint(60, 80),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(83.79195528008587, 61.61229643727948),
                new object[]
                {
                    new object[]
                    {
                        16.142230375755485,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        30.06933285525455,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        35.41952763341757,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(40.57648596422233, 88.72604856926107),
                new object[]
                {
                    new object[]
                    {
                        5.720152791407797,
                        4,
                        new PdfPoint(35, 90),
                    },
                    new object[]
                    {
                        17.23525613908212,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        21.293586384898983,
                        8,
                        new PdfPoint(60, 80),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(23.963796491024425, 39.78301198290163),
                new object[]
                {
                    new object[]
                    {
                        1.0586791353274034,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        17.049778177452716,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        27.969103262391588,
                        5,
                        new PdfPoint(50, 50),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(66.61803420364912, 81.92497907966498),
                new object[]
                {
                    new object[]
                    {
                        6.892308842312381,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        12.395274046915407,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        17.084446951544887,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(99.22597668831827, 95.16802163222447),
                new object[]
                {
                    new object[]
                    {
                        38.569249749849185,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        42.056463562550256,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        52.27326203806389,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(54.91457415134011, 20.76613744667287),
                new object[]
                {
                    new object[]
                    {
                        19.766322043728387,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        29.64408471981961,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        35.56435315560098,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(37.99829204035451, 49.05561293919847),
                new object[]
                {
                    new object[]
                    {
                        12.038806455343792,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        15.84170829396003,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        29.019917812230187,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(44.13914280799433, 96.26313880094763),
                new object[]
                {
                    new object[]
                    {
                        11.07929776226139,
                        4,
                        new PdfPoint(35, 90),
                    },
                    new object[]
                    {
                        22.34261473233293,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        22.716876425338192,
                        8,
                        new PdfPoint(60, 80),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(68.45663600770705, 53.9590240469649),
                new object[]
                {
                    new object[]
                    {
                        16.115051409739802,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        18.876474356336637,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        27.339656357785035,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(12.707495686246627, 54.33094645334382),
                new object[]
                {
                    new object[]
                    {
                        18.880722670286037,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        24.4811251417603,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        37.54314817876952,
                        5,
                        new PdfPoint(50, 50),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(58.52082047274562, 57.34835769135233),
                new object[]
                {
                    new object[]
                    {
                        11.25178840401906,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        17.08319688246026,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        19.18705857539686,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(99.15392575500378, 16.024553135770216),
                new object[]
                {
                    new object[]
                    {
                        46.64017963631754,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        59.75315394816014,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        61.34574354526617,
                        6,
                        new PdfPoint(70, 70),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(9.14386712267613, 18.48796945342841),
                new object[]
                {
                    new object[]
                    {
                        11.543821326096149,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        11.76300119672437,
                        3,
                        new PdfPoint(1, 10),
                    },
                    new object[]
                    {
                        26.724228858097668,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(20.364409084676716, 42.516138522943194),
                new object[]
                {
                    new object[]
                    {
                        5.274434206705637,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        16.250375355665845,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        30.56593580291902,
                        5,
                        new PdfPoint(50, 50),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(71.41826096965676, 51.52054502261746),
                new object[]
                {
                    new object[]
                    {
                        18.533799406467093,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        21.472167103721244,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        30.68315562943202,
                        8,
                        new PdfPoint(60, 80),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(66.76607506217822, 41.47325322334792),
                new object[]
                {
                    new object[]
                    {
                        18.809749694872096,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        28.7094679881515,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        37.048776933820484,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(75.22106099331124, 78.49278018746351),
                new object[]
                {
                    new object[]
                    {
                        9.96929251293435,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        15.295502911817039,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        24.471602094665585,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(20.23368754320779, 18.77886105160098),
                new object[]
                {
                    new object[]
                    {
                        15.186912788031798,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        21.142448719887277,
                        3,
                        new PdfPoint(1, 10),
                    },
                    new object[]
                    {
                        21.749815463654638,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(26.22566842248103, 2.1646330467549713),
                new object[]
                {
                    new object[]
                    {
                        26.414528628256093,
                        3,
                        new PdfPoint(1, 10),
                    },
                    new object[]
                    {
                        28.79789103157728,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        32.21924842664868,
                        2,
                        new PdfPoint(10, 30),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(73.433313570979, 20.353343954336044),
                new object[]
                {
                    new object[]
                    {
                        26.727120522437016,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        37.78947471989696,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        49.765229815536166,
                        6,
                        new PdfPoint(70, 70),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(56.567534506738085, 29.559840241162306),
                new object[]
                {
                    new object[]
                    {
                        21.46934187309903,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        28.602825717584764,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        33.24915293092673,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(58.883110628279766, 29.544477814049795),
                new object[]
                {
                    new object[]
                    {
                        22.301077156365295,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        28.807390750087734,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        35.45959855989831,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(10.578350777330447, 76.09626135135599),
                new object[]
                {
                    new object[]
                    {
                        28.102151148353634,
                        4,
                        new PdfPoint(35, 90),
                    },
                    new object[]
                    {
                        38.87060650217747,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        40.43651214967753,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(52.351579782397785, 29.629467324272664),
                new object[]
                {
                    new object[]
                    {
                        20.505816954363393,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        28.75170480024794,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        29.251613025117088,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(5.635680496099216, 31.218456664234452),
                new object[]
                {
                    new object[]
                    {
                        4.531216323984779,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        21.262463949577448,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        21.718941891213532,
                        3,
                        new PdfPoint(1, 10),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(70.54093686797465, 63.05914799333128),
                new object[]
                {
                    new object[]
                    {
                        6.961899114006998,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        19.952539105749896,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        22.900483844743007,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(16.722031960407623, 61.0191629618462),
                new object[]
                {
                    new object[]
                    {
                        22.59048398067558,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        31.739158535322147,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        34.263289846253635,
                        4,
                        new PdfPoint(35, 90),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(2.44084138208438, 6.372760538234356),
                new object[]
                {
                    new object[]
                    {
                        3.902933512284925,
                        3,
                        new PdfPoint(1, 10),
                    },
                    new object[]
                    {
                        24.807001503495414,
                        2,
                        new PdfPoint(10, 30),
                    },
                    new object[]
                    {
                        40.49329415307187,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(97.04197763063725, 76.19402759440892),
                new object[]
                {
                    new object[]
                    {
                        27.742287793478468,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        37.236991456624835,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        46.05745765927936,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(3.1014335951180305, 93.45996261954107),
                new object[]
                {
                    new object[]
                    {
                        32.08566471206863,
                        4,
                        new PdfPoint(35, 90),
                    },
                    new object[]
                    {
                        51.3326687749404,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        57.77122825309978,
                        1,
                        new PdfPoint(25, 40),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(94.23480523412653, 20.79954962662124),
                new object[]
                {
                    new object[]
                    {
                        43.94760638734355,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        53.00362531100362,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        54.84532889571684,
                        6,
                        new PdfPoint(70, 70),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(92.12116656208707, 68.7190578812228),
                new object[]
                {
                    new object[]
                    {
                        22.158222464341684,
                        6,
                        new PdfPoint(70, 70),
                    },
                    new object[]
                    {
                        34.04451492379561,
                        8,
                        new PdfPoint(60, 80),
                    },
                    new object[]
                    {
                        41.59808376988461,
                        0,
                        new PdfPoint(51, 75),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(37.35531423574498, 8.63839212418782),
                new object[]
                {
                    new object[]
                    {
                        19.227063477352917,
                        7,
                        new PdfPoint(55, 1),
                    },
                    new object[]
                    {
                        33.7076287866739,
                        1,
                        new PdfPoint(25, 40),
                    },
                    new object[]
                    {
                        34.7078018315236,
                        2,
                        new PdfPoint(10, 30),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(51.71954908303077, 51.41808119631073),
                new object[]
                {
                    new object[]
                    {
                        2.228856955545159,
                        5,
                        new PdfPoint(50, 50),
                    },
                    new object[]
                    {
                        23.592893958704682,
                        0,
                        new PdfPoint(51, 75),
                    },
                    new object[]
                    {
                        26.066503259060696,
                        6,
                        new PdfPoint(70, 70),
                    },
                }
            }
        };


        public static PdfPoint[] Tree2 = new PdfPoint[]
        {
            new PdfPoint(82.45353838109239, 62.415005093558115),
            new PdfPoint(16.445674398013832, 7.051331644986569),
            new PdfPoint(35.583597244228336, 27.46872033901967),
            new PdfPoint(63.8245554804822, 44.89509346225664),
            new PdfPoint(0.7742372570967326, 6.938583013317256),
            new PdfPoint(65.40181734760978, 22.29324720161768),
            new PdfPoint(97.13960454465617, 50.25370334786503),
            new PdfPoint(19.15551535520754, 45.53805856543256),
            new PdfPoint(80.59123120177551, 25.51233282470693),
            new PdfPoint(5.021252133213605, 56.04912906205743),
            new PdfPoint(24.41810676553939, 79.31739454102991),
            new PdfPoint(98.12630738875605, 12.483472098628233),
            new PdfPoint(82.48631571672695, 29.34364176375367),
            new PdfPoint(35.803655214742406, 5.16089154047169),
            new PdfPoint(1.813800375443253, 47.255096977735214),
            new PdfPoint(80.11998397458856, 25.99984035749021),
            new PdfPoint(90.26397977468233, 88.93339150438577),
            new PdfPoint(8.254965696884042, 8.97868520394496),
            new PdfPoint(14.158071542303697, 98.52077265281578),
            new PdfPoint(70.71899728005727, 9.70650341683359),
            new PdfPoint(58.60508497220939, 69.22169822597527),
            new PdfPoint(15.143510163282482, 66.19892308162831),
            new PdfPoint(16.49098129875618, 61.44894011847811),
            new PdfPoint(59.31519405414486, 64.30374393065894),
            new PdfPoint(58.880198617023936, 69.74930498275906),
            new PdfPoint(58.78888045620321, 4.76405876739463),
            new PdfPoint(11.228694290441743, 12.860145308234605),
            new PdfPoint(80.62371302859762, 81.02375037506395),
            new PdfPoint(90.87481745321885, 8.048659530077462),
            new PdfPoint(86.97060905691454, 53.23733886383501),
            new PdfPoint(79.77968787046065, 62.69132229661655),
            new PdfPoint(97.45369858932968, 33.575350634376086),
            new PdfPoint(55.68259729639997, 78.53266468780453),
            new PdfPoint(84.80993603153726, 40.80973096746926),
            new PdfPoint(52.89633110070122, 15.424096971651757),
            new PdfPoint(31.112041569584935, 55.44209995586027),
            new PdfPoint(49.53804959253269, 92.63065004232082),
            new PdfPoint(92.45607820110665, 72.66951564303699),
            new PdfPoint(24.636942156010523, 22.044633809864933),
            new PdfPoint(99.76982890678676, 52.90800974490838),
            new PdfPoint(82.61099784649278, 17.85771383958713),
            new PdfPoint(74.16588992664359, 81.00550169213861),
            new PdfPoint(13.34483441571791, 47.0726836958395),
            new PdfPoint(47.945049322341525, 9.252548969452246),
            new PdfPoint(82.78544384909999, 85.53384162961818),
            new PdfPoint(86.41791572882465, 84.5461317010868),
            new PdfPoint(67.53711275495073, 90.39020510040851),
            new PdfPoint(62.56803790975437, 46.05914229645705),
            new PdfPoint(71.56130305322326, 8.098723357173876),
            new PdfPoint(37.970724110053844, 36.503310666941424),
            new PdfPoint(18.3788032374675, 19.84795034896214),
            new PdfPoint(18.13308334914272, 1.2881803099952127),
            new PdfPoint(81.60160120456186, 59.00242318333139),
            new PdfPoint(37.57869607184197, 27.967241358959505),
            new PdfPoint(97.7460348272766, 5.634112362720456),
            new PdfPoint(84.9666796464414, 68.61319827025679),
            new PdfPoint(29.725739779656745, 38.277277199189065),
            new PdfPoint(15.448731011261907, 30.373061794712797),
            new PdfPoint(70.09436264721477, 86.01779473250726),
            new PdfPoint(71.14819197800337, 52.17424240997747),
            new PdfPoint(43.017566199578106, 57.09185921458071),
            new PdfPoint(33.433656443618766, 3.2699185921492235),
            new PdfPoint(2.4884293540186175, 29.514037079380717),
            new PdfPoint(72.74747856539602, 44.441442049411116),
            new PdfPoint(15.264247773398054, 0.8939481426395335),
            new PdfPoint(82.27200991589356, 79.74763514660499),
            new PdfPoint(34.43836768761709, 85.52303143204219),
            new PdfPoint(91.90691599722378, 96.20420265233045),
            new PdfPoint(10.747687004702266, 72.62390777764313),
            new PdfPoint(18.204503055467892, 62.00619128580146),
            new PdfPoint(91.00755813085259, 81.62926984298593),
            new PdfPoint(52.97863363490906, 68.26721122337617),
            new PdfPoint(84.97188896260866, 40.483497230772926),
            new PdfPoint(82.04208148460329, 71.99786553392418),
            new PdfPoint(75.03458936225277, 45.61844875850991),
            new PdfPoint(62.777729239709636, 93.54972672515291),
            new PdfPoint(70.36073331446983, 3.9743344551363857),
            new PdfPoint(84.64548329605812, 44.580018552016185),
            new PdfPoint(44.866927314279856, 7.669932380951572),
            new PdfPoint(90.03637785608008, 39.71900281528301),
            new PdfPoint(63.98092485758994, 24.420561596263646),
            new PdfPoint(11.201209707090698, 49.55375920684947),
            new PdfPoint(99.66377946738517, 22.268044551668787),
            new PdfPoint(57.19140901930086, 6.016576988006017),
            new PdfPoint(42.195134616764996, 62.54652068319705),
            new PdfPoint(49.61310425442538, 20.010647167526496),
            new PdfPoint(52.44257355334966, 84.19422038812934),
            new PdfPoint(67.0752294529232, 39.24937886844233),
            new PdfPoint(43.03278016511083, 4.6357991376672185),
            new PdfPoint(49.42598643546282, 18.91930873569896),
            new PdfPoint(93.45285655237589, 26.040139241642603),
            new PdfPoint(99.36796037285181, 90.9895050206579),
            new PdfPoint(69.38884149074713, 6.0366785105100185),
            new PdfPoint(99.69679458529599, 47.806517636673476),
            new PdfPoint(80.69475374791655, 21.854199922464968),
            new PdfPoint(89.04717161055198, 88.20721990563409),
            new PdfPoint(46.898436066324166, 25.173429288152636),
            new PdfPoint(8.420107657979937, 68.0080815210619),
            new PdfPoint(28.000497306010118, 81.20161949340638),
            new PdfPoint(24.103817108644833, 21.163820177769633)
        };

        public static IEnumerable<object[]> DataTree2 => new[]
        {
            new object[]
            {
                new PdfPoint(87.37822238977932, 47.13424664374255),
                new object[]
                {
                    3.7405807168027154,
                    77,
                    new PdfPoint(84.64548329605812, 44.580018552016185),
                }
            },
            new object[]
            {
                new PdfPoint(74.5026032549824, 69.93356102115371),
                new object[]
                {
                    7.816974165006036,
                    73,
                    new PdfPoint(82.04208148460329, 71.99786553392418),
                }
            },
            new object[]
            {
                new PdfPoint(54.01419184925133, 71.12439211149801),
                new object[]
                {
                    3.039056340830242,
                    71,
                    new PdfPoint(52.97863363490906, 68.26721122337617),
                }
            },
            new object[]
            {
                new PdfPoint(53.56967860131475, 54.19173302307295),
                new object[]
                {
                    10.943391067925589,
                    60,
                    new PdfPoint(43.017566199578106, 57.09185921458071),
                }
            },
            new object[]
            {
                new PdfPoint(33.62320720050764, 38.388295379543614),
                new object[]
                {
                    3.899048259891558,
                    56,
                    new PdfPoint(29.725739779656745, 38.277277199189065),
                }
            },
            new object[]
            {
                new PdfPoint(67.71643075209577, 98.50616915055454),
                new object[]
                {
                    6.996934624874407,
                    75,
                    new PdfPoint(62.777729239709636, 93.54972672515291),
                }
            },
            new object[]
            {
                new PdfPoint(62.853776192101186, 77.58809088626217),
                new object[]
                {
                    7.233120102743402,
                    32,
                    new PdfPoint(55.68259729639997, 78.53266468780453),
                }
            },
            new object[]
            {
                new PdfPoint(39.1710541173474, 6.731437418177089),
                new object[]
                {
                    3.7156412263891623,
                    13,
                    new PdfPoint(35.803655214742406, 5.16089154047169),
                }
            },
            new object[]
            {
                new PdfPoint(9.500054562360727, 40.24933325499364),
                new object[]
                {
                    7.832014004033237,
                    42,
                    new PdfPoint(13.34483441571791, 47.0726836958395),
                }
            },
            new object[]
            {
                new PdfPoint(4.158440156246623, 15.800929441459854),
                new object[]
                {
                    7.657460730577243,
                    26,
                    new PdfPoint(11.228694290441743, 12.860145308234605),
                }
            },
            new object[]
            {
                new PdfPoint(70.38603008213099, 37.92398478562566),
                new object[]
                {
                    3.5662403566120062,
                    87,
                    new PdfPoint(67.0752294529232, 39.24937886844233),
                }
            },
            new object[]
            {
                new PdfPoint(36.49654657133572, 22.300307481532634),
                new object[]
                {
                    5.24842528186342,
                    2,
                    new PdfPoint(35.583597244228336, 27.46872033901967),
                }
            },
            new object[]
            {
                new PdfPoint(44.448096442616745, 76.71337511467348),
                new object[]
                {
                    10.948730989453496,
                    86,
                    new PdfPoint(52.44257355334966, 84.19422038812934),
                }
            },
            new object[]
            {
                new PdfPoint(18.520340417793502, 44.31476289468993),
                new object[]
                {
                    1.378368419246679,
                    7,
                    new PdfPoint(19.15551535520754, 45.53805856543256),
                }
            },
            new object[]
            {
                new PdfPoint(68.27767536726084, 94.51997472754229),
                new object[]
                {
                    4.195644188434987,
                    46,
                    new PdfPoint(67.53711275495073, 90.39020510040851),
                }
            },
            new object[]
            {
                new PdfPoint(80.9852186456766, 37.41169774270004),
                new object[]
                {
                    5.032841375488803,
                    72,
                    new PdfPoint(84.97188896260866, 40.483497230772926),
                }
            },
            new object[]
            {
                new PdfPoint(57.035762190811745, 39.26879178691382),
                new object[]
                {
                    8.758706221403711,
                    47,
                    new PdfPoint(62.56803790975437, 46.05914229645705),
                }
            },
            new object[]
            {
                new PdfPoint(12.085443471887913, 97.92593979377935),
                new object[]
                {
                    2.1562961875551574,
                    18,
                    new PdfPoint(14.158071542303697, 98.52077265281578),
                }
            },
            new object[]
            {
                new PdfPoint(77.27841905993205, 42.15685387387795),
                new object[]
                {
                    4.125216461895987,
                    74,
                    new PdfPoint(75.03458936225277, 45.61844875850991),
                }
            },
            new object[]
            {
                new PdfPoint(85.87212254856951, 56.063370792337665),
                new object[]
                {
                    3.032017326786329,
                    29,
                    new PdfPoint(86.97060905691454, 53.23733886383501),
                }
            },
            new object[]
            {
                new PdfPoint(12.121061998950344, 88.5888682638655),
                new object[]
                {
                    10.138645504748764,
                    18,
                    new PdfPoint(14.158071542303697, 98.52077265281578),
                }
            },
            new object[]
            {
                new PdfPoint(87.40498458296966, 51.58451776586178),
                new object[]
                {
                    1.7089469504759578,
                    29,
                    new PdfPoint(86.97060905691454, 53.23733886383501),
                }
            },
            new object[]
            {
                new PdfPoint(55.13592516333022, 41.86805042798852),
                new object[]
                {
                    8.532382488232894,
                    47,
                    new PdfPoint(62.56803790975437, 46.05914229645705),
                }
            },
            new object[]
            {
                new PdfPoint(46.67110223109417, 55.89363812932652),
                new object[]
                {
                    3.8450044606910225,
                    60,
                    new PdfPoint(43.017566199578106, 57.09185921458071),
                }
            },
            new object[]
            {
                new PdfPoint(16.092850368369838, 39.120936314989095),
                new object[]
                {
                    7.110511570818105,
                    7,
                    new PdfPoint(19.15551535520754, 45.53805856543256),
                }
            },
            new object[]
            {
                new PdfPoint(60.44407078486588, 51.23153354793123),
                new object[]
                {
                    5.591499584720878,
                    47,
                    new PdfPoint(62.56803790975437, 46.05914229645705),
                }
            },
            new object[]
            {
                new PdfPoint(16.3698062506329, 73.24470440615686),
                new object[]
                {
                    5.656289708761183,
                    68,
                    new PdfPoint(10.747687004702266, 72.62390777764313),
                }
            },
            new object[]
            {
                new PdfPoint(23.603385764313224, 35.368200010880244),
                new object[]
                {
                    6.7783441028566624,
                    56,
                    new PdfPoint(29.725739779656745, 38.277277199189065),
                }
            },
            new object[]
            {
                new PdfPoint(55.00466410944499, 87.18171359059261),
                new object[]
                {
                    3.9356605103079088,
                    86,
                    new PdfPoint(52.44257355334966, 84.19422038812934),
                }
            },
            new object[]
            {
                new PdfPoint(79.35386322733488, 71.45043817963591),
                new object[]
                {
                    2.7433909868872557,
                    73,
                    new PdfPoint(82.04208148460329, 71.99786553392418),
                }
            },
            new object[]
            {
                new PdfPoint(92.04745240854216, 81.04432394243022),
                new object[]
                {
                    1.193122715963635,
                    70,
                    new PdfPoint(91.00755813085259, 81.62926984298593),
                }
            },
            new object[]
            {
                new PdfPoint(38.38165092946303, 48.26803735488079),
                new object[]
                {
                    9.967524396930479,
                    60,
                    new PdfPoint(43.017566199578106, 57.09185921458071),
                }
            },
            new object[]
            {
                new PdfPoint(76.69366761932686, 86.37384253935019),
                new object[]
                {
                    5.9336956035398005,
                    41,
                    new PdfPoint(74.16588992664359, 81.00550169213861),
                }
            },
            new object[]
            {
                new PdfPoint(26.857001817848914, 72.32337753974622),
                new object[]
                {
                    7.407056290485798,
                    10,
                    new PdfPoint(24.41810676553939, 79.31739454102991),
                }
            },
            new object[]
            {
                new PdfPoint(67.72139867299835, 92.06400298246639),
                new object[]
                {
                    1.6839123045966824,
                    46,
                    new PdfPoint(67.53711275495073, 90.39020510040851),
                }
            },
            new object[]
            {
                new PdfPoint(60.07059131774799, 65.14826966545107),
                new object[]
                {
                    1.1330704932109386,
                    23,
                    new PdfPoint(59.31519405414486, 64.30374393065894),
                }
            },
            new object[]
            {
                new PdfPoint(98.18469148101073, 19.636669855162936),
                new object[]
                {
                    3.018581465663707,
                    82,
                    new PdfPoint(99.66377946738517, 22.268044551668787),
                }
            },
            new object[]
            {
                new PdfPoint(13.315505500931046, 2.219169876084226),
                new object[]
                {
                    2.3566520929687598,
                    64,
                    new PdfPoint(15.264247773398054, 0.8939481426395335),
                }
            },
            new object[]
            {
                new PdfPoint(9.499232323084716, 53.073137917110024),
                new object[]
                {
                    3.9093162473638645,
                    81,
                    new PdfPoint(11.201209707090698, 49.55375920684947),
                }
            },
            new object[]
            {
                new PdfPoint(68.33664969059441, 84.3521765566152),
                new object[]
                {
                    2.4215364431973803,
                    58,
                    new PdfPoint(70.09436264721477, 86.01779473250726),
                }
            },
            new object[]
            {
                new PdfPoint(68.17553361331473, 88.07665773162144),
                new object[]
                {
                    2.4000172124415697,
                    46,
                    new PdfPoint(67.53711275495073, 90.39020510040851),
                }
            },
            new object[]
            {
                new PdfPoint(49.166278548969665, 1.397572786085366),
                new object[]
                {
                    6.93584258247533,
                    88,
                    new PdfPoint(43.03278016511083, 4.6357991376672185),
                }
            },
            new object[]
            {
                new PdfPoint(24.727638210913018, 32.081396611116276),
                new object[]
                {
                    7.960524829000644,
                    56,
                    new PdfPoint(29.725739779656745, 38.277277199189065),
                }
            },
            new object[]
            {
                new PdfPoint(88.42089809955208, 39.026696182483356),
                new object[]
                {
                    1.7575731329222621,
                    79,
                    new PdfPoint(90.03637785608008, 39.71900281528301),
                }
            },
            new object[]
            {
                new PdfPoint(51.42556152624328, 15.448878446223556),
                new object[]
                {
                    1.470978335201828,
                    34,
                    new PdfPoint(52.89633110070122, 15.424096971651757),
                }
            },
            new object[]
            {
                new PdfPoint(19.693229959708034, 2.7893290005286175),
                new object[]
                {
                    2.1650646266448024,
                    51,
                    new PdfPoint(18.13308334914272, 1.2881803099952127),
                }
            },
            new object[]
            {
                new PdfPoint(9.279840978602271, 29.123265376879715),
                new object[]
                {
                    6.294219198683488,
                    57,
                    new PdfPoint(15.448731011261907, 30.373061794712797),
                }
            },
            new object[]
            {
                new PdfPoint(78.20191411050928, 81.43682258294183),
                new object[]
                {
                    2.4567740328680956,
                    27,
                    new PdfPoint(80.62371302859762, 81.02375037506395),
                }
            },
            new object[]
            {
                new PdfPoint(40.537261184519394, 72.9327512887614),
                new object[]
                {
                    10.517715080249477,
                    84,
                    new PdfPoint(42.195134616764996, 62.54652068319705),
                }
            },
            new object[]
            {
                new PdfPoint(7.386788690671264, 3.4978575051093697),
                new object[]
                {
                    5.549162421342523,
                    17,
                    new PdfPoint(8.254965696884042, 8.97868520394496),
                }
            },
            new object[]
            {
                new PdfPoint(26.05756081867855, 84.20846377940514),
                new object[]
                {
                    3.579960160958175,
                    98,
                    new PdfPoint(28.000497306010118, 81.20161949340638),
                }
            },
            new object[]
            {
                new PdfPoint(74.3139313663137, 31.084939962251912),
                new object[]
                {
                    7.718062249062456,
                    15,
                    new PdfPoint(80.11998397458856, 25.99984035749021),
                }
            },
            new object[]
            {
                new PdfPoint(27.08914433160744, 37.16399756733006),
                new object[]
                {
                    2.8619970467116924,
                    56,
                    new PdfPoint(29.725739779656745, 38.277277199189065),
                }
            },
            new object[]
            {
                new PdfPoint(70.85333147347784, 5.242611226453819),
                new object[]
                {
                    1.3605803595971302,
                    76,
                    new PdfPoint(70.36073331446983, 3.9743344551363857),
                }
            },
            new object[]
            {
                new PdfPoint(51.77204458863274, 52.932206910888034),
                new object[]
                {
                    9.69245062675278,
                    60,
                    new PdfPoint(43.017566199578106, 57.09185921458071),
                }
            },
            new object[]
            {
                new PdfPoint(13.471218812546915, 11.31698816247696),
                new object[]
                {
                    2.7221774756150476,
                    26,
                    new PdfPoint(11.228694290441743, 12.860145308234605),
                }
            },
            new object[]
            {
                new PdfPoint(23.382642351306295, 64.21491294035737),
                new object[]
                {
                    5.629527326020367,
                    69,
                    new PdfPoint(18.204503055467892, 62.00619128580146),
                }
            },
            new object[]
            {
                new PdfPoint(21.987233262429573, 11.042886413409592),
                new object[]
                {
                    6.829449766789799,
                    1,
                    new PdfPoint(16.445674398013832, 7.051331644986569),
                }
            },
            new object[]
            {
                new PdfPoint(44.65334716075204, 56.72809804882697),
                new object[]
                {
                    1.6757391022022616,
                    60,
                    new PdfPoint(43.017566199578106, 57.09185921458071),
                }
            },
            new object[]
            {
                new PdfPoint(14.413577866791204, 65.20588702038663),
                new object[]
                {
                    1.2324454456029377,
                    21,
                    new PdfPoint(15.143510163282482, 66.19892308162831),
                }
            },
            new object[]
            {
                new PdfPoint(1.496525343024535, 34.243471455041515),
                new object[]
                {
                    4.83233101936407,
                    62,
                    new PdfPoint(2.4884293540186175, 29.514037079380717),
                }
            },
            new object[]
            {
                new PdfPoint(60.37475135874306, 86.38587789183474),
                new object[]
                {
                    7.556125515305361,
                    75,
                    new PdfPoint(62.777729239709636, 93.54972672515291),
                }
            },
            new object[]
            {
                new PdfPoint(87.96474891454915, 60.71664437764912),
                new object[]
                {
                    5.766963730198733,
                    0,
                    new PdfPoint(82.45353838109239, 62.415005093558115),
                }
            },
            new object[]
            {
                new PdfPoint(93.90443153077325, 3.5656814220870303),
                new object[]
                {
                    4.363063424232462,
                    54,
                    new PdfPoint(97.7460348272766, 5.634112362720456),
                }
            },
            new object[]
            {
                new PdfPoint(56.99120651646804, 81.00351342714225),
                new object[]
                {
                    2.7959884805796307,
                    32,
                    new PdfPoint(55.68259729639997, 78.53266468780453),
                }
            },
            new object[]
            {
                new PdfPoint(76.89422069517484, 54.17932624450519),
                new object[]
                {
                    6.085820174967678,
                    59,
                    new PdfPoint(71.14819197800337, 52.17424240997747),
                }
            },
            new object[]
            {
                new PdfPoint(63.45013931565355, 80.49930589119406),
                new object[]
                {
                    8.012639180927806,
                    32,
                    new PdfPoint(55.68259729639997, 78.53266468780453),
                }
            },
            new object[]
            {
                new PdfPoint(31.33405794501082, 30.022880285076926),
                new object[]
                {
                    4.958055796946755,
                    2,
                    new PdfPoint(35.583597244228336, 27.46872033901967),
                }
            },
            new object[]
            {
                new PdfPoint(59.209873769066554, 46.30109565207685),
                new object[]
                {
                    3.366869142407712,
                    47,
                    new PdfPoint(62.56803790975437, 46.05914229645705),
                }
            },
            new object[]
            {
                new PdfPoint(78.79398195705156, 78.45536064489083),
                new object[]
                {
                    3.153496725896489,
                    27,
                    new PdfPoint(80.62371302859762, 81.02375037506395),
                }
            },
            new object[]
            {
                new PdfPoint(85.57104507254752, 24.654183310002807),
                new object[]
                {
                    5.053213509947922,
                    8,
                    new PdfPoint(80.59123120177551, 25.51233282470693),
                }
            },
            new object[]
            {
                new PdfPoint(78.95708936964519, 12.704872047938153),
                new object[]
                {
                    6.31686834491134,
                    40,
                    new PdfPoint(82.61099784649278, 17.85771383958713),
                }
            },
            new object[]
            {
                new PdfPoint(13.174935980071456, 88.82914034992214),
                new object[]
                {
                    9.741370141218182,
                    18,
                    new PdfPoint(14.158071542303697, 98.52077265281578),
                }
            },
            new object[]
            {
                new PdfPoint(55.623046798666124, 96.84465577887713),
                new object[]
                {
                    7.4016913841622145,
                    36,
                    new PdfPoint(49.53804959253269, 92.63065004232082),
                }
            },
            new object[]
            {
                new PdfPoint(8.430144926873329, 97.99469494927773),
                new object[]
                {
                    5.75203451501427,
                    18,
                    new PdfPoint(14.158071542303697, 98.52077265281578),
                }
            },
            new object[]
            {
                new PdfPoint(43.22581030476383, 49.88363608992273),
                new object[]
                {
                    7.211230562268732,
                    60,
                    new PdfPoint(43.017566199578106, 57.09185921458071),
                }
            },
            new object[]
            {
                new PdfPoint(52.462469277256375, 74.30081146714733),
                new object[]
                {
                    5.317688044710818,
                    32,
                    new PdfPoint(55.68259729639997, 78.53266468780453),
                }
            },
            new object[]
            {
                new PdfPoint(2.0752344418159763, 84.50188522953496),
                new object[]
                {
                    14.707065710166793,
                    68,
                    new PdfPoint(10.747687004702266, 72.62390777764313),
                }
            },
            new object[]
            {
                new PdfPoint(87.72047866413772, 86.93785567990162),
                new object[]
                {
                    1.8361371712469572,
                    95,
                    new PdfPoint(89.04717161055198, 88.20721990563409),
                }
            },
            new object[]
            {
                new PdfPoint(85.69225991285919, 6.507776800176323),
                new object[]
                {
                    5.40677558683976,
                    28,
                    new PdfPoint(90.87481745321885, 8.048659530077462),
                }
            },
            new object[]
            {
                new PdfPoint(95.701988137853, 51.80572553493597),
                new object[]
                {
                    2.11554101881059,
                    6,
                    new PdfPoint(97.13960454465617, 50.25370334786503),
                }
            },
            new object[]
            {
                new PdfPoint(77.4844196684514, 50.073882954946704),
                new object[]
                {
                    5.084541514035221,
                    74,
                    new PdfPoint(75.03458936225277, 45.61844875850991),
                }
            },
            new object[]
            {
                new PdfPoint(93.83858926824583, 78.54914176983279),
                new object[]
                {
                    4.183530356997088,
                    70,
                    new PdfPoint(91.00755813085259, 81.62926984298593),
                }
            },
            new object[]
            {
                new PdfPoint(69.60341179436881, 2.118953273473234),
                new object[]
                {
                    2.003989823845886,
                    76,
                    new PdfPoint(70.36073331446983, 3.9743344551363857),
                }
            },
            new object[]
            {
                new PdfPoint(21.036323077268815, 30.797805649614528),
                new object[]
                {
                    5.603712380054862,
                    57,
                    new PdfPoint(15.448731011261907, 30.373061794712797),
                }
            },
            new object[]
            {
                new PdfPoint(67.01310772132939, 11.758224733999745),
                new object[]
                {
                    4.235938831569964,
                    19,
                    new PdfPoint(70.71899728005727, 9.70650341683359),
                }
            },
            new object[]
            {
                new PdfPoint(58.40755345770724, 54.22645743167164),
                new object[]
                {
                    9.16595152690823,
                    47,
                    new PdfPoint(62.56803790975437, 46.05914229645705),
                }
            },
            new object[]
            {
                new PdfPoint(49.248852518964966, 70.66641193673007),
                new object[]
                {
                    4.434797767182969,
                    71,
                    new PdfPoint(52.97863363490906, 68.26721122337617),
                }
            },
            new object[]
            {
                new PdfPoint(2.7054302324065804, 31.088830779825305),
                new object[]
                {
                    1.5896743629376124,
                    62,
                    new PdfPoint(2.4884293540186175, 29.514037079380717),
                }
            },
            new object[]
            {
                new PdfPoint(30.842485871231062, 31.67560039456263),
                new object[]
                {
                    6.3384522442698294,
                    2,
                    new PdfPoint(35.583597244228336, 27.46872033901967),
                }
            },
            new object[]
            {
                new PdfPoint(55.56502506452341, 65.54856266577112),
                new object[]
                {
                    3.75239531592163,
                    71,
                    new PdfPoint(52.97863363490906, 68.26721122337617),
                }
            },
            new object[]
            {
                new PdfPoint(1.267016931392484, 79.30364358908852),
                new object[]
                {
                    11.59749867642406,
                    68,
                    new PdfPoint(10.747687004702266, 72.62390777764313),
                }
            },
            new object[]
            {
                new PdfPoint(85.80159109593838, 72.9274073673635),
                new object[]
                {
                    3.8727200438238047,
                    73,
                    new PdfPoint(82.04208148460329, 71.99786553392418),
                }
            },
            new object[]
            {
                new PdfPoint(31.687705038346536, 73.64980455511628),
                new object[]
                {
                    8.403892534033446,
                    98,
                    new PdfPoint(28.000497306010118, 81.20161949340638),
                }
            },
            new object[]
            {
                new PdfPoint(9.155398598534447, 51.101027419495416),
                new object[]
                {
                    2.565030606787517,
                    81,
                    new PdfPoint(11.201209707090698, 49.55375920684947),
                }
            },
            new object[]
            {
                new PdfPoint(95.78765937402926, 29.508025935639814),
                new object[]
                {
                    4.180615066997134,
                    90,
                    new PdfPoint(93.45285655237589, 26.040139241642603),
                }
            },
            new object[]
            {
                new PdfPoint(67.92396367960997, 22.903155180355462),
                new object[]
                {
                    2.5948429360078844,
                    5,
                    new PdfPoint(65.40181734760978, 22.29324720161768),
                }
            },
            new object[]
            {
                new PdfPoint(38.49112754016583, 70.52567748005846),
                new object[]
                {
                    8.79696604588175,
                    84,
                    new PdfPoint(42.195134616764996, 62.54652068319705),
                }
            },
            new object[]
            {
                new PdfPoint(59.65725942551804, 65.37065649962103),
                new object[]
                {
                    1.120406688708154,
                    23,
                    new PdfPoint(59.31519405414486, 64.30374393065894),
                }
            },
            new object[]
            {
                new PdfPoint(45.15904139251526, 74.24820871457037),
                new object[]
                {
                    9.844711972778917,
                    71,
                    new PdfPoint(52.97863363490906, 68.26721122337617),
                }
            }
        };

        public static IEnumerable<object[]> DataTreeK2 => new[]
        {
            // k = 3
            new object[]
            {
                new PdfPoint(90.33048545330094, 47.33938084378586),
                new object[]
                {
                    new object[]
                    {
                        6.319282378964871,
                        77,
                        new PdfPoint(84.64548329605812, 44.580018552016185),
                    },
                    new object[]
                    {
                        6.787833100869266,
                        29,
                        new PdfPoint(86.97060905691454, 53.23733886383501),
                    },
                    new object[]
                    {
                        7.406576703041731,
                        6,
                        new PdfPoint(97.13960454465617, 50.25370334786503),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(94.00419687661137, 1.6606574220328518),
                new object[]
                {
                    new object[]
                    {
                        5.457993716991005,
                        54,
                        new PdfPoint(97.7460348272766, 5.634112362720456),
                    },
                    new object[]
                    {
                        7.11333863301438,
                        28,
                        new PdfPoint(90.87481745321885, 8.048659530077462),
                    },
                    new object[]
                    {
                        11.581239683136776,
                        11,
                        new PdfPoint(98.12630738875605, 12.483472098628233),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(85.52978116794068, 97.43823054363845),
                new object[]
                {
                    new object[]
                    {
                        6.495434817422279,
                        67,
                        new PdfPoint(91.90691599722378, 96.20420265233045),
                    },
                    new object[]
                    {
                        9.733700402810387,
                        16,
                        new PdfPoint(90.26397977468233, 88.93339150438577),
                    },
                    new object[]
                    {
                        9.878440814456654,
                        95,
                        new PdfPoint(89.04717161055198, 88.20721990563409),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(22.62162567299718, 79.83427737771372),
                new object[]
                {
                    new object[]
                    {
                        1.8693614371543983,
                        10,
                        new PdfPoint(24.41810676553939, 79.31739454102991),
                    },
                    new object[]
                    {
                        5.549944549793103,
                        98,
                        new PdfPoint(28.000497306010118, 81.20161949340638),
                    },
                    new object[]
                    {
                        13.114774665647976,
                        66,
                        new PdfPoint(34.43836768761709, 85.52303143204219),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(7.692210312894288, 89.82892292381919),
                new object[]
                {
                    new object[]
                    {
                        10.833079578284183,
                        18,
                        new PdfPoint(14.158071542303697, 98.52077265281578),
                    },
                    new object[]
                    {
                        17.47422341605857,
                        68,
                        new PdfPoint(10.747687004702266, 72.62390777764313),
                    },
                    new object[]
                    {
                        19.75469162216385,
                        10,
                        new PdfPoint(24.41810676553939, 79.31739454102991),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(34.94017096790447, 92.33233895331428),
                new object[]
                {
                    new object[]
                    {
                        6.827772363762564,
                        66,
                        new PdfPoint(34.43836768761709, 85.52303143204219),
                    },
                    new object[]
                    {
                        13.116858855258087,
                        98,
                        new PdfPoint(28.000497306010118, 81.20161949340638),
                    },
                    new object[]
                    {
                        14.60092633517482,
                        36,
                        new PdfPoint(49.53804959253269, 92.63065004232082),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(55.63755197233502, 33.336503157598464),
                new object[]
                {
                    new object[]
                    {
                        11.95859196631113,
                        96,
                        new PdfPoint(46.898436066324166, 25.173429288152636),
                    },
                    new object[]
                    {
                        12.210892065182815,
                        80,
                        new PdfPoint(63.98092485758994, 24.420561596263646),
                    },
                    new object[]
                    {
                        12.87565785976939,
                        87,
                        new PdfPoint(67.0752294529232, 39.24937886844233),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(81.20904836792344, 64.08948679068317),
                new object[]
                {
                    new object[]
                    {
                        1.9994837794229934,
                        30,
                        new PdfPoint(79.77968787046065, 62.69132229661655),
                    },
                    new object[]
                    {
                        2.0862991987929322,
                        0,
                        new PdfPoint(82.45353838109239, 62.415005093558115),
                    },
                    new object[]
                    {
                        5.102187165794271,
                        52,
                        new PdfPoint(81.60160120456186, 59.00242318333139),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(71.52680243530935, 60.971051107521035),
                new object[]
                {
                    new object[]
                    {
                        8.430269922710929,
                        30,
                        new PdfPoint(79.77968787046065, 62.69132229661655),
                    },
                    new object[]
                    {
                        8.804952534770395,
                        59,
                        new PdfPoint(71.14819197800337, 52.17424240997747),
                    },
                    new object[]
                    {
                        10.265333221324632,
                        52,
                        new PdfPoint(81.60160120456186, 59.00242318333139),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(29.27291899970018, 12.928347645791094),
                new object[]
                {
                    new object[]
                    {
                        9.723303049123018,
                        99,
                        new PdfPoint(24.103817108644833, 21.163820177769633),
                    },
                    new object[]
                    {
                        10.14809784435131,
                        13,
                        new PdfPoint(35.803655214742406, 5.16089154047169),
                    },
                    new object[]
                    {
                        10.227363038462595,
                        38,
                        new PdfPoint(24.636942156010523, 22.044633809864933),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(46.78746330316932, 67.20315193481265),
                new object[]
                {
                    new object[]
                    {
                        6.281943349489286,
                        71,
                        new PdfPoint(52.97863363490906, 68.26721122337617),
                    },
                    new object[]
                    {
                        6.540160347995691,
                        84,
                        new PdfPoint(42.195134616764996, 62.54652068319705),
                    },
                    new object[]
                    {
                        10.791217014122221,
                        60,
                        new PdfPoint(43.017566199578106, 57.09185921458071),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(89.49370609342411, 49.19740999815338),
                new object[]
                {
                    new object[]
                    {
                        4.763091842008821,
                        29,
                        new PdfPoint(86.97060905691454, 53.23733886383501),
                    },
                    new object[]
                    {
                        6.695189919618434,
                        77,
                        new PdfPoint(84.64548329605812, 44.580018552016185),
                    },
                    new object[]
                    {
                        7.718517912604583,
                        6,
                        new PdfPoint(97.13960454465617, 50.25370334786503),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(50.085173044511734, 76.09967482070661),
                new object[]
                {
                    new object[]
                    {
                        6.103326793563302,
                        32,
                        new PdfPoint(55.68259729639997, 78.53266468780453),
                    },
                    new object[]
                    {
                        8.34982635697827,
                        71,
                        new PdfPoint(52.97863363490906, 68.26721122337617),
                    },
                    new object[]
                    {
                        8.430836560042678,
                        86,
                        new PdfPoint(52.44257355334966, 84.19422038812934),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(51.41806121874436, 6.185936468302689),
                new object[]
                {
                    new object[]
                    {
                        4.63313326645828,
                        43,
                        new PdfPoint(47.945049322341525, 9.252548969452246),
                    },
                    new object[]
                    {
                        5.775831321961968,
                        83,
                        new PdfPoint(57.19140901930086, 6.016576988006017),
                    },
                    new object[]
                    {
                        6.717112422982287,
                        78,
                        new PdfPoint(44.866927314279856, 7.669932380951572),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(44.84550779030454, 6.558735867057342),
                new object[]
                {
                    new object[]
                    {
                        1.1114029370565919,
                        78,
                        new PdfPoint(44.866927314279856, 7.669932380951572),
                    },
                    new object[]
                    {
                        2.6426628820903373,
                        88,
                        new PdfPoint(43.03278016511083, 4.6357991376672185),
                    },
                    new object[]
                    {
                        4.106554119874308,
                        43,
                        new PdfPoint(47.945049322341525, 9.252548969452246),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(61.96609728269268, 15.51346888405012),
                new object[]
                {
                    new object[]
                    {
                        7.600629342352724,
                        5,
                        new PdfPoint(65.40181734760978, 22.29324720161768),
                    },
                    new object[]
                    {
                        9.070206499012121,
                        34,
                        new PdfPoint(52.89633110070122, 15.424096971651757),
                    },
                    new object[]
                    {
                        9.132131774155129,
                        80,
                        new PdfPoint(63.98092485758994, 24.420561596263646),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(69.5760144871494, 76.4622356528522),
                new object[]
                {
                    new object[]
                    {
                        6.458190369895054,
                        41,
                        new PdfPoint(74.16588992664359, 81.00550169213861),
                    },
                    new object[]
                    {
                        9.569607836260653,
                        58,
                        new PdfPoint(70.09436264721477, 86.01779473250726),
                    },
                    new object[]
                    {
                        11.952366277171725,
                        27,
                        new PdfPoint(80.62371302859762, 81.02375037506395),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(82.98110145635202, 52.72268932669417),
                new object[]
                {
                    new object[]
                    {
                        4.022565728614683,
                        29,
                        new PdfPoint(86.97060905691454, 53.23733886383501),
                    },
                    new object[]
                    {
                        6.429469515822035,
                        52,
                        new PdfPoint(81.60160120456186, 59.00242318333139),
                    },
                    new object[]
                    {
                        8.311032081103916,
                        77,
                        new PdfPoint(84.64548329605812, 44.580018552016185),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(50.69508238708692, 87.63717989744282),
                new object[]
                {
                    new object[]
                    {
                        3.861048505126353,
                        86,
                        new PdfPoint(52.44257355334966, 84.19422038812934),
                    },
                    new object[]
                    {
                        5.125765208772435,
                        36,
                        new PdfPoint(49.53804959253269, 92.63065004232082),
                    },
                    new object[]
                    {
                        10.381112761797503,
                        32,
                        new PdfPoint(55.68259729639997, 78.53266468780453),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(45.394375588571556, 61.05494327358284),
                new object[]
                {
                    new object[]
                    {
                        3.529864864914409,
                        84,
                        new PdfPoint(42.195134616764996, 62.54652068319705),
                    },
                    new object[]
                    {
                        4.6211749729180625,
                        60,
                        new PdfPoint(43.017566199578106, 57.09185921458071),
                    },
                    new object[]
                    {
                        10.4660297674453,
                        71,
                        new PdfPoint(52.97863363490906, 68.26721122337617),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(21.560319619569214, 46.112818457615276),
                new object[]
                {
                    new object[]
                    {
                        2.4725356384800556,
                        7,
                        new PdfPoint(19.15551535520754, 45.53805856543256),
                    },
                    new object[]
                    {
                        8.271368593543086,
                        42,
                        new PdfPoint(13.34483441571791, 47.0726836958395),
                    },
                    new object[]
                    {
                        10.915641594452936,
                        81,
                        new PdfPoint(11.201209707090698, 49.55375920684947),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(13.31631815615828, 49.618965383452654),
                new object[]
                {
                    new object[]
                    {
                        2.116113323237742,
                        81,
                        new PdfPoint(11.201209707090698, 49.55375920684947),
                    },
                    new object[]
                    {
                        2.5464413619271418,
                        42,
                        new PdfPoint(13.34483441571791, 47.0726836958395),
                    },
                    new object[]
                    {
                        7.1239051360014365,
                        7,
                        new PdfPoint(19.15551535520754, 45.53805856543256),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(78.68697517856405, 37.95356900539214),
                new object[]
                {
                    new object[]
                    {
                        6.75635336262533,
                        33,
                        new PdfPoint(84.80993603153726, 40.80973096746926),
                    },
                    new object[]
                    {
                        6.775003918703829,
                        72,
                        new PdfPoint(84.97188896260866, 40.483497230772926),
                    },
                    new object[]
                    {
                        8.49060090811873,
                        74,
                        new PdfPoint(75.03458936225277, 45.61844875850991),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(40.25217869763216, 47.69662857204957),
                new object[]
                {
                    new object[]
                    {
                        9.793759587731104,
                        60,
                        new PdfPoint(43.017566199578106, 57.09185921458071),
                    },
                    new object[]
                    {
                        11.423458353755956,
                        49,
                        new PdfPoint(37.970724110053844, 36.503310666941424),
                    },
                    new object[]
                    {
                        11.98058569840965,
                        35,
                        new PdfPoint(31.112041569584935, 55.44209995586027),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(54.1283188372334, 86.53269307585674),
                new object[]
                {
                    new object[]
                    {
                        2.8827403062682038,
                        86,
                        new PdfPoint(52.44257355334966, 84.19422038812934),
                    },
                    new object[]
                    {
                        7.632538955268626,
                        36,
                        new PdfPoint(49.53804959253269, 92.63065004232082),
                    },
                    new object[]
                    {
                        8.149615680403004,
                        32,
                        new PdfPoint(55.68259729639997, 78.53266468780453),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(81.21021662298843, 89.2711165353666),
                new object[]
                {
                    new object[]
                    {
                        4.055682992421869,
                        44,
                        new PdfPoint(82.78544384909999, 85.53384162961818),
                    },
                    new object[]
                    {
                        7.031757366483974,
                        45,
                        new PdfPoint(86.41791572882465, 84.5461317010868),
                    },
                    new object[]
                    {
                        7.908839327983186,
                        95,
                        new PdfPoint(89.04717161055198, 88.20721990563409),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(41.04671360406829, 96.94952583007202),
                new object[]
                {
                    new object[]
                    {
                        9.526566797068826,
                        36,
                        new PdfPoint(49.53804959253269, 92.63065004232082),
                    },
                    new object[]
                    {
                        13.199810982725637,
                        66,
                        new PdfPoint(34.43836768761709, 85.52303143204219),
                    },
                    new object[]
                    {
                        17.104485987625814,
                        86,
                        new PdfPoint(52.44257355334966, 84.19422038812934),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(20.268880198632655, 20.520060156880948),
                new object[]
                {
                    new object[]
                    {
                        2.0060215634503926,
                        50,
                        new PdfPoint(18.3788032374675, 19.84795034896214),
                    },
                    new object[]
                    {
                        3.888594613516348,
                        99,
                        new PdfPoint(24.103817108644833, 21.163820177769633),
                    },
                    new object[]
                    {
                        4.626477070824485,
                        38,
                        new PdfPoint(24.636942156010523, 22.044633809864933),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(38.68584315271796, 74.86521565365805),
                new object[]
                {
                    new object[]
                    {
                        11.473015514354003,
                        66,
                        new PdfPoint(34.43836768761709, 85.52303143204219),
                    },
                    new object[]
                    {
                        12.422826952193041,
                        98,
                        new PdfPoint(28.000497306010118, 81.20161949340638),
                    },
                    new object[]
                    {
                        12.808800582212788,
                        84,
                        new PdfPoint(42.195134616764996, 62.54652068319705),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(67.4231014401171, 82.7767352052507),
                new object[]
                {
                    new object[]
                    {
                        4.200012297096924,
                        58,
                        new PdfPoint(70.09436264721477, 86.01779473250726),
                    },
                    new object[]
                    {
                        6.971546796228628,
                        41,
                        new PdfPoint(74.16588992664359, 81.00550169213861),
                    },
                    new object[]
                    {
                        7.61432350405368,
                        46,
                        new PdfPoint(67.53711275495073, 90.39020510040851),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(14.38235206959435, 13.07129438479172),
                new object[]
                {
                    new object[]
                    {
                        3.160718481696952,
                        26,
                        new PdfPoint(11.228694290441743, 12.860145308234605),
                    },
                    new object[]
                    {
                        6.363744999573501,
                        1,
                        new PdfPoint(16.445674398013832, 7.051331644986569),
                    },
                    new object[]
                    {
                        7.3684675250439415,
                        17,
                        new PdfPoint(8.254965696884042, 8.97868520394496),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(52.14368788446655, 22.1218586105557),
                new object[]
                {
                    new object[]
                    {
                        3.2956133368175657,
                        85,
                        new PdfPoint(49.61310425442538, 20.010647167526496),
                    },
                    new object[]
                    {
                        4.200265094831746,
                        89,
                        new PdfPoint(49.42598643546282, 18.91930873569896),
                    },
                    new object[]
                    {
                        6.0683399901534365,
                        96,
                        new PdfPoint(46.898436066324166, 25.173429288152636),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(86.29153320726485, 95.11424407263593),
                new object[]
                {
                    new object[]
                    {
                        5.720186498989052,
                        67,
                        new PdfPoint(91.90691599722378, 96.20420265233045),
                    },
                    new object[]
                    {
                        7.347330821559044,
                        16,
                        new PdfPoint(90.26397977468233, 88.93339150438577),
                    },
                    new object[]
                    {
                        7.43643233366769,
                        95,
                        new PdfPoint(89.04717161055198, 88.20721990563409),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(81.89614459928313, 66.58499042641813),
                new object[]
                {
                    new object[]
                    {
                        3.6799201803348827,
                        55,
                        new PdfPoint(84.9666796464414, 68.61319827025679),
                    },
                    new object[]
                    {
                        4.207073270608355,
                        0,
                        new PdfPoint(82.45353838109239, 62.415005093558115),
                    },
                    new object[]
                    {
                        4.431708540733531,
                        30,
                        new PdfPoint(79.77968787046065, 62.69132229661655),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(46.050742000044394, 44.44807688975785),
                new object[]
                {
                    new object[]
                    {
                        11.331637103162638,
                        49,
                        new PdfPoint(37.970724110053844, 36.503310666941424),
                    },
                    new object[]
                    {
                        13.002514638101248,
                        60,
                        new PdfPoint(43.017566199578106, 57.09185921458071),
                    },
                    new object[]
                    {
                        16.595680037696102,
                        47,
                        new PdfPoint(62.56803790975437, 46.05914229645705),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(2.4117277982407592, 98.30730212163033),
                new object[]
                {
                    new object[]
                    {
                        11.748283322314418,
                        18,
                        new PdfPoint(14.158071542303697, 98.52077265281578),
                    },
                    new object[]
                    {
                        27.00231399196269,
                        68,
                        new PdfPoint(10.747687004702266, 72.62390777764313),
                    },
                    new object[]
                    {
                        29.067117249085932,
                        10,
                        new PdfPoint(24.41810676553939, 79.31739454102991),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(39.07511433748291, 61.28307901688501),
                new object[]
                {
                    new object[]
                    {
                        3.3661270604813507,
                        84,
                        new PdfPoint(42.195134616764996, 62.54652068319705),
                    },
                    new object[]
                    {
                        5.754063791457791,
                        60,
                        new PdfPoint(43.017566199578106, 57.09185921458071),
                    },
                    new object[]
                    {
                        9.875604502923744,
                        35,
                        new PdfPoint(31.112041569584935, 55.44209995586027),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(9.49024018298229, 59.73883570183125),
                new object[]
                {
                    new object[]
                    {
                        5.795324777487998,
                        9,
                        new PdfPoint(5.021252133213605, 56.04912906205743),
                    },
                    new object[]
                    {
                        7.206582635751992,
                        22,
                        new PdfPoint(16.49098129875618, 61.44894011847811),
                    },
                    new object[]
                    {
                        8.338201846917082,
                        97,
                        new PdfPoint(8.420107657979937, 68.0080815210619),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(12.96964790803895, 56.98374692967552),
                new object[]
                {
                    new object[]
                    {
                        5.686628092455948,
                        22,
                        new PdfPoint(16.49098129875618, 61.44894011847811),
                    },
                    new object[]
                    {
                        7.25456102910056,
                        69,
                        new PdfPoint(18.204503055467892, 62.00619128580146),
                    },
                    new object[]
                    {
                        7.637544843201832,
                        81,
                        new PdfPoint(11.201209707090698, 49.55375920684947),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(13.307937277045156, 52.91717446722356),
                new object[]
                {
                    new object[]
                    {
                        3.968735726616667,
                        81,
                        new PdfPoint(11.201209707090698, 49.55375920684947),
                    },
                    new object[]
                    {
                        5.844607238783087,
                        42,
                        new PdfPoint(13.34483441571791, 47.0726836958395),
                    },
                    new object[]
                    {
                        8.858797336947017,
                        9,
                        new PdfPoint(5.021252133213605, 56.04912906205743),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(26.36175597577918, 31.381877739024254),
                new object[]
                {
                    new object[]
                    {
                        7.672217459639145,
                        56,
                        new PdfPoint(29.725739779656745, 38.277277199189065),
                    },
                    new object[]
                    {
                        9.495214947829645,
                        38,
                        new PdfPoint(24.636942156010523, 22.044633809864933),
                    },
                    new object[]
                    {
                        10.01774212173994,
                        2,
                        new PdfPoint(35.583597244228336, 27.46872033901967),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(71.51791012620515, 60.383581685691176),
                new object[]
                {
                    new object[]
                    {
                        8.21766042452993,
                        59,
                        new PdfPoint(71.14819197800337, 52.17424240997747),
                    },
                    new object[]
                    {
                        8.578032304834814,
                        30,
                        new PdfPoint(79.77968787046065, 62.69132229661655),
                    },
                    new object[]
                    {
                        10.17783987751681,
                        52,
                        new PdfPoint(81.60160120456186, 59.00242318333139),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(13.820958137480366, 45.57166856903693),
                new object[]
                {
                    new object[]
                    {
                        1.5747190890171712,
                        42,
                        new PdfPoint(13.34483441571791, 47.0726836958395),
                    },
                    new object[]
                    {
                        4.766563509099969,
                        81,
                        new PdfPoint(11.201209707090698, 49.55375920684947),
                    },
                    new object[]
                    {
                        5.334663095411686,
                        7,
                        new PdfPoint(19.15551535520754, 45.53805856543256),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(62.60837807772033, 59.51480363049938),
                new object[]
                {
                    new object[]
                    {
                        5.8119712844804265,
                        23,
                        new PdfPoint(59.31519405414486, 64.30374393065894),
                    },
                    new object[]
                    {
                        10.500007541726367,
                        20,
                        new PdfPoint(58.60508497220939, 69.22169822597527),
                    },
                    new object[]
                    {
                        10.892398267625163,
                        24,
                        new PdfPoint(58.880198617023936, 69.74930498275906),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(54.246412695504056, 33.090909319980845),
                new object[]
                {
                    new object[]
                    {
                        10.801817004438293,
                        96,
                        new PdfPoint(46.898436066324166, 25.173429288152636),
                    },
                    new object[]
                    {
                        13.035937123351204,
                        80,
                        new PdfPoint(63.98092485758994, 24.420561596263646),
                    },
                    new object[]
                    {
                        13.876628015735688,
                        85,
                        new PdfPoint(49.61310425442538, 20.010647167526496),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(37.59119855061173, 20.84553738123185),
                new object[]
                {
                    new object[]
                    {
                        6.920766973227989,
                        2,
                        new PdfPoint(35.583597244228336, 27.46872033901967),
                    },
                    new object[]
                    {
                        7.1217149520573555,
                        53,
                        new PdfPoint(37.57869607184197, 27.967241358959505),
                    },
                    new object[]
                    {
                        10.264273892091708,
                        96,
                        new PdfPoint(46.898436066324166, 25.173429288152636),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(60.2394597901307, 52.39882244517341),
                new object[]
                {
                    new object[]
                    {
                        6.753800444728761,
                        47,
                        new PdfPoint(62.56803790975437, 46.05914229645705),
                    },
                    new object[]
                    {
                        8.316180599171837,
                        3,
                        new PdfPoint(63.8245554804822, 44.89509346225664),
                    },
                    new object[]
                    {
                        10.911043677803592,
                        59,
                        new PdfPoint(71.14819197800337, 52.17424240997747),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(69.05917872300058, 14.344285937634815),
                new object[]
                {
                    new object[]
                    {
                        4.92585265234346,
                        19,
                        new PdfPoint(70.71899728005727, 9.70650341683359),
                    },
                    new object[]
                    {
                        6.728125898818047,
                        48,
                        new PdfPoint(71.56130305322326, 8.098723357173876),
                    },
                    new object[]
                    {
                        8.314145698967357,
                        92,
                        new PdfPoint(69.38884149074713, 6.0366785105100185),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(51.331730724820865, 96.31696159244558),
                new object[]
                {
                    new object[]
                    {
                        4.099534711270266,
                        36,
                        new PdfPoint(49.53804959253269, 92.63065004232082),
                    },
                    new object[]
                    {
                        11.775757759634676,
                        75,
                        new PdfPoint(62.777729239709636, 93.54972672515291),
                    },
                    new object[]
                    {
                        12.173529730383077,
                        86,
                        new PdfPoint(52.44257355334966, 84.19422038812934),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(80.9218188849778, 57.661185428982684),
                new object[]
                {
                    new object[]
                    {
                        1.5036697495492302,
                        52,
                        new PdfPoint(81.60160120456186, 59.00242318333139),
                    },
                    new object[]
                    {
                        4.994493569730729,
                        0,
                        new PdfPoint(82.45353838109239, 62.415005093558115),
                    },
                    new object[]
                    {
                        5.1581721725288885,
                        30,
                        new PdfPoint(79.77968787046065, 62.69132229661655),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(4.784694797817046, 62.959677091870205),
                new object[]
                {
                    new object[]
                    {
                        6.221142495114544,
                        97,
                        new PdfPoint(8.420107657979937, 68.0080815210619),
                    },
                    new object[]
                    {
                        6.9145956819816385,
                        9,
                        new PdfPoint(5.021252133213605, 56.04912906205743),
                    },
                    new object[]
                    {
                        10.853468125809684,
                        21,
                        new PdfPoint(15.143510163282482, 66.19892308162831),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(82.41590594603949, 28.680736275752395),
                new object[]
                {
                    new object[]
                    {
                        0.6666342489180029,
                        12,
                        new PdfPoint(82.48631571672695, 29.34364176375367),
                    },
                    new object[]
                    {
                        3.5296544623441446,
                        15,
                        new PdfPoint(80.11998397458856, 25.99984035749021),
                    },
                    new object[]
                    {
                        3.65625742405422,
                        8,
                        new PdfPoint(80.59123120177551, 25.51233282470693),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(58.93828314851418, 41.6374695944503),
                new object[]
                {
                    new object[]
                    {
                        5.72069131402993,
                        47,
                        new PdfPoint(62.56803790975437, 46.05914229645705),
                    },
                    new object[]
                    {
                        5.8726289314290705,
                        3,
                        new PdfPoint(63.8245554804822, 44.89509346225664),
                    },
                    new object[]
                    {
                        8.480145781558287,
                        87,
                        new PdfPoint(67.0752294529232, 39.24937886844233),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(87.95455074935215, 32.292861392280614),
                new object[]
                {
                    new object[]
                    {
                        6.212848846488876,
                        12,
                        new PdfPoint(82.48631571672695, 29.34364176375367),
                    },
                    new object[]
                    {
                        7.712430261385789,
                        79,
                        new PdfPoint(90.03637785608008, 39.71900281528301),
                    },
                    new object[]
                    {
                        8.326337790207875,
                        90,
                        new PdfPoint(93.45285655237589, 26.040139241642603),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(79.20389918634515, 25.654064891808016),
                new object[]
                {
                    new object[]
                    {
                        0.9791690415442634,
                        15,
                        new PdfPoint(80.11998397458856, 25.99984035749021),
                    },
                    new object[]
                    {
                        1.3945530107825985,
                        8,
                        new PdfPoint(80.59123120177551, 25.51233282470693),
                    },
                    new object[]
                    {
                        4.081864905775139,
                        94,
                        new PdfPoint(80.69475374791655, 21.854199922464968),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(63.37032482613486, 52.83441603652857),
                new object[]
                {
                    new object[]
                    {
                        6.822609365125496,
                        47,
                        new PdfPoint(62.56803790975437, 46.05914229645705),
                    },
                    new object[]
                    {
                        7.805834141801166,
                        59,
                        new PdfPoint(71.14819197800337, 52.17424240997747),
                    },
                    new object[]
                    {
                        7.952305855894424,
                        3,
                        new PdfPoint(63.8245554804822, 44.89509346225664),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(66.0404611511353, 76.6960457458976),
                new object[]
                {
                    new object[]
                    {
                        9.197499841720573,
                        41,
                        new PdfPoint(74.16588992664359, 81.00550169213861),
                    },
                    new object[]
                    {
                        9.976300255487988,
                        24,
                        new PdfPoint(58.880198617023936, 69.74930498275906),
                    },
                    new object[]
                    {
                        10.165093285812645,
                        58,
                        new PdfPoint(70.09436264721477, 86.01779473250726),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(73.6630405559061, 20.85637324912446),
                new object[]
                {
                    new object[]
                    {
                        7.10215801603454,
                        94,
                        new PdfPoint(80.69475374791655, 21.854199922464968),
                    },
                    new object[]
                    {
                        8.25514216757696,
                        15,
                        new PdfPoint(80.11998397458856, 25.99984035749021),
                    },
                    new object[]
                    {
                        8.347322037334528,
                        8,
                        new PdfPoint(80.59123120177551, 25.51233282470693),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(97.68962763674809, 43.53575778881722),
                new object[]
                {
                    new object[]
                    {
                        4.718909708545427,
                        93,
                        new PdfPoint(99.69679458529599, 47.806517636673476),
                    },
                    new object[]
                    {
                        6.740424165893753,
                        6,
                        new PdfPoint(97.13960454465617, 50.25370334786503),
                    },
                    new object[]
                    {
                        8.55218397447652,
                        79,
                        new PdfPoint(90.03637785608008, 39.71900281528301),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(3.9643845233934605, 49.339573111296765),
                new object[]
                {
                    new object[]
                    {
                        2.995004662567729,
                        14,
                        new PdfPoint(1.813800375443253, 47.255096977735214),
                    },
                    new object[]
                    {
                        6.792283136109303,
                        9,
                        new PdfPoint(5.021252133213605, 56.04912906205743),
                    },
                    new object[]
                    {
                        7.239994089978434,
                        81,
                        new PdfPoint(11.201209707090698, 49.55375920684947),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(70.73913716966032, 10.028007391201522),
                new object[]
                {
                    new object[]
                    {
                        0.3221341656633137,
                        19,
                        new PdfPoint(70.71899728005727, 9.70650341683359),
                    },
                    new object[]
                    {
                        2.0971632325712752,
                        48,
                        new PdfPoint(71.56130305322326, 8.098723357173876),
                    },
                    new object[]
                    {
                        4.2135501248156215,
                        92,
                        new PdfPoint(69.38884149074713, 6.0366785105100185),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(79.28915979507235, 54.70843490604055),
                new object[]
                {
                    new object[]
                    {
                        4.877060651440881,
                        52,
                        new PdfPoint(81.60160120456186, 59.00242318333139),
                    },
                    new object[]
                    {
                        7.82104764898212,
                        29,
                        new PdfPoint(86.97060905691454, 53.23733886383501),
                    },
                    new object[]
                    {
                        7.997944041024629,
                        30,
                        new PdfPoint(79.77968787046065, 62.69132229661655),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(87.95765064513492, 76.78785901003045),
                new object[]
                {
                    new object[]
                    {
                        5.721992181478006,
                        70,
                        new PdfPoint(91.00755813085259, 81.62926984298593),
                    },
                    new object[]
                    {
                        6.098901750707647,
                        37,
                        new PdfPoint(92.45607820110665, 72.66951564303699),
                    },
                    new object[]
                    {
                        6.409897446967801,
                        65,
                        new PdfPoint(82.27200991589356, 79.74763514660499),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(8.309511701427963, 39.13056340529091),
                new object[]
                {
                    new object[]
                    {
                        9.403815690802748,
                        42,
                        new PdfPoint(13.34483441571791, 47.0726836958395),
                    },
                    new object[]
                    {
                        10.40203400303521,
                        14,
                        new PdfPoint(1.813800375443253, 47.255096977735214),
                    },
                    new object[]
                    {
                        10.816881624275172,
                        81,
                        new PdfPoint(11.201209707090698, 49.55375920684947),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(39.732285598197045, 75.85998020600681),
                new object[]
                {
                    new object[]
                    {
                        11.018172527281603,
                        66,
                        new PdfPoint(34.43836768761709, 85.52303143204219),
                    },
                    new object[]
                    {
                        12.890615455027152,
                        98,
                        new PdfPoint(28.000497306010118, 81.20161949340638),
                    },
                    new object[]
                    {
                        13.53934377116391,
                        84,
                        new PdfPoint(42.195134616764996, 62.54652068319705),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(42.52668278329558, 59.630604107901206),
                new object[]
                {
                    new object[]
                    {
                        2.5857672288398494,
                        60,
                        new PdfPoint(43.017566199578106, 57.09185921458071),
                    },
                    new object[]
                    {
                        2.9347050381281665,
                        84,
                        new PdfPoint(42.195134616764996, 62.54652068319705),
                    },
                    new object[]
                    {
                        12.158848673678182,
                        35,
                        new PdfPoint(31.112041569584935, 55.44209995586027),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(11.91848255296697, 43.04840499795828),
                new object[]
                {
                    new object[]
                    {
                        4.269578278307316,
                        42,
                        new PdfPoint(13.34483441571791, 47.0726836958395),
                    },
                    new object[]
                    {
                        6.544777591222621,
                        81,
                        new PdfPoint(11.201209707090698, 49.55375920684947),
                    },
                    new object[]
                    {
                        7.6533011613775805,
                        7,
                        new PdfPoint(19.15551535520754, 45.53805856543256),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(45.884180750302804, 84.54782622509595),
                new object[]
                {
                    new object[]
                    {
                        6.567918486628291,
                        86,
                        new PdfPoint(52.44257355334966, 84.19422038812934),
                    },
                    new object[]
                    {
                        8.870332483989317,
                        36,
                        new PdfPoint(49.53804959253269, 92.63065004232082),
                    },
                    new object[]
                    {
                        11.487282614334951,
                        66,
                        new PdfPoint(34.43836768761709, 85.52303143204219),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(55.67904148326467, 33.58226552458229),
                new object[]
                {
                    new object[]
                    {
                        12.157613184301322,
                        96,
                        new PdfPoint(46.898436066324166, 25.173429288152636),
                    },
                    new object[]
                    {
                        12.363579030000482,
                        80,
                        new PdfPoint(63.98092485758994, 24.420561596263646),
                    },
                    new object[]
                    {
                        12.727500693064066,
                        87,
                        new PdfPoint(67.0752294529232, 39.24937886844233),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(62.05241890629847, 18.690682757200527),
                new object[]
                {
                    new object[]
                    {
                        4.919038574237582,
                        5,
                        new PdfPoint(65.40181734760978, 22.29324720161768),
                    },
                    new object[]
                    {
                        6.045713085692203,
                        80,
                        new PdfPoint(63.98092485758994, 24.420561596263646),
                    },
                    new object[]
                    {
                        9.721343867910239,
                        34,
                        new PdfPoint(52.89633110070122, 15.424096971651757),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(11.707658570636525, 65.12592360890554),
                new object[]
                {
                    new object[]
                    {
                        3.5995005257884185,
                        21,
                        new PdfPoint(15.143510163282482, 66.19892308162831),
                    },
                    new object[]
                    {
                        4.372050461043956,
                        97,
                        new PdfPoint(8.420107657979937, 68.0080815210619),
                    },
                    new object[]
                    {
                        6.033273067765282,
                        22,
                        new PdfPoint(16.49098129875618, 61.44894011847811),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(33.90020310829871, 47.7313194477634),
                new object[]
                {
                    new object[]
                    {
                        8.199389051021392,
                        35,
                        new PdfPoint(31.112041569584935, 55.44209995586027),
                    },
                    new object[]
                    {
                        10.334653304296424,
                        56,
                        new PdfPoint(29.725739779656745, 38.277277199189065),
                    },
                    new object[]
                    {
                        11.943086804002764,
                        49,
                        new PdfPoint(37.970724110053844, 36.503310666941424),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(42.15217638961487, 22.384017040998284),
                new object[]
                {
                    new object[]
                    {
                        5.505252183445487,
                        96,
                        new PdfPoint(46.898436066324166, 25.173429288152636),
                    },
                    new object[]
                    {
                        7.217278988769222,
                        53,
                        new PdfPoint(37.57869607184197, 27.967241358959505),
                    },
                    new object[]
                    {
                        7.829324949202796,
                        85,
                        new PdfPoint(49.61310425442538, 20.010647167526496),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(59.752335901407626, 57.2509255780393),
                new object[]
                {
                    new object[]
                    {
                        7.066352645437156,
                        23,
                        new PdfPoint(59.31519405414486, 64.30374393065894),
                    },
                    new object[]
                    {
                        11.540545516643135,
                        47,
                        new PdfPoint(62.56803790975437, 46.05914229645705),
                    },
                    new object[]
                    {
                        12.025621916687646,
                        20,
                        new PdfPoint(58.60508497220939, 69.22169822597527),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(97.9181308204627, 34.375867392622084),
                new object[]
                {
                    new object[]
                    {
                        0.9254860223406257,
                        31,
                        new PdfPoint(97.45369858932968, 33.575350634376086),
                    },
                    new object[]
                    {
                        9.456375526398574,
                        90,
                        new PdfPoint(93.45285655237589, 26.040139241642603),
                    },
                    new object[]
                    {
                        9.522138727011285,
                        79,
                        new PdfPoint(90.03637785608008, 39.71900281528301),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(68.84376255969894, 78.95185188531323),
                new object[]
                {
                    new object[]
                    {
                        5.704604915246542,
                        41,
                        new PdfPoint(74.16588992664359, 81.00550169213861),
                    },
                    new object[]
                    {
                        7.175761206917839,
                        58,
                        new PdfPoint(70.09436264721477, 86.01779473250726),
                    },
                    new object[]
                    {
                        11.512743286703174,
                        46,
                        new PdfPoint(67.53711275495073, 90.39020510040851),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(36.86160352788826, 13.756241445424322),
                new object[]
                {
                    new object[]
                    {
                        8.660213312722579,
                        13,
                        new PdfPoint(35.803655214742406, 5.16089154047169),
                    },
                    new object[]
                    {
                        10.05626008778861,
                        78,
                        new PdfPoint(44.866927314279856, 7.669932380951572),
                    },
                    new object[]
                    {
                        11.012079230414543,
                        88,
                        new PdfPoint(43.03278016511083, 4.6357991376672185),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(0.6887485300444696, 90.80035635580387),
                new object[]
                {
                    new object[]
                    {
                        15.52506007098662,
                        18,
                        new PdfPoint(14.158071542303697, 98.52077265281578),
                    },
                    new object[]
                    {
                        20.774155245195235,
                        68,
                        new PdfPoint(10.747687004702266, 72.62390777764313),
                    },
                    new object[]
                    {
                        24.067856284005092,
                        97,
                        new PdfPoint(8.420107657979937, 68.0080815210619),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(37.792743719353716, 82.90805938696141),
                new object[]
                {
                    new object[]
                    {
                        4.253224348519968,
                        66,
                        new PdfPoint(34.43836768761709, 85.52303143204219),
                    },
                    new object[]
                    {
                        9.939820266481515,
                        98,
                        new PdfPoint(28.000497306010118, 81.20161949340638),
                    },
                    new object[]
                    {
                        13.848241320909453,
                        10,
                        new PdfPoint(24.41810676553939, 79.31739454102991),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(28.425825069053435, 93.38421306346834),
                new object[]
                {
                    new object[]
                    {
                        9.896910900999169,
                        66,
                        new PdfPoint(34.43836768761709, 85.52303143204219),
                    },
                    new object[]
                    {
                        12.190015988477223,
                        98,
                        new PdfPoint(28.000497306010118, 81.20161949340638),
                    },
                    new object[]
                    {
                        14.62659185673608,
                        10,
                        new PdfPoint(24.41810676553939, 79.31739454102991),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(48.45367432788545, 3.2912155909772367),
                new object[]
                {
                    new object[]
                    {
                        5.585158765696226,
                        88,
                        new PdfPoint(43.03278016511083, 4.6357991376672185),
                    },
                    new object[]
                    {
                        5.660204489805277,
                        78,
                        new PdfPoint(44.866927314279856, 7.669932380951572),
                    },
                    new object[]
                    {
                        5.982992148213537,
                        43,
                        new PdfPoint(47.945049322341525, 9.252548969452246),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(41.681865022692435, 82.6132344499873),
                new object[]
                {
                    new object[]
                    {
                        7.806098392924558,
                        66,
                        new PdfPoint(34.43836768761709, 85.52303143204219),
                    },
                    new object[]
                    {
                        10.876229338256989,
                        86,
                        new PdfPoint(52.44257355334966, 84.19422038812934),
                    },
                    new object[]
                    {
                        12.7306029372109,
                        36,
                        new PdfPoint(49.53804959253269, 92.63065004232082),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(7.175621818812649, 4.404267177041543),
                new object[]
                {
                    new object[]
                    {
                        4.700030158625489,
                        17,
                        new PdfPoint(8.254965696884042, 8.97868520394496),
                    },
                    new object[]
                    {
                        6.884800728051085,
                        4,
                        new PdfPoint(0.7742372570967326, 6.938583013317256),
                    },
                    new object[]
                    {
                        8.81749451695195,
                        64,
                        new PdfPoint(15.264247773398054, 0.8939481426395335),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(67.06375058678753, 2.0706332172362285),
                new object[]
                {
                    new object[]
                    {
                        3.807121420419626,
                        76,
                        new PdfPoint(70.36073331446983, 3.9743344551363857),
                    },
                    new object[]
                    {
                        4.597343034838145,
                        92,
                        new PdfPoint(69.38884149074713, 6.0366785105100185),
                    },
                    new object[]
                    {
                        7.521027118921692,
                        48,
                        new PdfPoint(71.56130305322326, 8.098723357173876),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(21.17990951913106, 20.77232612239539),
                new object[]
                {
                    new object[]
                    {
                        2.949689300873121,
                        50,
                        new PdfPoint(18.3788032374675, 19.84795034896214),
                    },
                    new object[]
                    {
                        2.9500005402388694,
                        99,
                        new PdfPoint(24.103817108644833, 21.163820177769633),
                    },
                    new object[]
                    {
                        3.683726578350228,
                        38,
                        new PdfPoint(24.636942156010523, 22.044633809864933),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(83.4783024984307, 24.393830521827052),
                new object[]
                {
                    new object[]
                    {
                        3.0961634442512573,
                        8,
                        new PdfPoint(80.59123120177551, 25.51233282470693),
                    },
                    new object[]
                    {
                        3.7225758420518424,
                        15,
                        new PdfPoint(80.11998397458856, 25.99984035749021),
                    },
                    new object[]
                    {
                        3.768005736156072,
                        94,
                        new PdfPoint(80.69475374791655, 21.854199922464968),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(67.94892501031262, 58.40285832426694),
                new object[]
                {
                    new object[]
                    {
                        7.002211460552816,
                        59,
                        new PdfPoint(71.14819197800337, 52.17424240997747),
                    },
                    new object[]
                    {
                        10.457617375062723,
                        23,
                        new PdfPoint(59.31519405414486, 64.30374393065894),
                    },
                    new object[]
                    {
                        12.58403246559697,
                        30,
                        new PdfPoint(79.77968787046065, 62.69132229661655),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(70.30054809098338, 38.229402533712175),
                new object[]
                {
                    new object[]
                    {
                        3.3827550961350985,
                        87,
                        new PdfPoint(67.0752294529232, 39.24937886844233),
                    },
                    new object[]
                    {
                        6.676593719196517,
                        63,
                        new PdfPoint(72.74747856539602, 44.441442049411116),
                    },
                    new object[]
                    {
                        8.77548579112744,
                        74,
                        new PdfPoint(75.03458936225277, 45.61844875850991),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(6.666591704963154, 10.002159844611135),
                new object[]
                {
                    new object[]
                    {
                        1.8895587522745774,
                        17,
                        new PdfPoint(8.254965696884042, 8.97868520394496),
                    },
                    new object[]
                    {
                        5.383387494014664,
                        26,
                        new PdfPoint(11.228694290441743, 12.860145308234605),
                    },
                    new object[]
                    {
                        6.641185431873729,
                        4,
                        new PdfPoint(0.7742372570967326, 6.938583013317256),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(37.737895538821185, 56.99302071734109),
                new object[]
                {
                    new object[]
                    {
                        5.280595736713236,
                        60,
                        new PdfPoint(43.017566199578106, 57.09185921458071),
                    },
                    new object[]
                    {
                        6.8049464384399085,
                        35,
                        new PdfPoint(31.112041569584935, 55.44209995586027),
                    },
                    new object[]
                    {
                        7.120979010551258,
                        84,
                        new PdfPoint(42.195134616764996, 62.54652068319705),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(46.09269243297545, 55.527895517428725),
                new object[]
                {
                    new object[]
                    {
                        3.449983158993363,
                        60,
                        new PdfPoint(43.017566199578106, 57.09185921458071),
                    },
                    new object[]
                    {
                        8.028203793393617,
                        84,
                        new PdfPoint(42.195134616764996, 62.54652068319705),
                    },
                    new object[]
                    {
                        14.481241345005133,
                        71,
                        new PdfPoint(52.97863363490906, 68.26721122337617),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(53.61166019135128, 29.402010229671795),
                new object[]
                {
                    new object[]
                    {
                        7.933994891088919,
                        96,
                        new PdfPoint(46.898436066324166, 25.173429288152636),
                    },
                    new object[]
                    {
                        10.207161689017788,
                        85,
                        new PdfPoint(49.61310425442538, 20.010647167526496),
                    },
                    new object[]
                    {
                        11.287466296847745,
                        89,
                        new PdfPoint(49.42598643546282, 18.91930873569896),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(44.192808857614644, 34.74442504823799),
                new object[]
                {
                    new object[]
                    {
                        6.465911940748226,
                        49,
                        new PdfPoint(37.970724110053844, 36.503310666941424),
                    },
                    new object[]
                    {
                        9.469778598317067,
                        53,
                        new PdfPoint(37.57869607184197, 27.967241358959505),
                    },
                    new object[]
                    {
                        9.946073518332808,
                        96,
                        new PdfPoint(46.898436066324166, 25.173429288152636),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(87.18609487820613, 36.034544097040076),
                new object[]
                {
                    new object[]
                    {
                        4.658256014904542,
                        79,
                        new PdfPoint(90.03637785608008, 39.71900281528301),
                    },
                    new object[]
                    {
                        4.96949613369587,
                        72,
                        new PdfPoint(84.97188896260866, 40.483497230772926),
                    },
                    new object[]
                    {
                        5.333717325854641,
                        33,
                        new PdfPoint(84.80993603153726, 40.80973096746926),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(72.26195709778688, 9.598162829344847),
                new object[]
                {
                    new object[]
                    {
                        1.5467587665908336,
                        19,
                        new PdfPoint(70.71899728005727, 9.70650341683359),
                    },
                    new object[]
                    {
                        1.6550633887763093,
                        48,
                        new PdfPoint(71.56130305322326, 8.098723357173876),
                    },
                    new object[]
                    {
                        4.575911258396712,
                        92,
                        new PdfPoint(69.38884149074713, 6.0366785105100185),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(34.1612429164763, 19.22222038671292),
                new object[]
                {
                    new object[]
                    {
                        8.368264652666655,
                        2,
                        new PdfPoint(35.583597244228336, 27.46872033901967),
                    },
                    new object[]
                    {
                        9.389056282404074,
                        53,
                        new PdfPoint(37.57869607184197, 27.967241358959505),
                    },
                    new object[]
                    {
                        9.933696316427113,
                        38,
                        new PdfPoint(24.636942156010523, 22.044633809864933),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(71.38391153141247, 13.520311281559493),
                new object[]
                {
                    new object[]
                    {
                        3.871335866429107,
                        19,
                        new PdfPoint(70.71899728005727, 9.70650341683359),
                    },
                    new object[]
                    {
                        5.424489227001405,
                        48,
                        new PdfPoint(71.56130305322326, 8.098723357173876),
                    },
                    new object[]
                    {
                        7.745002512529356,
                        92,
                        new PdfPoint(69.38884149074713, 6.0366785105100185),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(98.937208620892, 67.26912373356589),
                new object[]
                {
                    new object[]
                    {
                        8.43618896742764,
                        37,
                        new PdfPoint(92.45607820110665, 72.66951564303699),
                    },
                    new object[]
                    {
                        14.035035311182662,
                        55,
                        new PdfPoint(84.9666796464414, 68.61319827025679),
                    },
                    new object[]
                    {
                        14.3852303261261,
                        39,
                        new PdfPoint(99.76982890678676, 52.90800974490838),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(89.80071239640789, 2.42574224851011),
                new object[]
                {
                    new object[]
                    {
                        5.724587358789773,
                        28,
                        new PdfPoint(90.87481745321885, 8.048659530077462),
                    },
                    new object[]
                    {
                        8.568651429497155,
                        54,
                        new PdfPoint(97.7460348272766, 5.634112362720456),
                    },
                    new object[]
                    {
                        13.056548614184756,
                        11,
                        new PdfPoint(98.12630738875605, 12.483472098628233),
                    },
                }
            },
            new object[]
            {
                new PdfPoint(4.9811550769006345, 11.349911276229651),
                new object[]
                {
                    new object[]
                    {
                        4.042344500583644,
                        17,
                        new PdfPoint(8.254965696884042, 8.97868520394496),
                    },
                    new object[]
                    {
                        6.095734130172781,
                        4,
                        new PdfPoint(0.7742372570967326, 6.938583013317256),
                    },
                    new object[]
                    {
                        6.4274841933807805,
                        26,
                        new PdfPoint(11.228694290441743, 12.860145308234605),
                    },
                }
            }
        };
        #endregion

        [Fact]
        public void BuildTree()
        {
            // wiki example
            var candidates = new[]
            {
                new PdfPoint(2, 3),
                new PdfPoint(4, 7),
                new PdfPoint(5, 4),
                new PdfPoint(7, 2),
                new PdfPoint(8, 1),
                new PdfPoint(9, 6)
            };

            // root
            KdTree kdTree = new KdTree(candidates);

            Assert.Equal(new PdfPoint(7, 2), kdTree.Root.Element, PointComparer);
            Assert.Equal(0, kdTree.Root.Depth);
            Assert.Equal(3, kdTree.Root.Index);
            Assert.False(kdTree.Root.IsLeaf);
            Assert.True(kdTree.Root.IsAxisCutX);

            // root -> left side
            Assert.Equal(new PdfPoint(5, 4), kdTree.Root.LeftChild.Element, PointComparer);
            Assert.Equal(1, kdTree.Root.LeftChild.Depth);
            Assert.Equal(2, kdTree.Root.LeftChild.Index);
            Assert.False(kdTree.Root.LeftChild.IsLeaf);
            Assert.False(kdTree.Root.LeftChild.IsAxisCutX);

            // root -> left side -> left side
            Assert.Equal(new PdfPoint(2, 3), kdTree.Root.LeftChild.LeftChild.Element, PointComparer);
            Assert.Equal(2, kdTree.Root.LeftChild.LeftChild.Depth);
            Assert.Equal(0, kdTree.Root.LeftChild.LeftChild.Index);
            Assert.True(kdTree.Root.LeftChild.LeftChild.IsLeaf);
            Assert.True(kdTree.Root.LeftChild.LeftChild.IsAxisCutX);

            Assert.Null(kdTree.Root.LeftChild.LeftChild.LeftChild);
            Assert.Null(kdTree.Root.LeftChild.LeftChild.RightChild);

            // root -> left side -> right side
            Assert.Equal(new PdfPoint(4, 7), kdTree.Root.LeftChild.RightChild.Element, PointComparer);
            Assert.Equal(2, kdTree.Root.LeftChild.RightChild.Depth);
            Assert.Equal(1, kdTree.Root.LeftChild.RightChild.Index);
            Assert.True(kdTree.Root.LeftChild.RightChild.IsLeaf);
            Assert.True(kdTree.Root.LeftChild.RightChild.IsAxisCutX);

            Assert.Null(kdTree.Root.LeftChild.RightChild.LeftChild);
            Assert.Null(kdTree.Root.LeftChild.RightChild.RightChild);

            // root -> right side
            Assert.Equal(new PdfPoint(9, 6), kdTree.Root.RightChild.Element, PointComparer);
            Assert.Equal(1, kdTree.Root.RightChild.Depth);
            Assert.Equal(5, kdTree.Root.RightChild.Index);
            Assert.False(kdTree.Root.RightChild.IsLeaf);
            Assert.False(kdTree.Root.RightChild.IsAxisCutX);

            // root -> right side -> left side
            Assert.Equal(new PdfPoint(8, 1), kdTree.Root.RightChild.LeftChild.Element, PointComparer);
            Assert.Equal(2, kdTree.Root.RightChild.LeftChild.Depth);
            Assert.Equal(4, kdTree.Root.RightChild.LeftChild.Index);
            Assert.True(kdTree.Root.RightChild.LeftChild.IsLeaf);
            Assert.True(kdTree.Root.RightChild.LeftChild.IsAxisCutX);

            Assert.Null(kdTree.Root.RightChild.LeftChild.RightChild);
            Assert.Null(kdTree.Root.RightChild.LeftChild.LeftChild);

            // root -> right side -> right side
            Assert.Null(kdTree.Root.RightChild.RightChild);
        }

        [Theory]
        [MemberData(nameof(DataTree1))]
        public void FindNearestNeighbour1(PdfPoint point, object[] expected)
        {
            KdTree kdTree = new KdTree(Tree1);

            var nn = kdTree.FindNearestNeighbour(point, Distances.Euclidean, out int index, out double distance);

            var expectedDistance = (double)expected[0];
            var expectedIndex = (int)expected[1];
            var expectedPoint = (PdfPoint)expected[2];

            Assert.Equal(expectedDistance, distance, PreciseDoubleComparer);
            Assert.Equal(expectedIndex, index);
            Assert.Equal(expectedPoint, nn, PointComparer);
        }

        [Theory]
        [MemberData(nameof(DataTreeK1))]
        public void FindNearestNeighbourK1(PdfPoint point, object[] expectedArr)
        {
            KdTree kdTree = new KdTree(Tree1);

            var nn = kdTree.FindNearestNeighbours(point, 3, Distances.Euclidean);

            for (int i = 0; i < 3; i++)
            {
                var expected = (object[])expectedArr[i];

                var expectedDistance = (double)expected[0];
                var expectedIndex = (int)expected[1];
                var expectedPoint = (PdfPoint)expected[2];

                var result = nn[i];

                Assert.Equal(expectedDistance, result.Item3, PreciseDoubleComparer);
                Assert.Equal(expectedIndex, result.Item2);
                Assert.Equal(expectedPoint, result.Item1, PointComparer);
            }
        }

        [Theory]
        [MemberData(nameof(DataTree2))]
        public void FindNearestNeighbour2(PdfPoint point, object[] expected)
        {
            KdTree kdTree = new KdTree(Tree2);

            var nn = kdTree.FindNearestNeighbour(point, Distances.Euclidean, out int index, out double distance);

            var expectedDistance = (double)expected[0];
            var expectedIndex = (int)expected[1];
            var expectedPoint = (PdfPoint)expected[2];

            Assert.Equal(expectedDistance, distance, PreciseDoubleComparer);
            Assert.Equal(expectedIndex, index);
            Assert.Equal(expectedPoint, nn, PointComparer);
        }

        [Theory]
        [MemberData(nameof(DataTreeK2))]
        public void FindNearestNeighbourK2(PdfPoint point, object[] expectedArr)
        {
            KdTree kdTree = new KdTree(Tree2);

            var nn = kdTree.FindNearestNeighbours(point, 3, Distances.Euclidean);

            for (int i = 0; i < 3; i++)
            {
                var expected = (object[])expectedArr[i];

                var expectedDistance = (double)expected[0];
                var expectedIndex = (int)expected[1];
                var expectedPoint = (PdfPoint)expected[2];

                var result = nn[i];

                Assert.Equal(expectedDistance, result.Item3, PreciseDoubleComparer);
                Assert.Equal(expectedIndex, result.Item2);
                Assert.Equal(expectedPoint, result.Item1, PointComparer);
            }
        }
    }
}
