namespace UglyToad.PdfPig.Tests.Dla
{
    using System.Collections.Generic;
    using System.Linq;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.PageSegmenter;
    using UglyToad.PdfPig.DocumentLayoutAnalysis.WordExtractor;
    using UglyToad.PdfPig.Fonts.SystemFonts;
    using Xunit;

    public class DocstrumBoundingBoxesTests
    {
        public static IEnumerable<object[]> DataExtract => new[]
        {
            new object[]
            {
                "complex rotated.pdf",
                new string[]
                {
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse at ullamcorper libero. Cras sit amet dui laoreet tellus tristique commodo. Nam pretium id ligula ac malesuada. Mauris at lacinia magna. Curabitur ex lectus, lobortis lobortis turpis ac, congue aliquet quam. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Etiam consectetur sem et ex sagittis pretium. Praesent urna velit, mollis vitae ex vel, hendrerit finibus mauris. In at luctus orci. Nunc odio justo, rhoncus et euismod nec, bibendum vitae lacus. Aenean maximus sapien lacus, ut pellentesque tellus egestas eget. Nulla semper massa ut vehicula faucibus. Nam rhoncus, dolor consectetur pulvinar gravida, nisi sem luctus nibh, non venenatis nunc lorem et velit.",
                    "Morbi euismod mattis libero, nec porta neque aliquam et. Nunc sed felis id libero tincidunt malesuada et laoreet orci. Phasellus massa libero, cursus imperdiet rhoncus quis, consequat eu eros. Nullam imperdiet felis sed ligula faucibus bibendum. Vestibulum rhoncus metus eu congue cursus. Maecenas vulputate dignissim dolor a iaculis. Praesent vel diam congue, dapibus lorem nec, viverra dolor. Vestibulum quis odio a risus semper aliquam.",
                    "Mauris tincidunt massa id lorem consectetur, in vestibulum nibh feugiat. Quisque eget commodo tortor. Duis iaculis, urna eget porttitor consectetur, metus lacus tempus urna, vehicula facilisis quam est scelerisque dui. Vestibulum imperdiet, tellus vel vulputate pretium, dolor mauris aliquet erat, sit amet fringilla nisi dui ut felis.",
                    "Cras gravida vel risus sit amet sagittis. Vestibulum et purus pretium, accumsan turpis ac, consectetur augue. Nam viverra purus in urna mollis eleifend. Donec non imperdiet justo. In commodo tortor in diam feugiat, eget placerat augue posuere. Donec justo arcu, rutrum in massa quis, dictum condimentum risus. Nunc euismod et dolor at elementum. Duis pretium risus rhoncus mauris pulvinar, vel semper elit tempus. Quisque imperdiet, odio et hendrerit laoreet, justo dolor blandit sapien, ut mollis risus elit sit amet lacus. Vivamus id tortor eleifend, gravida tortor vitae, dignissim mauris. Integer efficitur ac neque id venenatis. Suspendisse pharetra neque sit amet ornare convallis. Sed eget eros dignissim risus eleifend elementum. Duis non bibendum ipsum.",
                    "Morbi euismod mattis libero, nec porta neque aliquam et. Nunc sed felis id libero tincidunt malesuada et laoreet orci. Phasellus massa libero, cursus imperdiet rhoncus quis, consequat eu eros. Nullam imperdiet felis sed ligula faucibus bibendum. Vestibulum rhoncus metus eu congue cursus. Maecenas vulputate dignissim dolor a iaculis. Praesent vel diam congue, dapibus lorem nec, viverra dolor. Vestibulum quis odio a risus semper aliquam."
                }
            },
            new object[]
            {
                "90 180 270 rotated.pdf",
                new string[]
                {
                    "Morbi euismod mattis libero, nec porta neque aliquam et. Nunc sed felis id libero tincidunt malesuada et laoreet orci. Phasellus massa libero, cursus imperdiet rhoncus quis, consequat eu eros. Nullam imperdiet felis sed ligula faucibus bibendum. Vestibulum rhoncus metus eu congue cursus. Maecenas vulputate dignissim dolor a iaculis. Praesent vel diam congue, dapibus lorem nec, viverra dolor. Vestibulum quis odio a risus semper aliquam.",
                    "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Suspendisse at ullamcorper libero. Cras sit amet dui laoreet tellus tristique commodo. Nam pretium id ligula ac malesuada. Mauris at lacinia magna. Curabitur ex lectus, lobortis lobortis turpis ac, congue aliquet quam. Class aptent taciti sociosqu ad litora torquent per conubia nostra, per inceptos himenaeos. Etiam consectetur sem et ex sagittis pretium. Praesent urna velit, mollis vitae ex vel, hendrerit finibus mauris. In at luctus orci. Nunc odio justo, rhoncus et euismod nec, bibendum vitae lacus. Aenean maximus sapien lacus, ut pellentesque tellus egestas eget. Nulla semper massa ut vehicula faucibus. Nam rhoncus, dolor consectetur pulvinar gravida, nisi sem luctus nibh, non venenatis nunc lorem et velit.",
                    "Cras gravida vel risus sit amet sagittis. Vestibulum et purus pretium, accumsan turpis ac, consectetur augue. Nam viverra purus in urna mollis eleifend. Donec non imperdiet justo. In commodo tortor in diam feugiat, eget placerat augue posuere. Donec justo arcu, rutrum in massa quis, dictum condimentum risus. Nunc euismod et dolor at elementum. Duis pretium risus rhoncus mauris pulvinar, vel semper elit tempus. Quisque imperdiet, odio et hendrerit laoreet, justo dolor blandit sapien, ut mollis risus elit sit amet lacus. Vivamus id tortor eleifend, gravida tortor vitae, dignissim mauris. Integer efficitur ac neque id venenatis. Suspendisse pharetra neque sit amet ornare convallis. Sed eget eros dignissim risus eleifend elementum. Duis non bibendum ipsum.",
                }
            },
            new object[]
            {
                "Random 2 Columns Lists Hyph - Justified.pdf",
                new string[]
                {
                    "Random Big Title",
                    "Lorem Ipsum text with lists Lorem ipsum dolor sit amet, consectetur adipiscing elit. In sodales gravida felis, in rhoncus velit rutrum at. Curabitur hendrerit dapibus nulla, ut hendrerit diam imperdiet quis. Pellentesque id neque ali- quam, pulvinar neque in, vulputate elit. Pel- lentesque ut erat sit amet massa suscipit ullamcor- per. Sed porttitor viverra convallis. Duis vitae sem- per metus. Pellentesque eros purus, egestas eget velit eget, elementum aliquet velit. Suspendisse potenti. Nulla vitae massa rutrum, blandit erat vi- tae, aliquet arcu.",
                    "Aenean feugiat leo sed enim sodales vehicula. Sus- pendisse tempus hendrerit magna sagittis dictum. Duis ultrices dapibus egestas. Cras eu felis eu lectus suscipit pharetra at at lacus. Nulla facilisi. Proin in- terdum faucibus elit nec rhoncus. Proin sodaless metus sed tincidunt hendrerit.",
                    "Donec ultricies cursus odio sed rutrum. Nam ven- enatis metus vitae elementum scelerisque. Ali- quam tempor sapien at turpis posuere eleifend. Sed placerat posuere nunc vel efficitur. Quisque auctor felis vel lectus dictum fringilla. Quisque vo- lutpat pulvinar© elit. Aliquam ultrices feugiat ali- quam. Vestibulum ante ipsum primis in faucibus orci luctus et ultrices posuere cubilia Curae; Sus- pendisse imperdiet ex lorem, porta bibendum pu- rus ultricies id.",
                    "Integer vel lacus sapien. Nam sodales ante eu risus facilisis placerat. Aliquam suscipit pulvinar ultricies. Aenean pulvinar, ex ac fermentum egestas, erat nisi feugiat velit, vitae suscipit tellus odio vitae quam. Morbi elementum sem in elit posuere, non",
                    "•","•","•","•",
                    "Duis leo enim, convallis sit amet orci eget, condimentum mattis mi ; Etiam dolor erat, maximus nec mi sed, con- vallis convallis orci ; Morbi viverra diam in diam cursus, vitae aliquet velit tempus ; Donec at nisi fermentum, ultricies odio eget, egestas massa at nisi fermentum, ul- tricies odio eget, egestas massa.",
                    "rhoncus magna fringilla. Phasellus cursus in dolor laoreet rutrum. Curabitur tincidunt risus ullamcor- per, vehicula velit at, pulvinar metus.",
                    "Donec quis ante leo. Vivamus pharetra, nisl ac vehi- cula tempor, tellus lacus aliquam sapien, eu congue nibh quam sit amet odio. Quisque metus arcu, sem- per nec consequat eu, pellentesque vel sem. Sed purus risus, tincidunt¹ sit amet dictum vitae, euis- mod id nibh. Praesent ultrices libero quis enim porta, sit amet pellentesque augue pretium. Viva- mus nec molestie nunc. Donec finibus enim nec tel- lus laoreet elementum. Curabitur efficitur placerat dolor et semper.",
                    "Morbi laoreet dui eu tortor luctus, nec ultrices do- lor ullamcorper. Ut gravida sed nisl a efficitur. In tincidunt orci a condimentum semper. Suspendisse scelerisque fermentum lacinia. Vestibulum sit amet ornare tellus, aliquet euismod mauris. Cras suscipit venenatis ultrices. Sed diam erat, aliquet a tellus ut, viverra 12º ongue magna. Cras id justo tortor. Mauris in tortor vulputate, pellentesque nisl ac, facilisis ligula. Class aptent taciti² sociosqu ad li- tora torquent per conubia nostra³, per inceptos himenaeos. Aliquam eget dolor turpis. Mauris id molestie tellus. Sed elementum molestie nisi, at ali- quet sem vehicula nec. Morbi tempus nulla enim, a vulputate magna €51 luctus £66 eu. Fusce sodales, libero quis suscipit ultrices, metus erat auctor urna, sit amet dictum arcu tortor eu metus.",
                    "Morbi vestibulum varius ipsum nec molestie. Proin auctor efficitur diam ut luctus. Phasellus cursus maximus ultricies. Mauris eu neque ut sem semper tempus. Curabitur non lorem eu nunc lobortis vi- verra at in diam. Pellentesque euismod purus a leo lobortis tempor. Maecenas mollis ligula at sem sus- cipit fringilla. Mauris sollicitudin tincidunt lectus id tempor. Etiam ut nisi est.",
                    "1. Ut volutpat, velit at interdum consectetur, nisl lorem consequat mauris, feugiat dignissim tellus massa ut nisl. 2. Praesent at est nisi. Pellentesque rutrum lorem sed dui accumsan gravida. 3. Pellentesque dictum nisl vitae urna luctus, congue pulvinar mi congue.",
                }
            },
            new object[]
            {
                "no vertical distance.pdf",
                new string[]
                {
                    "Document’s second line left aligned.",
                    "Document’s first line right aligned."
                }
            },
            new object[]
            {
                "no horizontal distance.pdf",
                new string[]
                {
                    "First. Second."
                }
            }
        };

