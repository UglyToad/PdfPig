namespace UglyToad.PdfPig.Tests.Actions
{
    using System.Collections.Generic;
    using UglyToad.PdfPig.Actions;
    using UglyToad.PdfPig.Logging;
    using UglyToad.PdfPig.Tests.Tokens;
    using UglyToad.PdfPig.Tokens;

    public class ActionProviderTests
    {
        private static readonly ILog Log = new NoOpLog();
        private static readonly TestPdfTokenScanner Scanner = new TestPdfTokenScanner();

        private static PdfAction Parse(DictionaryToken actionDictionary)
        {
            var holder = new DictionaryToken(new Dictionary<NameToken, IToken>
            {
                { NameToken.A, actionDictionary }
            });

            Assert.True(ActionProvider.TryGetAction(holder, null, Scanner, Log, out var action));
            return action;
        }

        private static DictionaryToken ActionDictionary(NameToken subType, params (NameToken Key, IToken Value)[] entries)
        {
            var data = new Dictionary<NameToken, IToken> { { NameToken.S, subType } };
            foreach (var (key, value) in entries)
            {
                data[key] = value;
            }

            return new DictionaryToken(data);
        }

        private static DictionaryToken Dict(params (NameToken Key, IToken Value)[] entries)
        {
            var data = new Dictionary<NameToken, IToken>();
            foreach (var (key, value) in entries)
            {
                data[key] = value;
            }

            return new DictionaryToken(data);
        }

        [Fact]
        public void ParsesNamedAction()
        {
            var action = Parse(ActionDictionary(NameToken.Named, (NameToken.N, NameToken.Create("NextPage"))));

            var named = Assert.IsType<NamedAction>(action);
            Assert.Equal(ActionType.Named, named.Type);
            Assert.Equal("NextPage", named.Name);
        }

        [Fact]
        public void ParsesJavaScriptActionFromString()
        {
            var action = Parse(ActionDictionary(NameToken.JavaScript, (NameToken.Js, new StringToken("app.alert('hi');"))));

            var js = Assert.IsType<JavaScriptAction>(action);
            Assert.Equal(ActionType.JavaScript, js.Type);
            Assert.Equal("app.alert('hi');", js.JavaScript);
        }

        [Fact]
        public void ParsesLaunchActionWithFileAndNewWindow()
        {
            var action = Parse(ActionDictionary(NameToken.Launch,
                (NameToken.F, new StringToken("notepad.exe")),
                (NameToken.NewWindow, BooleanToken.True)));

            var launch = Assert.IsType<LaunchAction>(action);
            Assert.Equal(ActionType.Launch, launch.Type);
            Assert.Equal("notepad.exe", launch.FileName);
            Assert.Equal(OpenMode.NewWindow, launch.OpenInNewWindow);
        }

        [Fact]
        public void ParsesLaunchActionDefaultsToUserPreference()
        {
            var action = Parse(ActionDictionary(NameToken.Launch, (NameToken.F, new StringToken("a.pdf"))));

            var launch = Assert.IsType<LaunchAction>(action);
            Assert.Equal(OpenMode.UserPreference, launch.OpenInNewWindow);
        }

        [Fact]
        public void ParsesSoundActionWithFlags()
        {
            var action = Parse(ActionDictionary(NameToken.Sound,
                (NameToken.Volume, new NumericToken(0.5)),
                (NameToken.Synchronous, BooleanToken.True),
                (NameToken.Repeat, BooleanToken.True),
                (NameToken.Mix, BooleanToken.True)));

            var sound = Assert.IsType<SoundAction>(action);
            Assert.Equal(ActionType.Sound, sound.Type);
            Assert.Equal(0.5, sound.Volume);
            Assert.True(sound.Synchronous);
            Assert.True(sound.Repeat);
            Assert.True(sound.Mix);
        }

        [Fact]
        public void ParsesSoundActionWithDefaults()
        {
            var action = Parse(ActionDictionary(NameToken.Sound));

            var sound = Assert.IsType<SoundAction>(action);
            Assert.Equal(1.0, sound.Volume);
            Assert.False(sound.Synchronous);
            Assert.False(sound.Repeat);
            Assert.False(sound.Mix);
        }

        [Fact]
        public void ParsesMovieAction()
        {
            var movieAnnotation = Dict((NameToken.Subtype, NameToken.Create("Movie")));

            var action = Parse(ActionDictionary(NameToken.Movie,
                (NameToken.Annotation, movieAnnotation),
                (NameToken.T, new StringToken("My Movie")),
                (NameToken.Operation, NameToken.Create("Stop"))));

            var movie = Assert.IsType<MovieAction>(action);
            Assert.Equal(ActionType.Movie, movie.Type);
            Assert.NotNull(movie.TargetAnnotation);
            Assert.Equal("My Movie", movie.Title);
            Assert.Equal(MovieOperation.Stop, movie.Operation);
        }

        [Fact]
        public void ParsesMovieActionWithDefaults()
        {
            var action = Parse(ActionDictionary(NameToken.Movie));

            var movie = Assert.IsType<MovieAction>(action);
            Assert.Null(movie.TargetAnnotation);
            Assert.Null(movie.Title);
            Assert.Equal(MovieOperation.Play, movie.Operation);
        }

        [Fact]
        public void ParsesImportDataAction()
        {
            var action = Parse(ActionDictionary(NameToken.ImportData, (NameToken.F, new StringToken("data.fdf"))));

            var importData = Assert.IsType<ImportDataAction>(action);
            Assert.Equal(ActionType.ImportData, importData.Type);
            Assert.Equal("data.fdf", importData.FileName);
        }

        [Fact]
        public void ParsesResetFormAction()
        {
            var action = Parse(ActionDictionary(NameToken.ResetForm,
                (NameToken.Fields, new ArrayToken(new IToken[] { new StringToken("name"), new StringToken("email") })),
                (NameToken.Flags, new NumericToken(1))));

            var reset = Assert.IsType<ResetFormAction>(action);
            Assert.Equal(ActionType.ResetForm, reset.Type);
            Assert.Equal(1, reset.Flags);
            Assert.Equal(new[] { "name", "email" }, reset.Fields);
        }

        [Fact]
        public void ParsesResetFormActionWithDefaults()
        {
            var action = Parse(ActionDictionary(NameToken.ResetForm));

            var reset = Assert.IsType<ResetFormAction>(action);
            Assert.Equal(0, reset.Flags);
            Assert.Empty(reset.Fields);
        }

        [Fact]
        public void ParsesSubmitFormAction()
        {
            var action = Parse(ActionDictionary(NameToken.SubmitForm,
                (NameToken.F, new StringToken("https://example.com/submit")),
                (NameToken.Fields, new ArrayToken(new IToken[] { new StringToken("name") })),
                (NameToken.Flags, new NumericToken(4))));

            var submit = Assert.IsType<SubmitFormAction>(action);
            Assert.Equal(ActionType.SubmitForm, submit.Type);
            Assert.Equal("https://example.com/submit", submit.FileName);
            Assert.Equal(4, submit.Flags);
            Assert.Equal(new[] { "name" }, submit.Fields);
        }

        [Fact]
        public void ParsesHideActionDefaultsToHidden()
        {
            var action = Parse(ActionDictionary(NameToken.Hide, (NameToken.T, new StringToken("Field1"))));

            var hide = Assert.IsType<HideAction>(action);
            Assert.Equal(ActionType.Hide, hide.Type);
            Assert.True(hide.Hide);
            Assert.Equal(new[] { "Field1" }, hide.Fields);
        }

        [Fact]
        public void ParsesHideActionWithShowAndArrayTargets()
        {
            var action = Parse(ActionDictionary(NameToken.Hide,
                (NameToken.T, new ArrayToken(new IToken[] { new StringToken("Field1"), new StringToken("Field2") })),
                (NameToken.H, BooleanToken.False)));

            var hide = Assert.IsType<HideAction>(action);
            Assert.False(hide.Hide);
            Assert.Equal(new[] { "Field1", "Field2" }, hide.Fields);
        }

        [Fact]
        public void ParsesThreadAction()
        {
            var action = Parse(ActionDictionary(NameToken.Thread, (NameToken.F, new StringToken("article.pdf"))));

            var thread = Assert.IsType<ThreadAction>(action);
            Assert.Equal(ActionType.Thread, thread.Type);
            Assert.Equal("article.pdf", thread.FileName);
        }

        [Fact]
        public void ParsesTransAction()
        {
            var action = Parse(ActionDictionary(NameToken.Trans,
                (NameToken.Trans, Dict((NameToken.S, NameToken.Create("Wipe")), (NameToken.D, new NumericToken(2))))));

            var trans = Assert.IsType<TransAction>(action);
            Assert.Equal(ActionType.Trans, trans.Type);
            Assert.Equal("Wipe", trans.Style);
            Assert.Equal(2, trans.Duration);
        }

        [Fact]
        public void ParsesTransActionWithDefaults()
        {
            var action = Parse(ActionDictionary(NameToken.Trans));

            var trans = Assert.IsType<TransAction>(action);
            Assert.Equal("R", trans.Style);
            Assert.Equal(1, trans.Duration);
        }

        [Fact]
        public void ParsesSetOcgStateAction()
        {
            var layer1 = Dict((NameToken.Name, new StringToken("Layer 1")));
            var layer2 = Dict((NameToken.Name, new StringToken("Layer 2")));
            var layer3 = Dict((NameToken.Name, new StringToken("Layer 3")));

            var action = Parse(ActionDictionary(NameToken.SetOCGState,
                (NameToken.State, new ArrayToken(new IToken[] { NameToken.Off, layer1, layer2, NameToken.On, layer3 })),
                (NameToken.PreserveRB, BooleanToken.False)));

            var setState = Assert.IsType<SetOcgStateAction>(action);
            Assert.Equal(ActionType.SetOCGState, setState.Type);
            Assert.Equal(new[] { "Layer 1", "Layer 2" }, setState.Off);
            Assert.Equal(new[] { "Layer 3" }, setState.On);
            Assert.Empty(setState.Toggle);
            Assert.False(setState.PreserveRadioButtons);
        }

        [Fact]
        public void ParsesSetOcgStateActionWithDefaults()
        {
            var action = Parse(ActionDictionary(NameToken.SetOCGState));

            var setState = Assert.IsType<SetOcgStateAction>(action);
            Assert.Empty(setState.On);
            Assert.Empty(setState.Off);
            Assert.Empty(setState.Toggle);
            Assert.True(setState.PreserveRadioButtons);
        }

        [Fact]
        public void ParsesGoTo3DViewActionWithViewName()
        {
            var annotation = Dict((NameToken.Subtype, NameToken.Create("3D")));

            var action = Parse(ActionDictionary(NameToken.GoTo3DView,
                (NameToken.TA, annotation),
                (NameToken.V, new StringToken("DefaultView"))));

            var goTo3D = Assert.IsType<GoTo3DViewAction>(action);
            Assert.Equal(ActionType.GoTo3DView, goTo3D.Type);
            Assert.NotNull(goTo3D.TargetAnnotation);
            Assert.Equal("DefaultView", goTo3D.ViewName);
            Assert.Null(goTo3D.ViewIndex);
        }

        [Fact]
        public void ParsesGoTo3DViewActionWithViewIndex()
        {
            var action = Parse(ActionDictionary(NameToken.GoTo3DView, (NameToken.V, new NumericToken(2))));

            var goTo3D = Assert.IsType<GoTo3DViewAction>(action);
            Assert.Equal(2, goTo3D.ViewIndex);
            Assert.Null(goTo3D.ViewName);
            Assert.Null(goTo3D.TargetAnnotation);
        }

        [Fact]
        public void ParsesGoToDpAction()
        {
            var documentPart = Dict((NameToken.Type, NameToken.Create("DPart")));

            var action = Parse(ActionDictionary(NameToken.GoToDp, (NameToken.DpLower, documentPart)));

            var goToDp = Assert.IsType<GoToDpAction>(action);
            Assert.Equal(ActionType.GoToDp, goToDp.Type);
            Assert.NotNull(goToDp.DocumentPart);
        }

        [Fact]
        public void ParsesRenditionActionWithOperation()
        {
            var annotation = Dict(
                (NameToken.Subtype, NameToken.Create("Screen")),
                (NameToken.Nm, new StringToken("screen-1")));
            var rendition = Dict((NameToken.S, NameToken.Create("MR")));

            var action = Parse(ActionDictionary(NameToken.Rendition,
                (NameToken.Op, new NumericToken(4)),
                (NameToken.AN, annotation),
                (NameToken.R, rendition)));

            var renditionAction = Assert.IsType<RenditionAction>(action);
            Assert.Equal(ActionType.Rendition, renditionAction.Type);
            Assert.Equal(RenditionOperation.Play, renditionAction.Operation);
            Assert.NotNull(renditionAction.TargetAnnotation);
            Assert.NotNull(renditionAction.Rendition);
            Assert.Null(renditionAction.JavaScript);
        }

        [Fact]
        public void ParsesRenditionActionWithJavaScript()
        {
            var action = Parse(ActionDictionary(NameToken.Rendition,
                (NameToken.Js, new StringToken("this.media.play();"))));

            var renditionAction = Assert.IsType<RenditionAction>(action);
            Assert.Null(renditionAction.Operation);
            Assert.Equal("this.media.play();", renditionAction.JavaScript);
            Assert.Null(renditionAction.TargetAnnotation);
        }

        [Fact]
        public void ParsesRichMediaExecuteAction()
        {
            var annotation = Dict((NameToken.Subtype, NameToken.Create("RichMedia")));
            var instance = Dict((NameToken.Subtype, NameToken.Create("Video")));
            var command = Dict((NameToken.C, new StringToken("play")), (NameToken.A, new NumericToken(1)));

            var action = Parse(ActionDictionary(NameToken.RichMediaExecute,
                (NameToken.TA, annotation),
                (NameToken.Ti, instance),
                (NameToken.Cmd, command)));

            var richMedia = Assert.IsType<RichMediaExecuteAction>(action);
            Assert.Equal(ActionType.RichMediaExecute, richMedia.Type);
            Assert.NotNull(richMedia.TargetAnnotation);
            Assert.NotNull(richMedia.TargetInstance);
            Assert.Equal("play", richMedia.Command);
            var argument = Assert.IsType<NumericToken>(richMedia.CommandArguments);
            Assert.Equal(1, argument.Int);
        }

        [Fact]
        public void ParsesRichMediaExecuteActionWithCommandOnly()
        {
            var command = Dict((NameToken.C, new StringToken("stop")));

            var action = Parse(ActionDictionary(NameToken.RichMediaExecute,
                (NameToken.Cmd, command)));

            var richMedia = Assert.IsType<RichMediaExecuteAction>(action);
            Assert.Equal("stop", richMedia.Command);
            Assert.Null(richMedia.CommandArguments);
            Assert.Null(richMedia.TargetAnnotation);
            Assert.Null(richMedia.TargetInstance);
        }
    }
}
