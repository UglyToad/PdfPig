namespace UglyToad.PdfPig.Functions.Type4
{
    /// <summary>
    /// Provides the arithmetic operators such as "add" and "sub".
    /// </summary>
    internal sealed class ArithmeticOperators
    {
        private ArithmeticOperators()
        {
            // Private constructor.
        }

        private static double ToRadians(double val)
        {
            return (Math.PI / 180.0) * val;
        }

        private static double ToDegrees(double val)
        {
            return (180.0 / Math.PI) * val;
        }

        /// <summary>
        /// the "Abs" operator.
        /// </summary>
        internal sealed class Abs : Operator
        {
            public void Execute(ExecutionContext context)
            {
                var num = context.PopNumber();
                if (num is int numi)
                {
                    context.Stack.Push(Math.Abs(numi));
                }
                else
                {
                    context.Stack.Push(Math.Abs(Convert.ToDouble(num)));
                }
            }
        }

        /// <summary>
        /// the "add" operator.
        /// </summary>
        internal sealed class Add : Operator
        {
            public void Execute(ExecutionContext context)
            {
                var num2 = context.PopNumber();
                var num1 = context.PopNumber();
                if (num1 is int num1i && num2 is int num2i)
                {
                    long sum = (long)num1i + (long)num2i; // Keep both cast here
                    if (sum < int.MinValue || sum > int.MaxValue)
                    {
                        context.Stack.Push((double)sum);
                    }
                    else
                    {
                        context.Stack.Push((int)sum);
                    }
                }
                else
                {
                    double sum = Convert.ToDouble(num1) + Convert.ToDouble(num2);
                    context.Stack.Push(sum);
                }
            }
        }

        /// <summary>
        /// the "atan" operator.
        /// </summary>
        internal sealed class Atan : Operator
        {
            public void Execute(ExecutionContext context)
            {
                double den = context.PopReal();
                double num = context.PopReal();
                double atan = Math.Atan2(num, den);
                atan = ToDegrees(atan) % 360;
                if (atan < 0)
                {
                    atan += 360;
                }
                context.Stack.Push(atan);
            }
        }

        /// <summary>
        /// the "ceiling" operator.
        /// </summary>
        internal sealed class Ceiling : Operator
        {
            public void Execute(ExecutionContext context)
            {
                var num = context.PopNumber();
                if (num is int numi)
                {
                    context.Stack.Push(numi);
                }
                else
                {
                    context.Stack.Push(Math.Ceiling(Convert.ToDouble(num)));
                }
            }
        }

        /// <summary>
        /// the "cos" operator.
        /// </summary>
        internal sealed class Cos : Operator
        {
            public void Execute(ExecutionContext context)
            {
                double angle = context.PopReal();
                double cos = Math.Cos(ToRadians(angle));
                context.Stack.Push(cos);
            }
        }

        /// <summary>
        /// the "cvi" operator.
        /// </summary>
        internal sealed class Cvi : Operator
        {
            public void Execute(ExecutionContext context)
            {
                var num = context.PopNumber();
                context.Stack.Push((int)Math.Truncate(Convert.ToDouble(num)));
            }
        }

        /// <summary>
        /// the "cvr" operator.
        /// </summary>
        internal sealed class Cvr : Operator
        {
            public void Execute(ExecutionContext context)
            {
                var num = context.PopNumber();
                context.Stack.Push(Convert.ToDouble(num));
            }
        }

        /// <summary>
        /// the "div" operator.
        /// </summary>
        internal sealed class Div : Operator
        {
            public void Execute(ExecutionContext context)
            {
                double num2 = Convert.ToDouble(context.PopNumber());
                double num1 = Convert.ToDouble(context.PopNumber());
                context.Stack.Push(num1 / num2);
            }
        }

        /// <summary>
        /// the "exp" operator.
        /// </summary>
        internal sealed class Exp : Operator
        {
            public void Execute(ExecutionContext context)
            {
                double exp = Convert.ToDouble(context.PopNumber());
                double base_ = Convert.ToDouble(context.PopNumber());
                double value = Math.Pow(base_, exp);
                context.Stack.Push(value);
            }
        }

        /// <summary>
        /// the "floor" operator.
        /// </summary>
        internal sealed class Floor : Operator
        {
            public void Execute(ExecutionContext context)
            {
                var num = context.PopNumber();
                if (num is int numi)
                {
                    context.Stack.Push(numi);
                }
                else
                {
                    context.Stack.Push(Math.Floor(Convert.ToDouble(num)));
                }
            }
        }

        /// <summary>
        /// the "idiv" operator.
        /// </summary>
        internal sealed class IDiv : Operator
        {
            public void Execute(ExecutionContext context)
            {
                int num2 = context.PopInt();
                int num1 = context.PopInt();
                context.Stack.Push(num1 / num2);
            }
        }

        /// <summary>
        /// the "ln" operator.
        /// </summary>
        internal sealed class Ln : Operator
        {
            public void Execute(ExecutionContext context)
            {
                var num = context.PopNumber();
                context.Stack.Push(Math.Log(Convert.ToDouble(num)));
            }
        }

        /// <summary>
        /// the "log" operator.
        /// </summary>
        internal sealed class Log : Operator
        {
            public void Execute(ExecutionContext context)
            {
                var num = context.PopNumber();
                context.Stack.Push(Math.Log10(Convert.ToDouble(num)));
            }
        }

        /// <summary>
        /// the "mod" operator.
        /// </summary>
        internal sealed class Mod : Operator
        {
            public void Execute(ExecutionContext context)
            {
                int int2 = context.PopInt();
                int int1 = context.PopInt();
                context.Stack.Push(int1 % int2);
            }
        }

        /// <summary>
        /// the "mul" operator.
        /// </summary>
        internal sealed class Mul : Operator
        {
            public void Execute(ExecutionContext context)
            {
                var num2 = context.PopNumber();
                var num1 = context.PopNumber();
                if (num1 is int num1i && num2 is int num2i)
                {
                    long result = (long)num1i * (long)num2i; // Keep both cast here
                    if (result >= int.MinValue && result <= int.MaxValue)
                    {
                        context.Stack.Push((int)result);
                    }
                    else
                    {
                        context.Stack.Push((double)result);
                    }
                }
                else
                {
                    double result = Convert.ToDouble(num1) * Convert.ToDouble(num2);
                    context.Stack.Push(result);
                }
            }
        }

        /// <summary>
        /// the "neg" operator.
        /// </summary>
        internal sealed class Neg : Operator
        {
            public void Execute(ExecutionContext context)
            {
                var num = context.PopNumber();
                if (num is int v)
                {
                    if (v == int.MinValue)
                    {
                        context.Stack.Push(-Convert.ToDouble(v));
                    }
                    else
                    {
                        context.Stack.Push(-v);
                    }
                }
                else
                {
                    context.Stack.Push(-Convert.ToDouble(num));
                }
            }
        }

        /// <summary>
        /// the "round" operator.
        /// </summary>
        internal sealed class Round : Operator
        {
            public void Execute(ExecutionContext context)
            {
                var num = context.PopNumber();
                if (num is int numi)
                {
                    context.Stack.Push(numi);
                }
                else
                {
                    double value = Convert.ToDouble(num);
                    // The way java works...
                    double roundedValue = value < 0 ? Math.Round(value) : Math.Round(value, MidpointRounding.AwayFromZero);
                    context.Stack.Push(roundedValue);
                }
            }
        }

        /// <summary>
        /// the "sin" operator.
        /// </summary>
        internal sealed class Sin : Operator
        {
            public void Execute(ExecutionContext context)
            {
                double angle = context.PopReal();
                double sin = Math.Sin(ToRadians(angle));
                context.Stack.Push(sin);
            }
        }

        /// <summary>
        /// the "sqrt" operator.
        /// </summary>
        internal sealed class Sqrt : Operator
        {
            public void Execute(ExecutionContext context)
            {
                double num = context.PopReal();
                if (num < 0)
                {
                    throw new ArgumentException("argument must be nonnegative");
                }
                context.Stack.Push(Math.Sqrt(num));
            }
        }

        /// <summary>
        /// the "sub" operator.
        /// </summary>
        internal sealed class Sub : Operator
        {
            public void Execute(ExecutionContext context)
            {
                var num2 = context.PopNumber();
                var num1 = context.PopNumber();
                if (num1 is int num1i && num2 is int num2i)
                {
                    long result = (long)num1i - (long)num2i; // Keep both cast here
                    if (result < int.MinValue || result > int.MaxValue)
                    {
                        context.Stack.Push((double)result);
                    }
                    else
                    {
                        context.Stack.Push((int)result);
                    }
                }
                else
                {
                    double result = Convert.ToDouble(num1) - Convert.ToDouble(num2);
                    context.Stack.Push(result);
                }
            }
        }

        /// <summary>
        /// the "truncate" operator.
        /// </summary>
        internal sealed class Truncate : Operator
        {
            public void Execute(ExecutionContext context)
            {
                var num = context.PopNumber();
                if (num is int numi)
                {
                    context.Stack.Push(numi);
                }
                else
                {
                    context.Stack.Push(Math.Truncate(Convert.ToDouble(num)));
                }
            }
        }
    }
}
