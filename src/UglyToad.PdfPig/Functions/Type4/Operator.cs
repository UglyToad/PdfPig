namespace UglyToad.PdfPig.Functions.Type4
{
    /// <summary>
    /// Interface for PostScript operators.e
    /// </summary>
    internal interface Operator
    {
        /// <summary>
        /// Executes the operator. The method can inspect and manipulate the stack.
        /// </summary>
        /// <param name="context">the execution context</param>
        void Execute(ExecutionContext context);
    }
}
