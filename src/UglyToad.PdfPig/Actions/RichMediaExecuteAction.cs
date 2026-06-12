namespace UglyToad.PdfPig.Actions
{
    using Tokens;

    /// <summary>
    /// Rich-media-execute action (PDF 2.0), sending a command to the handler of a rich-media annotation.
    /// </summary>
    public sealed class RichMediaExecuteAction : PdfAction
    {
        /// <summary>
        /// The target rich-media annotation dictionary (the <c>TA</c> entry), or <see langword="null"/>
        /// if it is absent.
        /// </summary>
        public DictionaryToken? TargetAnnotation { get; }

        /// <summary>
        /// The target rich-media instance dictionary within the annotation's content (the <c>TI</c>
        /// entry), or <see langword="null"/> if it is absent.
        /// </summary>
        public DictionaryToken? TargetInstance { get; }

        /// <summary>
        /// The name of the command to execute (the <c>C</c> entry of the <c>CMD</c> command dictionary),
        /// or <see langword="null"/> if it is absent.
        /// </summary>
        public string? Command { get; }

        /// <summary>
        /// The argument(s) passed to the command (the <c>A</c> entry of the <c>CMD</c> command
        /// dictionary), or <see langword="null"/> if it is absent. May be a single value or an array.
        /// </summary>
        public IToken? CommandArguments { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="targetAnnotation">The target rich-media annotation dictionary.</param>
        /// <param name="targetInstance">The target rich-media instance dictionary.</param>
        /// <param name="command">The name of the command to execute.</param>
        /// <param name="commandArguments">The argument(s) passed to the command.</param>
        public RichMediaExecuteAction(
            DictionaryToken? targetAnnotation,
            DictionaryToken? targetInstance,
            string? command,
            IToken? commandArguments) : base(ActionType.RichMediaExecute)
        {
            TargetAnnotation = targetAnnotation;
            TargetInstance = targetInstance;
            Command = command;
            CommandArguments = commandArguments;
        }
    }
}