        [SkippableTheory]
        [MemberData(nameof(DataExtract))]
        public void GetBlocks(string name, string[] expected)
        {
            if (name == "90 180 270 rotated.pdf")
            {
                // The 'TimesNewRomanPSMT' font is used by this particular document. Thus, results cannot be trusted on
                // platforms where this font isn't generally available (e.g. OSX, Linux, etc.), so we skip it!
                var font = SystemFontFinder.Instance.GetTrueTypeFont("TimesNewRomanPSMT");
                Skip.If(font == null, "Skipped because the font TimesNewRomanPSMT could not be found in the execution environment.");
            }

            var options = new DocstrumBoundingBoxes.DocstrumBoundingBoxesOptions() { LineSeparator = " " };
            using (var document = PdfDocument.Open(DlaHelper.GetDocumentPath(name)))
            {
                var page = document.GetPage(1);
                var words = NearestNeighbourWordExtractor.Instance.GetWords(page.Letters);
                var blocks = DocstrumBoundingBoxes.Instance.GetBlocks(words, options);

                Assert.Equal(expected.Length, blocks.Count);
                var orderedBlocks = blocks.OrderBy(b => b.BoundingBox.BottomLeft.X)
                                          .ThenByDescending(b => b.BoundingBox.BottomLeft.Y).ToList();

                for (int i = 0; i < orderedBlocks.Count; i++)
                {
                    Assert.Equal(expected[i], orderedBlocks[i].Text);
                }
            }
        }
    }
}