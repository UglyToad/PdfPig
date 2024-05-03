namespace UglyToad.PdfPig.Functions.Type4
{
    using System.Globalization;

    /// <summary>
    /// Basic parser for Type 4 functions which is used to build up instruction sequences.
    /// </summary>
    internal sealed class InstructionSequenceBuilder : Parser.AbstractSyntaxHandler
    {
        private readonly InstructionSequence mainSequence = new InstructionSequence();
        private readonly Stack<InstructionSequence> seqStack = new Stack<InstructionSequence>();

        private InstructionSequenceBuilder()
        {
            this.seqStack.Push(this.mainSequence);
        }

        /// <summary>
        /// Returns the instruction sequence that has been build from the syntactic elements.
        /// </summary>
        /// <returns>the instruction sequence</returns>
        public InstructionSequence GetInstructionSequence()
        {
            return this.mainSequence;
        }

        /// <summary>
        /// Parses the given text into an instruction sequence representing a Type 4 function that can be executed.
        /// </summary>
        /// <param name="text">the Type 4 function text</param>
        /// <returns>the instruction sequence</returns>
        public static InstructionSequence Parse(string text)
        {
            InstructionSequenceBuilder builder = new InstructionSequenceBuilder();
            Parser.Parse(text, builder);
            return builder.GetInstructionSequence();
        }

        private InstructionSequence GetCurrentSequence()
        {
            return this.seqStack.Peek();
        }

        /// <inheritdoc/>
        public void Token(char[] text)
        {
            string val = string.Concat(text);
            Token(val);
        }

        public override void Token(string token)
        {
            if ("{".Equals(token))
            {
                InstructionSequence child = new InstructionSequence();
                GetCurrentSequence().AddProc(child);
                this.seqStack.Push(child);
            }
            else if ("}".Equals(token))
            {
                this.seqStack.Pop();
            }
            else
            {
                if (int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int tokenInt))
                {
                    GetCurrentSequence().AddInteger(tokenInt);
                    return;
                }

                if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double tokenFloat))
                {
                    GetCurrentSequence().AddReal(tokenFloat);
                    return;
                }

                //TODO Maybe implement radix numbers, such as 8#1777 or 16#FFFE

                GetCurrentSequence().AddName(token);
            }
        }
    }
}
