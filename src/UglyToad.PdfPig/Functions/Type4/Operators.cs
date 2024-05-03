namespace UglyToad.PdfPig.Functions.Type4
{
    /// <summary>
    /// This class provides all the supported operators.
    /// </summary>
    internal sealed class Operators
    {
        //Arithmetic operators
        private static readonly Operator ABS = new ArithmeticOperators.Abs();
        private static readonly Operator ADD = new ArithmeticOperators.Add();
        private static readonly Operator ATAN = new ArithmeticOperators.Atan();
        private static readonly Operator CEILING = new ArithmeticOperators.Ceiling();
        private static readonly Operator COS = new ArithmeticOperators.Cos();
        private static readonly Operator CVI = new ArithmeticOperators.Cvi();
        private static readonly Operator CVR = new ArithmeticOperators.Cvr();
        private static readonly Operator DIV = new ArithmeticOperators.Div();
        private static readonly Operator EXP = new ArithmeticOperators.Exp();
        private static readonly Operator FLOOR = new ArithmeticOperators.Floor();
        private static readonly Operator IDIV = new ArithmeticOperators.IDiv();
        private static readonly Operator LN = new ArithmeticOperators.Ln();
        private static readonly Operator LOG = new ArithmeticOperators.Log();
        private static readonly Operator MOD = new ArithmeticOperators.Mod();
        private static readonly Operator MUL = new ArithmeticOperators.Mul();
        private static readonly Operator NEG = new ArithmeticOperators.Neg();
        private static readonly Operator ROUND = new ArithmeticOperators.Round();
        private static readonly Operator SIN = new ArithmeticOperators.Sin();
        private static readonly Operator SQRT = new ArithmeticOperators.Sqrt();
        private static readonly Operator SUB = new ArithmeticOperators.Sub();
        private static readonly Operator TRUNCATE = new ArithmeticOperators.Truncate();

        //Relational, boolean and bitwise operators
        private static readonly Operator AND = new BitwiseOperators.And();
        private static readonly Operator BITSHIFT = new BitwiseOperators.Bitshift();
        private static readonly Operator EQ = new RelationalOperators.Eq();
        private static readonly Operator FALSE = new BitwiseOperators.False();
        private static readonly Operator GE = new RelationalOperators.Ge();
        private static readonly Operator GT = new RelationalOperators.Gt();
        private static readonly Operator LE = new RelationalOperators.Le();
        private static readonly Operator LT = new RelationalOperators.Lt();
        private static readonly Operator NE = new RelationalOperators.Ne();
        private static readonly Operator NOT = new BitwiseOperators.Not();
        private static readonly Operator OR = new BitwiseOperators.Or();
        private static readonly Operator TRUE = new BitwiseOperators.True();
        private static readonly Operator XOR = new BitwiseOperators.Xor();

        //Conditional operators
        private static readonly Operator IF = new ConditionalOperators.If();
        private static readonly Operator IFELSE = new ConditionalOperators.IfElse();

        //Stack operators
        private static readonly Operator COPY = new StackOperators.Copy();
        private static readonly Operator DUP = new StackOperators.Dup();
        private static readonly Operator EXCH = new StackOperators.Exch();
        private static readonly Operator INDEX = new StackOperators.Index();
        private static readonly Operator POP = new StackOperators.Pop();
        private static readonly Operator ROLL = new StackOperators.Roll();

        private readonly Dictionary<string, Operator> operators = new Dictionary<string, Operator>();

        /// <summary>
        /// Creates a new Operators object with the default set of operators.
        /// </summary>
        public Operators()
        {
            operators.Add("add", ADD);
            operators.Add("abs", ABS);
            operators.Add("atan", ATAN);
            operators.Add("ceiling", CEILING);
            operators.Add("cos", COS);
            operators.Add("cvi", CVI);
            operators.Add("cvr", CVR);
            operators.Add("div", DIV);
            operators.Add("exp", EXP);
            operators.Add("floor", FLOOR);
            operators.Add("idiv", IDIV);
            operators.Add("ln", LN);
            operators.Add("log", LOG);
            operators.Add("mod", MOD);
            operators.Add("mul", MUL);
            operators.Add("neg", NEG);
            operators.Add("round", ROUND);
            operators.Add("sin", SIN);
            operators.Add("sqrt", SQRT);
            operators.Add("sub", SUB);
            operators.Add("truncate", TRUNCATE);

            operators.Add("and", AND);
            operators.Add("bitshift", BITSHIFT);
            operators.Add("eq", EQ);
            operators.Add("false", FALSE);
            operators.Add("ge", GE);
            operators.Add("gt", GT);
            operators.Add("le", LE);
            operators.Add("lt", LT);
            operators.Add("ne", NE);
            operators.Add("not", NOT);
            operators.Add("or", OR);
            operators.Add("true", TRUE);
            operators.Add("xor", XOR);

            operators.Add("if", IF);
            operators.Add("ifelse", IFELSE);

            operators.Add("copy", COPY);
            operators.Add("dup", DUP);
            operators.Add("exch", EXCH);
            operators.Add("index", INDEX);
            operators.Add("pop", POP);
            operators.Add("roll", ROLL);
        }

        /// <summary>
        /// Returns the operator for the given operator name.
        /// </summary>
        /// <param name="operatorName">the operator name</param>
        /// <returns>the operator (or null if there's no such operator</returns>
        public Operator GetOperator(string operatorName)
        {
            return this.operators[operatorName];
        }
    }
}
