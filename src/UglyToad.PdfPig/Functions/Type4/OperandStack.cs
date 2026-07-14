namespace UglyToad.PdfPig.Functions.Type4
{
    using System;
    using System.Buffers;

    /// <summary>
    /// Typed operand stack for Type 4 function execution. Operates on a caller-provided
    /// (usually stack-allocated) buffer and only rents from the array pool if a program
    /// exceeds that capacity. Call <see cref="Dispose"/> when finished.
    /// </summary>
    internal ref struct OperandStack
    {
        private Span<Operand> items;
        private Operand[]? rented;

        public OperandStack(Span<Operand> initialBuffer)
        {
            items = initialBuffer;
            rented = null;
            Count = 0;
        }

        public int Count { get; private set; }

        public void Push(in Operand value)
        {
            if (Count == items.Length)
            {
                Grow(Count + 1);
            }
            items[Count++] = value;
        }

        public Operand Pop()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("The PostScript operand stack is empty.");
            }
            return items[--Count];
        }

        public readonly Operand Peek()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("The PostScript operand stack is empty.");
            }
            return items[Count - 1];
        }

        /// <summary>
        /// Returns the element <paramref name="n"/> positions below the top without removing it (n = 0 is the top).
        /// </summary>
        public readonly Operand FromTop(int n)
        {
            if (n < 0 || n >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(n));
            }
            return items[Count - 1 - n];
        }

        public double PopReal() => Pop().ToReal();

        public int PopInt()
        {
            Operand op = Pop();
            if (op.Kind != OperandKind.Integer)
            {
                throw new InvalidCastException("PopInt cannot be done as the value is not integer");
            }
            return op.AsInteger;
        }

        public bool PopStrictBoolean()
        {
            Operand op = Pop();
            if (op.Kind != OperandKind.Boolean)
            {
                throw new InvalidCastException("The operand is not a boolean.");
            }
            return op.AsBoolean;
        }

        /// <summary>
        /// Mirrors Convert.ToBoolean on the previously boxed values: numbers convert to true when non-zero.
        /// </summary>
        public bool PopConvertedBoolean()
        {
            Operand op = Pop();
            if (op.Kind == OperandKind.Procedure)
            {
                throw new InvalidCastException("A procedure cannot be converted to a boolean.");
            }
            return op.Value != 0;
        }

        /// <summary>
        /// Mirrors Convert.ToInt32 on the previously boxed values (banker's rounding for reals).
        /// </summary>
        public int PopConvertedInt()
        {
            Operand op = Pop();
            switch (op.Kind)
            {
                case OperandKind.Integer:
                    return op.AsInteger;
                case OperandKind.Real:
                    return checked((int)Math.Round(op.Value, MidpointRounding.ToEven));
                case OperandKind.Boolean:
                    return op.AsBoolean ? 1 : 0;
                default:
                    throw new InvalidCastException("A procedure cannot be converted to an integer.");
            }
        }

        public int PopProcedure()
        {
            Operand op = Pop();
            if (op.Kind != OperandKind.Procedure)
            {
                throw new InvalidCastException("The operand is not a procedure.");
            }
            return op.AsProcedureIndex;
        }

        public void Exchange()
        {
            if (Count < 2)
            {
                throw new InvalidOperationException("The PostScript operand stack is empty.");
            }
            (items[Count - 1], items[Count - 2]) = (items[Count - 2], items[Count - 1]);
        }

        /// <summary>
        /// Implements "copy": duplicates the top n items preserving order. Copies only the
        /// available items when n exceeds the stack size (matching the previous ToList().Take(n)).
        /// A non-positive n is a no-op.
        /// </summary>
        public void CopyTop(int n)
        {
            if (n <= 0)
            {
                return;
            }
            
            if (n > Count)
            {
                n = Count;
            }
            
            if (Count + n > items.Length)
            {
                Grow(Count + n);
            }
            
            items.Slice(Count - n, n).CopyTo(items.Slice(Count, n));
            Count += n;
        }

        /// <summary>
        /// Implements "roll": circularly shifts the top n items by j positions (j &gt; 0 rolls towards the top).
        /// </summary>
        public readonly void Roll(int n, int j)
        {
            if (j == 0)
            {
                return; // Nothing to do (checked before the rangecheck, as before)
            }
            
            if (n < 0)
            {
                throw new ArgumentException("rangecheck: " + n);
            }
            
            if (n == 0)
            {
                return;
            }
            
            if (n > Count)
            {
                throw new InvalidOperationException("The PostScript operand stack is empty.");
            }
            
            j = ((j % n) + n) % n;
            if (j == 0)
            {
                return;
            }
            
            Span<Operand> slice = items.Slice(Count - n, n);
            slice.Reverse();
            slice.Slice(0, j).Reverse();
            slice.Slice(j).Reverse();
        }

        /// <summary>
        /// Copies the stack contents, bottom first. For tests and diagnostics only.
        /// </summary>
        public readonly Operand[] ToArray()
        {
            return items.Slice(0, Count).ToArray();
        }

        public void Dispose()
        {
            Operand[]? toReturn = rented;
            rented = null;
            items = default;
            Count = 0;
            
            if (toReturn is not null)
            {
                ArrayPool<Operand>.Shared.Return(toReturn);
            }
        }

        private void Grow(int required)
        {
            int newCapacity = items.Length == 0 ? 16 : items.Length * 2;
            if (newCapacity < required)
            {
                newCapacity = required;
            }
            
            Operand[] newArray = ArrayPool<Operand>.Shared.Rent(newCapacity);
            items.Slice(0, Count).CopyTo(newArray);
            Operand[]? toReturn = rented;
            items = newArray;
            rented = newArray;
            
            if (toReturn is not null)
            {
                ArrayPool<Operand>.Shared.Return(toReturn);
            }
        }
    }
}
