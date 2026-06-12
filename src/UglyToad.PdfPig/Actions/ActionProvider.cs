namespace UglyToad.PdfPig.Actions
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Core;
    using Logging;
    using Outline.Destinations;
    using Parser.Parts;
    using Tokenization.Scanner;
    using Tokens;
    using Util;

    internal static class ActionProvider
    {
        /// <summary>
        /// Get an action (A) from dictionary. If GoTo, GoToR or GoToE, also fetches the action destination.
        /// </summary>
        internal static bool TryGetAction(
            DictionaryToken dictionary,
            NamedDestinations namedDestinations,
            IPdfTokenScanner pdfScanner,
            ILog log,
            [NotNullWhen(true)] out PdfAction? result)
        {
            result = null;

            if (!dictionary.TryGet(NameToken.A, pdfScanner, out DictionaryToken? actionDictionary))
            {
                return false;
            }

            if (!actionDictionary.TryGet(NameToken.S, pdfScanner, out NameToken? actionType))
            {
                throw new PdfDocumentFormatException($"No action type (/S) specified for action: {actionDictionary}.");
            }

            if (actionType.Equals(NameToken.GoTo))
            {
                // For GoTo, D(estination) is required
                if (DestinationProvider.TryGetDestination(actionDictionary,
                        NameToken.D,
                        namedDestinations,
                        pdfScanner,
                        log,
                        false,
                        out var destination))
                {
                    result = new GoToAction(destination);
                    return true;
                }
            }
            else if (actionType.Equals(NameToken.GoToR))
            {
                // For GoToR, F(ile) and D(estination) are required
                if (actionDictionary.TryGetOptionalStringDirect(NameToken.F, pdfScanner, out var filename)
                    && DestinationProvider.TryGetDestination(actionDictionary,
                        NameToken.D,
                        namedDestinations,
                        pdfScanner,
                        log,
                        true,
                        out var destination))
                {
                    result = new GoToRAction(destination, filename);
                    return true;
                }
            }
            else if (actionType.Equals(NameToken.GoToE))
            {
                // For GoToE, D(estination) is required
                if (DestinationProvider.TryGetDestination(actionDictionary,
                        NameToken.D,
                        namedDestinations,
                        pdfScanner,
                        log,
                        true,
                        out var destination))
                {
                    // F(ile specification) is optional
                    if (!actionDictionary.TryGetOptionalStringDirect(NameToken.F,
                            pdfScanner,
                            out var fileSpecification))
                    {
                        fileSpecification = null;
                    }

                    result = new GoToEAction(destination, fileSpecification!);
                    return true;
                }
            }
            else if (actionType.Equals(NameToken.Uri))
            {
                if (!actionDictionary.TryGetOptionalStringDirect(NameToken.Uri, pdfScanner, out var uri))
                {
                    uri = null;
                }
                result = new UriAction(uri);
                return true;
            }
            else if (actionType.Equals(NameToken.Launch))
            {
                if (!actionDictionary.TryGetOptionalStringDirect(NameToken.F, pdfScanner, out var fileName))
                {
                    fileName = null;
                }

                result = new LaunchAction(fileName, GetOpenMode(actionDictionary));
                return true;
            }
            else if (actionType.Equals(NameToken.Thread))
            {
                if (!actionDictionary.TryGetOptionalStringDirect(NameToken.F, pdfScanner, out var fileName))
                {
                    fileName = null;
                }

                result = new ThreadAction(fileName);
                return true;
            }
            else if (actionType.Equals(NameToken.Named))
            {
                string name = string.Empty;
                if (actionDictionary.TryGet(NameToken.N, pdfScanner, out NameToken? namedToken))
                {
                    name = namedToken.Data;
                }
                else if (actionDictionary.TryGetOptionalStringDirect(NameToken.N, pdfScanner, out var namedString))
                {
                    name = namedString;
                }

                result = new NamedAction(name);
                return true;
            }
            else if (actionType.Equals(NameToken.JavaScript))
            {
                if (!actionDictionary.TryGetOptionalStringDirect(NameToken.Js, pdfScanner, out var javaScript))
                {
                    javaScript = string.Empty;
                }

                result = new JavaScriptAction(javaScript);
                return true;
            }
            else if (actionType.Equals(NameToken.Sound))
            {
                result = new SoundAction(
                    GetVolume(actionDictionary),
                    actionDictionary.GetBooleanOrDefault(NameToken.Synchronous, false),
                    actionDictionary.GetBooleanOrDefault(NameToken.Repeat, false),
                    actionDictionary.GetBooleanOrDefault(NameToken.Mix, false));
                return true;
            }
            else if (actionType.Equals(NameToken.Movie))
            {
                result = GetMovieAction(actionDictionary, pdfScanner);
                return true;
            }
            else if (actionType.Equals(NameToken.ImportData))
            {
                if (!actionDictionary.TryGetOptionalStringDirect(NameToken.F, pdfScanner, out var fileName))
                {
                    fileName = null;
                }

                result = new ImportDataAction(fileName);
                return true;
            }
            else if (actionType.Equals(NameToken.ResetForm))
            {
                result = new ResetFormAction(
                    GetFieldNames(actionDictionary, NameToken.Fields, pdfScanner),
                    actionDictionary.GetIntOrDefault(NameToken.Flags, 0));
                return true;
            }
            else if (actionType.Equals(NameToken.SubmitForm))
            {
                if (!actionDictionary.TryGetOptionalStringDirect(NameToken.F, pdfScanner, out var fileName))
                {
                    fileName = null;
                }

                result = new SubmitFormAction(
                    fileName,
                    GetFieldNames(actionDictionary, NameToken.Fields, pdfScanner),
                    actionDictionary.GetIntOrDefault(NameToken.Flags, 0));
                return true;
            }
            else if (actionType.Equals(NameToken.Hide))
            {
                result = new HideAction(
                    GetFieldNames(actionDictionary, NameToken.T, pdfScanner),
                    actionDictionary.GetBooleanOrDefault(NameToken.H, true));
                return true;
            }
            else if (actionType.Equals(NameToken.Trans))
            {
                result = GetTransAction(actionDictionary, pdfScanner);
                return true;
            }
            else if (actionType.Equals(NameToken.SetOCGState))
            {
                result = GetSetOcgStateAction(actionDictionary, pdfScanner);
                return true;
            }
            else if (actionType.Equals(NameToken.GoTo3DView))
            {
                result = GetGoTo3DViewAction(actionDictionary, pdfScanner);
                return true;
            }
            else if (actionType.Equals(NameToken.GoToDp))
            {
                // NameToken.Dp is the (different, case-sensitive) "DP" name, using NameToken.DpLower ("Dp").
                actionDictionary.TryGet(NameToken.DpLower, pdfScanner, out DictionaryToken? documentPart);
                result = new GoToDpAction(documentPart);
                return true;
            }
            else if (actionType.Equals(NameToken.Rendition))
            {
                result = GetRenditionAction(actionDictionary, pdfScanner);
                return true;
            }
            else if (actionType.Equals(NameToken.RichMediaExecute))
            {
                result = GetRichMediaExecuteAction(actionDictionary, pdfScanner);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Reads a movie action's target movie annotation, title and operation.
        /// </summary>
        private static MovieAction GetMovieAction(DictionaryToken actionDictionary, IPdfTokenScanner pdfScanner)
        {
            actionDictionary.TryGet(NameToken.Annotation, pdfScanner, out DictionaryToken? targetAnnotation);

            if (!actionDictionary.TryGetOptionalStringDirect(NameToken.T, pdfScanner, out var title))
            {
                title = null;
            }

            var operation = MovieOperation.Play;
            if (actionDictionary.TryGet(NameToken.Operation, pdfScanner, out NameToken? operationToken))
            {
                if (operationToken.Equals(NameToken.Stop))
                {
                    operation = MovieOperation.Stop;
                }
                else if (operationToken.Equals(NameToken.Pause))
                {
                    operation = MovieOperation.Pause;
                }
                else if (operationToken.Equals(NameToken.Resume))
                {
                    operation = MovieOperation.Resume;
                }
            }

            return new MovieAction(targetAnnotation, title, operation);
        }

        /// <summary>
        /// Reads a rendition action's rendition, target screen annotation, operation and JavaScript.
        /// </summary>
        private static RenditionAction GetRenditionAction(DictionaryToken actionDictionary, IPdfTokenScanner pdfScanner)
        {
            actionDictionary.TryGet(NameToken.R, pdfScanner, out DictionaryToken? rendition);
            actionDictionary.TryGet(NameToken.AN, pdfScanner, out DictionaryToken? targetAnnotation);

            RenditionOperation? operation = null;
            if (actionDictionary.GetObjectOrDefault(NameToken.Op) is NumericToken operationToken
                && operationToken.Int >= 0 && operationToken.Int <= 4)
            {
                operation = (RenditionOperation)operationToken.Int;
            }

            if (!actionDictionary.TryGetOptionalStringDirect(NameToken.Js, pdfScanner, out var javaScript))
            {
                javaScript = null;
            }

            return new RenditionAction(rendition, targetAnnotation, operation, javaScript);
        }

        /// <summary>
        /// Reads a rich-media-execute action's target annotation, target instance and command.
        /// </summary>
        private static RichMediaExecuteAction GetRichMediaExecuteAction(DictionaryToken actionDictionary, IPdfTokenScanner pdfScanner)
        {
            actionDictionary.TryGet(NameToken.TA, pdfScanner, out DictionaryToken? targetAnnotation);
            actionDictionary.TryGet(NameToken.Ti, pdfScanner, out DictionaryToken? targetInstance);

            string? command = null;
            IToken? commandArguments = null;
            if (actionDictionary.TryGet(NameToken.Cmd, pdfScanner, out DictionaryToken? cmd))
            {
                if (!cmd.TryGetOptionalStringDirect(NameToken.C, pdfScanner, out command))
                {
                    command = null;
                }

                if (cmd.TryGet(NameToken.A, out var argumentsToken))
                {
                    commandArguments = argumentsToken;
                }
            }

            return new RichMediaExecuteAction(targetAnnotation, targetInstance, command, commandArguments);
        }

        /// <summary>
        /// Reads a transition action's <c>Trans</c> transition dictionary into a <see cref="TransAction"/>.
        /// </summary>
        private static TransAction GetTransAction(DictionaryToken actionDictionary, IPdfTokenScanner pdfScanner)
        {
            string style = "R";
            double duration = 1.0;

            if (actionDictionary.TryGet(NameToken.Trans, pdfScanner, out DictionaryToken? transition))
            {
                if (transition.TryGet(NameToken.S, pdfScanner, out NameToken? styleToken))
                {
                    style = styleToken.Data;
                }

                if (transition.GetObjectOrDefault(NameToken.D) is NumericToken durationToken)
                {
                    duration = durationToken.Double;
                }
            }

            return new TransAction(style, duration);
        }

        /// <summary>
        /// Reads a set-OCG-state action's <c>State</c> array, grouping the referenced optional content
        /// group names by the preceding <c>ON</c>, <c>OFF</c> or <c>Toggle</c> mode.
        /// </summary>
        private static SetOcgStateAction GetSetOcgStateAction(DictionaryToken actionDictionary, IPdfTokenScanner pdfScanner)
        {
            bool preserveRadioButtons = actionDictionary.GetBooleanOrDefault(NameToken.PreserveRB, true);
            if (!actionDictionary.TryGet(NameToken.State, pdfScanner, out ArrayToken? state))
            {
                return new SetOcgStateAction([], [], [], preserveRadioButtons);
            }

            var on = new List<string>();
            var off = new List<string>();
            var toggle = new List<string>();
            List<string>? current = null;
            foreach (var item in state.Data)
            {
                if (item is NameToken mode)
                {
                    if (mode.Equals(NameToken.On))
                    {
                        current = on;
                    }
                    else if (mode.Equals(NameToken.Off))
                    {
                        current = off;
                    }
                    else if (mode.Equals(NameToken.Toggle))
                    {
                        current = toggle;
                    }

                    continue;
                }

                if (current is null)
                {
                    continue;
                }

                if (DirectObjectFinder.TryGet(item, pdfScanner, out DictionaryToken? group)
                    && group.TryGetOptionalStringDirect(NameToken.Name, pdfScanner, out var name))
                {
                    current.Add(name);
                }
            }

            return new SetOcgStateAction(on, off, toggle, preserveRadioButtons);
        }

        /// <summary>
        /// Reads a go-to-3D-view action's target annotation and view specifier.
        /// </summary>
        private static GoTo3DViewAction GetGoTo3DViewAction(DictionaryToken actionDictionary, IPdfTokenScanner pdfScanner)
        {
            actionDictionary.TryGet(NameToken.TA, pdfScanner, out DictionaryToken? targetAnnotation);

            string? viewName = null;
            int? viewIndex = null;

            switch (actionDictionary.GetObjectOrDefault(NameToken.V))
            {
                case NameToken viewNameToken:
                    viewName = viewNameToken.Data;
                    break;
                case StringToken viewStringToken:
                    viewName = viewStringToken.Data;
                    break;
                case HexToken viewHexToken:
                    viewName = viewHexToken.Data;
                    break;
                case NumericToken viewIndexToken:
                    viewIndex = viewIndexToken.Int;
                    break;
            }

            return new GoTo3DViewAction(targetAnnotation, viewName, viewIndex);
        }

        /// <summary>
        /// Reads the <c>NewWindow</c> flag of a launch action into an <see cref="OpenMode"/>.
        /// </summary>
        private static OpenMode GetOpenMode(DictionaryToken actionDictionary)
        {
            if (actionDictionary.GetObjectOrDefault(NameToken.NewWindow) is BooleanToken newWindow)
            {
                return newWindow.Data ? OpenMode.NewWindow : OpenMode.SameWindow;
            }

            return OpenMode.UserPreference;
        }

        /// <summary>
        /// Reads the <c>Volume</c> of a sound action, clamped to the range -1.0 to 1.0 with a default of 1.0.
        /// </summary>
        private static double GetVolume(DictionaryToken actionDictionary)
        {
            if (actionDictionary.GetObjectOrDefault(NameToken.Volume) is not NumericToken volumeToken)
            {
                return 1;
            }

            var volume = volumeToken.Double;
            return volume < -1 || volume > 1 ? 1 : volume;
        }

        /// <summary>
        /// Reads a field list (<c>Fields</c> or <c>T</c>) collecting the entries that are text strings.
        /// Entries given as annotation/field references are ignored.
        /// </summary>
        private static IReadOnlyList<string> GetFieldNames(
            DictionaryToken actionDictionary,
            NameToken name,
            IPdfTokenScanner pdfScanner)
        {
            var result = new List<string>();

            if (actionDictionary.TryGet(name, pdfScanner, out ArrayToken? array))
            {
                foreach (var item in array.Data)
                {
                    switch (item)
                    {
                        case StringToken stringToken:
                            result.Add(stringToken.Data);
                            break;
                        case HexToken hexToken:
                            result.Add(hexToken.Data);
                            break;
                    }
                }
            }
            else if (actionDictionary.TryGetOptionalStringDirect(name, pdfScanner, out var single))
            {
                result.Add(single);
            }

            return result;
        }
    }
}
