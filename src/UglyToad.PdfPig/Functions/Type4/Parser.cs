namespace UglyToad.PdfPig.Functions.Type4
{
    using System.Text;

    /// <summary>
    /// Parser for PDF Type 4 functions. This implements a small subset of the PostScript
    /// language but is no full PostScript interpreter.
    /// </summary>
    internal sealed class Parser
    {
        /// <summary>
        /// Used to indicate the parsers current state.
        /// </summary>
        internal enum State
        {
            NEWLINE, WHITESPACE, COMMENT, TOKEN
        }

        private Parser()
        {
            //nop
        }

        /// <summary>
        /// Parses a Type 4 function and sends the syntactic elements to the given syntax handler.
        /// </summary>
        /// <param name="input">the text source</param>
        /// <param name="handler">the syntax handler</param>
        public static void Parse(string input, SyntaxHandler handler)
        {
            Tokenizer tokenizer = new Tokenizer(input, handler);
            tokenizer.Tokenize();
        }

        /// <summary>
        /// This interface defines all possible syntactic elements of a Type 4 function.
        /// It is called by the parser as the function is interpreted.
        /// </summary>
        public interface SyntaxHandler
        {
            /// <summary>
            /// Indicates that a new line starts.
            /// </summary>
            /// <param name="text">the new line character (CR, LF, CR/LF or FF)</param>
            void NewLine(string text);

            /// <summary>
            /// Called when whitespace characters are encountered.
            /// </summary>
            /// <param name="text">the whitespace text</param>
            void Whitespace(string text);

            /// <summary>
            /// Called when a token is encountered. No distinction between operators and values is done here.
            /// </summary>
            /// <param name="text">the token text</param>
            void Token(string text);

            /// <summary>
            /// Called for a comment.
            /// </summary>
            /// <param name="text">the comment</param>
            void Comment(string text);
        }

        /// <summary>
        /// Abstract base class for a <see cref="SyntaxHandler"/>.
        /// </summary>
        public abstract class AbstractSyntaxHandler : SyntaxHandler
        {
            /// <inheritdoc/>
            public void Comment(string text)
            {
                //nop
            }

            /// <inheritdoc/>
            public void NewLine(string text)
            {
                //nop
            }

            /// <inheritdoc/>
            public void Whitespace(string text)
            {
                //nop
            }

            /// <inheritdoc/>
            public abstract void Token(string text);
        }

        /// <summary>
        /// Tokenizer for Type 4 functions.
        /// </summary>
        internal class Tokenizer
        {
            private const char NUL = '\u0000'; //NUL
            private const char EOT = '\u0004'; //END OF TRANSMISSION
            private const char TAB = '\u0009'; //TAB CHARACTER
            private const char FF = '\u000C'; //FORM FEED
            private const char CR = '\r'; //CARRIAGE RETURN
            private const char LF = '\n'; //LINE FEED
            private const char SPACE = '\u0020'; //SPACE

            private readonly string input;
            private int index;
            private readonly SyntaxHandler handler;
            private State state = State.WHITESPACE;
            private readonly StringBuilder buffer = new StringBuilder();

            internal Tokenizer(string text, SyntaxHandler syntaxHandler)
            {
                this.input = text;
                this.handler = syntaxHandler;
            }

            private bool HasMore()
            {
                return index < input.Length;
            }

            private char CurrentChar()
            {
                return input[index];
            }

            private char NextChar()
            {
                index++;
                if (!HasMore())
                {
                    return EOT;
                }
                else
                {
                    return CurrentChar();
                }
            }

            private char Peek()
            {
                if (index < input.Length - 1)
                {
                    return input[index + 1];
                }
                else
                {
                    return EOT;
                }
            }

            private State NextState()
            {
                char ch = CurrentChar();
                switch (ch)
                {
                    case CR:
                    case LF:
                    case FF: //FF
                        state = State.NEWLINE;
                        break;
                    case NUL:
                    case TAB:
                    case SPACE:
                        state = State.WHITESPACE;
                        break;
                    case '%':
                        state = State.COMMENT;
                        break;
                    default:
                        state = State.TOKEN;
                        break;
                }
                return state;
            }

            internal void Tokenize()
            {
                while (HasMore())
                {
                    buffer.Length = 0;
                    NextState();
                    switch (state)
                    {
                        case State.NEWLINE:
                            ScanNewLine();
                            break;
                        case State.WHITESPACE:
                            ScanWhitespace();
                            break;
                        case State.COMMENT:
                            ScanComment();
                            break;
                        default:
                            ScanToken();
                            break;
                    }
                }
            }

            private void ScanNewLine()
            {
                char ch = CurrentChar();
                buffer.Append(ch);
                if (ch == CR && Peek() == LF)
                {
                    //CRLF is treated as one newline
                    buffer.Append(NextChar());
                }
                handler.NewLine(buffer.ToString());
                NextChar();
            }

            private void ScanWhitespace()
            {
                buffer.Append(CurrentChar());

                bool loop = true;
                while (HasMore() && loop)
                {
                    char ch = NextChar();
                    switch (ch)
                    {
                        case NUL:
                        case TAB:
                        case SPACE:
                            buffer.Append(ch);
                            break;
                        default:
                            loop = false;
                            break;
                    }
                }
                handler.Whitespace(buffer.ToString());
            }

            private void ScanComment()
            {
                buffer.Append(CurrentChar());

                bool loop = true;
                while (HasMore() && loop)
                {
                    char ch = NextChar();
                    switch (ch)
                    {
                        case CR:
                        case LF:
                        case FF:
                            loop = false;
                            break;
                        default:
                            buffer.Append(ch);
                            break;
                    }
                }
                //EOF reached
                handler.Comment(buffer.ToString());
            }

            private void ScanToken()
            {
                char ch = CurrentChar();
                buffer.Append(ch);
                switch (ch)
                {
                    case '{':
                    case '}':
                        handler.Token(buffer.ToString());
                        NextChar();
                        return;
                    default:
                        //continue
                        break;
                }

                bool loop = true;
                while (HasMore() && loop)
                {
                    ch = NextChar();
                    switch (ch)
                    {
                        case NUL:
                        case TAB:
                        case SPACE:
                        case CR:
                        case LF:
                        case FF:
                        case EOT:
                        case '{':
                        case '}':
                            loop = false;
                            break;

                        default:
                            buffer.Append(ch);
                            break;
                    }
                }
                //EOF reached
                handler.Token(buffer.ToString());
            }
        }
    }
}
