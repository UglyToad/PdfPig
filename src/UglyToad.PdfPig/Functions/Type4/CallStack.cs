namespace UglyToad.PdfPig.Functions.Type4
{
    using System;

    /// <summary>
    /// A suspended procedure invocation: the procedure that was executing and the instruction to
    /// resume at once the called procedure returns.
    /// </summary>
    internal readonly struct CallFrame
    {
        public readonly int ProcedureIndex;
        public readonly int InstructionPointer;

        public CallFrame(int procedureIndex, int instructionPointer)
        {
            ProcedureIndex = procedureIndex;
            InstructionPointer = instructionPointer;
        }
    }

    /// <summary>
    /// The call stack used to execute nested Type 4 procedures without recursing.
    /// </summary>
    internal ref struct CallStack
    {
        private Span<CallFrame> frames;

        public CallStack(Span<CallFrame> initialBuffer)
        {
            frames = initialBuffer;
            Count = 0;
        }

        public int Count { get; private set; }

        public void Push(int procedureIndex, int instructionPointer)
        {
            if (Count == frames.Length)
            {
                Grow();
            }

            frames[Count++] = new CallFrame(procedureIndex, instructionPointer);
        }

        public void Pop(out int procedureIndex, out int instructionPointer)
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("The PostScript call stack is empty.");
            }

            ref readonly CallFrame frame = ref frames[--Count];
            procedureIndex = frame.ProcedureIndex;
            instructionPointer = frame.InstructionPointer;
        }

        private void Grow()
        {
            var newArray = new CallFrame[frames.Length == 0 ? 16 : frames.Length * 2];
            frames.CopyTo(newArray);
            frames = newArray;
        }
    }
}
