namespace UglyToad.PdfPig.Functions.Type4
{
    using System;

    /// <summary>
    /// A compiled Type 4 (PostScript calculator) function: a flat instruction array per
    /// procedure, executed against a typed operand stack without boxing or per-evaluation
    /// allocations. Immutable after construction and safe for concurrent use.
    /// </summary>
    internal sealed class Type4Program
    {
        private readonly Instruction[][] procedures;

        /// <param name="procedures">procedures[0] is the main program; nested procedures are referenced by index.</param>
        internal Type4Program(Instruction[][] procedures)
        {
            this.procedures = procedures;
        }

        public void Execute(ref OperandStack stack)
        {
            ExecuteProcedure(0, ref stack);
        }

        private void ExecuteProcedure(int procedureIndex, ref OperandStack stack)
        {
            Instruction[] code = procedures[procedureIndex];
            for (int i = 0; i < code.Length; ++i)
            {
                ref readonly Instruction instruction = ref code[i];
                switch (instruction.Op)
                {
                    case OpCode.Push:
                        stack.Push(instruction.Immediate);
                        break;

                    case OpCode.Abs:
                    {
                        Operand num = stack.Pop();
                        if (num.Kind == OperandKind.Integer)
                        {
                            stack.Push(Operand.Integer(Math.Abs(num.AsInteger)));
                        }
                        else
                        {
                            stack.Push(Operand.Real(Math.Abs(num.ToReal())));
                        }
                        break;
                    }

                    case OpCode.Add:
                    {
                        Operand num2 = stack.Pop();
                        Operand num1 = stack.Pop();
                        if (num1.Kind == OperandKind.Integer && num2.Kind == OperandKind.Integer)
                        {
                            long sum = (long)num1.AsInteger + num2.AsInteger;
                            if (sum < int.MinValue || sum > int.MaxValue)
                            {
                                stack.Push(Operand.Real(sum));
                            }
                            else
                            {
                                stack.Push(Operand.Integer((int)sum));
                            }
                        }
                        else
                        {
                            stack.Push(Operand.Real(num1.ToReal() + num2.ToReal()));
                        }
                        break;
                    }

                    case OpCode.Atan:
                    {
                        double den = stack.PopReal();
                        double num = stack.PopReal();
                        double atan = ToDegrees(Math.Atan2(num, den)) % 360;
                        if (atan < 0)
                        {
                            atan += 360;
                        }
                        stack.Push(Operand.Real(atan));
                        break;
                    }

                    case OpCode.Ceiling:
                    {
                        Operand num = stack.Pop();
                        stack.Push(num.Kind == OperandKind.Integer ? num : Operand.Real(Math.Ceiling(num.ToReal())));
                        break;
                    }

                    case OpCode.Cos:
                        stack.Push(Operand.Real(Math.Cos(ToRadians(stack.PopReal()))));
                        break;

                    case OpCode.Cvi:
                        stack.Push(Operand.Integer((int)Math.Truncate(stack.PopReal())));
                        break;

                    case OpCode.Cvr:
                        stack.Push(Operand.Real(stack.PopReal()));
                        break;

                    case OpCode.Div:
                    {
                        double num2 = stack.PopReal();
                        double num1 = stack.PopReal();
                        stack.Push(Operand.Real(num1 / num2));
                        break;
                    }

                    case OpCode.Exp:
                    {
                        double exponent = stack.PopReal();
                        double @base = stack.PopReal();
                        stack.Push(Operand.Real(Math.Pow(@base, exponent)));
                        break;
                    }

                    case OpCode.Floor:
                    {
                        Operand num = stack.Pop();
                        stack.Push(num.Kind == OperandKind.Integer ? num : Operand.Real(Math.Floor(num.ToReal())));
                        break;
                    }

                    case OpCode.IDiv:
                    {
                        int num2 = stack.PopInt();
                        int num1 = stack.PopInt();
                        stack.Push(Operand.Integer(num1 / num2));
                        break;
                    }

                    case OpCode.Ln:
                        stack.Push(Operand.Real(Math.Log(stack.PopReal())));
                        break;

                    case OpCode.Log:
                        stack.Push(Operand.Real(Math.Log10(stack.PopReal())));
                        break;

                    case OpCode.Mod:
                    {
                        int int2 = stack.PopInt();
                        int int1 = stack.PopInt();
                        stack.Push(Operand.Integer(int1 % int2));
                        break;
                    }

                    case OpCode.Mul:
                    {
                        Operand num2 = stack.Pop();
                        Operand num1 = stack.Pop();
                        if (num1.Kind == OperandKind.Integer && num2.Kind == OperandKind.Integer)
                        {
                            long result = (long)num1.AsInteger * num2.AsInteger;
                            if (result >= int.MinValue && result <= int.MaxValue)
                            {
                                stack.Push(Operand.Integer((int)result));
                            }
                            else
                            {
                                stack.Push(Operand.Real(result));
                            }
                        }
                        else
                        {
                            stack.Push(Operand.Real(num1.ToReal() * num2.ToReal()));
                        }
                        break;
                    }

                    case OpCode.Neg:
                    {
                        Operand num = stack.Pop();
                        if (num.Kind == OperandKind.Integer)
                        {
                            int v = num.AsInteger;
                            if (v == int.MinValue)
                            {
                                stack.Push(Operand.Real(-(double)v));
                            }
                            else
                            {
                                stack.Push(Operand.Integer(-v));
                            }
                        }
                        else
                        {
                            stack.Push(Operand.Real(-num.ToReal()));
                        }
                        break;
                    }

                    case OpCode.Round:
                    {
                        Operand num = stack.Pop();
                        if (num.Kind == OperandKind.Integer)
                        {
                            stack.Push(num);
                        }
                        else
                        {
                            double value = num.ToReal();
                            // The way java works...
                            stack.Push(Operand.Real(value < 0 ? Math.Round(value) : Math.Round(value, MidpointRounding.AwayFromZero)));
                        }
                        break;
                    }

                    case OpCode.Sin:
                        stack.Push(Operand.Real(Math.Sin(ToRadians(stack.PopReal()))));
                        break;

                    case OpCode.Sqrt:
                    {
                        double num = stack.PopReal();
                        if (num < 0)
                        {
                            throw new ArgumentException("argument must be nonnegative");
                        }
                        stack.Push(Operand.Real(Math.Sqrt(num)));
                        break;
                    }

                    case OpCode.Sub:
                    {
                        Operand num2 = stack.Pop();
                        Operand num1 = stack.Pop();
                        if (num1.Kind == OperandKind.Integer && num2.Kind == OperandKind.Integer)
                        {
                            long result = (long)num1.AsInteger - num2.AsInteger;
                            if (result >= int.MinValue && result <= int.MaxValue)
                            {
                                stack.Push(Operand.Integer((int)result));
                            }
                            else
                            {
                                stack.Push(Operand.Real(result));
                            }
                        }
                        else
                        {
                            stack.Push(Operand.Real(num1.ToReal() - num2.ToReal()));
                        }
                        break;
                    }

                    case OpCode.Truncate:
                    {
                        Operand num = stack.Pop();
                        stack.Push(num.Kind == OperandKind.Integer ? num : Operand.Real(Math.Truncate(num.ToReal())));
                        break;
                    }

                    case OpCode.And:
                    case OpCode.Or:
                    case OpCode.Xor:
                    {
                        Operand op2 = stack.Pop();
                        Operand op1 = stack.Pop();
                        if (op1.Kind == OperandKind.Boolean && op2.Kind == OperandKind.Boolean)
                        {
                            bool b1 = op1.AsBoolean;
                            bool b2 = op2.AsBoolean;
                            bool result = instruction.Op switch
                            {
                                OpCode.And => b1 && b2,
                                OpCode.Or => b1 || b2,
                                _ => b1 ^ b2
                            };
                            stack.Push(Operand.Boolean(result));
                        }
                        else if (op1.Kind == OperandKind.Integer && op2.Kind == OperandKind.Integer)
                        {
                            int i1 = op1.AsInteger;
                            int i2 = op2.AsInteger;
                            int result = instruction.Op switch
                            {
                                OpCode.And => i1 & i2,
                                OpCode.Or => i1 | i2,
                                _ => i1 ^ i2
                            };
                            stack.Push(Operand.Integer(result));
                        }
                        else
                        {
                            throw new InvalidCastException("Operands must be bool/bool or int/int");
                        }
                        break;
                    }

                    case OpCode.Bitshift:
                    {
                        int shift = stack.PopConvertedInt();
                        int int1 = stack.PopConvertedInt();
                        stack.Push(Operand.Integer(shift < 0 ? int1 >> Math.Abs(shift) : int1 << shift));
                        break;
                    }

                    case OpCode.Eq:
                    {
                        Operand op2 = stack.Pop();
                        Operand op1 = stack.Pop();
                        stack.Push(Operand.Boolean(StrictEquals(op1, op2)));
                        break;
                    }

                    case OpCode.Ne:
                    {
                        Operand op2 = stack.Pop();
                        Operand op1 = stack.Pop();
                        stack.Push(Operand.Boolean(!StrictEquals(op1, op2)));
                        break;
                    }

                    case OpCode.Ge:
                    case OpCode.Gt:
                    case OpCode.Le:
                    case OpCode.Lt:
                    {
                        double num2 = stack.PopReal();
                        double num1 = stack.PopReal();
                        bool result = instruction.Op switch
                        {
                            OpCode.Ge => num1 >= num2,
                            OpCode.Gt => num1 > num2,
                            OpCode.Le => num1 <= num2,
                            _ => num1 < num2
                        };
                        stack.Push(Operand.Boolean(result));
                        break;
                    }

                    case OpCode.Not:
                    {
                        Operand op1 = stack.Pop();
                        if (op1.Kind == OperandKind.Boolean)
                        {
                            stack.Push(Operand.Boolean(!op1.AsBoolean));
                        }
                        else if (op1.Kind == OperandKind.Integer)
                        {
                            // Preserves the existing (PdfBox-derived) arithmetic negation behaviour.
                            stack.Push(Operand.Integer(-op1.AsInteger));
                        }
                        else
                        {
                            throw new InvalidCastException("Operand must be bool or int");
                        }
                        break;
                    }

                    case OpCode.True:
                        stack.Push(Operand.Boolean(true));
                        break;

                    case OpCode.False:
                        stack.Push(Operand.Boolean(false));
                        break;

                    case OpCode.If:
                    {
                        int procedure = stack.PopProcedure();
                        bool condition = stack.PopStrictBoolean();
                        if (condition)
                        {
                            ExecuteProcedure(procedure, ref stack);
                        }
                        break;
                    }

                    case OpCode.IfElse:
                    {
                        int procedure2 = stack.PopProcedure();
                        int procedure1 = stack.PopProcedure();
                        bool condition = stack.PopConvertedBoolean();
                        ExecuteProcedure(condition ? procedure1 : procedure2, ref stack);
                        break;
                    }

                    case OpCode.Copy:
                        stack.CopyTop(stack.PopInt());
                        break;

                    case OpCode.Dup:
                        stack.Push(stack.Peek());
                        break;

                    case OpCode.Exch:
                        stack.Exchange();
                        break;

                    case OpCode.Index:
                    {
                        int n = stack.PopConvertedInt();
                        if (n < 0)
                        {
                            throw new ArgumentException("rangecheck: " + n);
                        }
                        stack.Push(stack.FromTop(n));
                        break;
                    }

                    case OpCode.Pop:
                        stack.Pop();
                        break;

                    case OpCode.Roll:
                    {
                        int j = stack.PopInt();
                        int n = stack.PopInt();
                        stack.Roll(n, j);
                        break;
                    }

                    case OpCode.Unknown:
                        throw new InvalidOperationException("Unknown operator or name: " + instruction.Name);

                    default:
                        throw new InvalidOperationException("Unhandled opcode: " + instruction.Op);
                }
            }

            // Handles procs left on the stack that simply need to be executed
            // (mirrors the tail of the previous InstructionSequence.Execute).
            while (stack.Count > 0 && stack.Peek().Kind == OperandKind.Procedure)
            {
                ExecuteProcedure(stack.Pop().AsProcedureIndex, ref stack);
            }
        }

        private static double ToRadians(double val) => (Math.PI / 180.0) * val;

        private static double ToDegrees(double val) => (180.0 / Math.PI) * val;

        private static bool StrictEquals(in Operand op1, in Operand op2)
        {
            // Mirrors the previous boxed Equals semantics: operands of different kinds are never
            // equal (int 1 is not equal to real 1.0); reals use double.Equals (NaN equals NaN);
            // procedure indices behave like the previous reference equality.
            return op1.Kind == op2.Kind && op1.Value.Equals(op2.Value);
        }
    }
}
