namespace UglyToad.PdfPig.Fonts.Type1.Parser
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using Core;

    internal class Type1Tokenizer
    {
        private readonly StringBuilder commentBuffer = new StringBuilder();
        private readonly StringBuilder literalBuffer = new StringBuilder();
        private readonly StringBuilder stringBuffer = new StringBuilder();

        private readonly IInputBytes bytes;
        private readonly List<string> comments;

        private int openParens;
        private Type1Token previousToken;

        public Type1Token CurrentToken { get; private set; }
        public IReadOnlyList<string> Comments => comments;

        public Type1Tokenizer(IInputBytes bytes)
        {
            this.bytes = bytes;
            comments = new List<string>();
            CurrentToken = ReadNextToken();
        }

        public Type1Token GetNext()
        {
            CurrentToken = ReadNextToken();
            return CurrentToken;
        }

        private Type1Token ReadNextToken()
        {
            previousToken = CurrentToken;
            bool skip;
            do
            {
                skip = false;
                while (bytes.MoveNext())
                {
                    var b = bytes.CurrentByte;
                    var c = (char)b;

                    switch (c)
                    {
                        case '%':
                            comments.Add(ReadComment());
                            break;
                        case '(':
                            return ReadString();
                        case ')':
                            throw new InvalidOperationException("Encountered an end of string ')' outside of string.");
                        case '[':
                            return new Type1Token(c, Type1Token.TokenType.StartArray);
                        case ']':
                            return new Type1Token(c, Type1Token.TokenType.EndArray);
                        case '{':
                            return new Type1Token(c, Type1Token.TokenType.StartProc);
                        case '}':
                            return new Type1Token(c, Type1Token.TokenType.EndProc);
                        case '/':
                            {
                                var name = ReadLiteral();
                                return new Type1Token(name, Type1Token.TokenType.Literal);
                            }
                        case '<':
                            {
                                var following = bytes.Peek();
                                if (following == '<')
                                {
                                    bytes.MoveNext();
                                    return new Type1Token("<<", Type1Token.TokenType.StartDict);
                                }

                                return new Type1Token(c, Type1Token.TokenType.Name);
                            }
                        case '>':
                            {
                                var following = bytes.Peek();
                                if (following == '>')
                                {
                                    bytes.MoveNext();
                                    return new Type1Token(">>", Type1Token.TokenType.EndDict);
                                }

                                return new Type1Token(c, Type1Token.TokenType.Name);
                            }
                        default:
                            {
                                if (ReadHelper.IsWhitespace(b))
                                {
                                    skip = true;
                                    break;
                                }

                                if (b == 0)
                                {
                                    skip = true;
                                    break;
                                }

                                if (TryReadNumber(c, out var number))
                                {
                                    return number;
                                }

                                var name = ReadLiteral(c);
                                if (name == null)
                                {
                                    throw new InvalidOperationException($"The binary portion of the type 1 font was invalid at position {bytes.CurrentOffset}.");
                                }

                                if (name.Equals(Type1Symbols.RdProcedure, StringComparison.OrdinalIgnoreCase) || name.Equals(Type1Symbols.RdProcedureAlt))
                                {
                                    if (previousToken.Type == Type1Token.TokenType.Integer)
                                    {
                                        return ReadCharString(previousToken.AsInt());
                                    }

                                    throw new InvalidOperationException($"Expected integer token before {name} at offset {bytes.CurrentOffset}.");
                                }

                                return new Type1Token(name, Type1Token.TokenType.Name);
                            }
                    }
                }
            } while (skip);

            return null;
        }

        private Type1Token ReadString()
        {
            char GetNext()
            {
                bytes.MoveNext();
                return (char)bytes.CurrentByte;
            }
            stringBuffer.Clear();

            while (bytes.MoveNext())
            {
                var c = (char)bytes.CurrentByte;

                // string context
                switch (c)
                {
                    case '(':
                        openParens++;
                        stringBuffer.Append('(');
                        break;
                    case ')':
                        if (openParens == 0)
                        {
                            // end of string
                            return new Type1Token(stringBuffer.ToString(), Type1Token.TokenType.String);
                        }
                        stringBuffer.Append(')');
                        openParens--;
                        break;
                    case '\\':
                        // escapes: \n \r \t \b \f \\ \( \)
                        char c1 = GetNext();
                        switch (c1)
                        {
                            case 'n':
                            case 'r': stringBuffer.Append('\n'); break;
                            case 't': stringBuffer.Append('\t'); break;
                            case 'b': stringBuffer.Append('\b'); break;
                            case 'f': stringBuffer.Append('\f'); break;
                            case '\\': stringBuffer.Append('\\'); break;
                            case '(': stringBuffer.Append('('); break;
                            case ')': stringBuffer.Append(')'); break;
                        }
                        // octal \ddd
                        if (char.IsDigit(c1))
                        {
                            var rawOctal = new string([c1, GetNext(), GetNext()]);
                            var code = Convert.ToInt32(rawOctal, 8);
                            stringBuffer.Append((char)code);
                        }
                        break;
                    case '\r':
                    case '\n':
                        stringBuffer.Append('\n');
                        break;
                    default:
                        stringBuffer.Append(c);
                        break;
                }
            }
            return null;
        }

        private bool TryReadNumber(char c, out Type1Token numberToken)
        {
            char GetNext()
            {
                bytes.MoveNext();
                return (char)bytes.CurrentByte;
            }

            numberToken = null;

            var currentPosition = bytes.CurrentOffset;

            var sb = new StringBuilder();
            StringBuilder radix = null;

            var hasDigit = false;

            // optional + or -
            if (c == '+' || c == '-')
            {
                sb.Append(c);
                c = GetNext();
            }

            // optional digits
            while (char.IsDigit(c))
            {
                sb.Append(c);
                c = GetNext();
                hasDigit = true;
            }

            // optional .
            if (c == '.')
            {
                sb.Append(c);
                c = GetNext();
            }
            else if (c == '#')
            {
                // PostScript radix number takes the form base#number
                radix = sb;
                sb = new StringBuilder();
                c = GetNext();
            }
            else if (sb.Length == 0 || !hasDigit)
            {
                // failure
                bytes.Seek(currentPosition);
                return false;
            }
            else
            {
                // integer
                bytes.Seek(bytes.CurrentOffset - 1);

                numberToken = new Type1Token(sb.ToString(), Type1Token.TokenType.Integer);
                return true;
            }

            // required digit
            if (char.IsDigit(c))
            {
                sb.Append(c);
                c = GetNext();
            }
            else
            {
                bytes.Seek(currentPosition);
                return false;
            }

            // optional digits
            while (char.IsDigit(c))
            {
                sb.Append(c);
                c = GetNext();
            }

            // optional E
            if (c == 'E')
            {
                sb.Append(c);
                c = GetNext();

                // optional minus
                if (c == '-')
                {
                    sb.Append(c);
                    c = GetNext();
                }

                // required digit
                if (char.IsDigit(c))
                {
                    sb.Append(c);
                    c = GetNext();
                }
                else
                {
                    bytes.Seek(currentPosition);
                    return false;
                }

                // optional digits
                while (char.IsDigit(c))
                {
                    sb.Append(c);
                    c = GetNext();
                }
            }

            bytes.Seek(bytes.CurrentOffset - 1);
            if (radix != null)
            {
                var number = Convert.ToInt32(sb.ToString(), int.Parse(radix.ToString(), CultureInfo.InvariantCulture));
                numberToken = new Type1Token(number.ToString(), Type1Token.TokenType.Integer);
            }
            else
            {
                numberToken = new Type1Token(sb.ToString(), Type1Token.TokenType.Real);
            }

            return true;
        }

        private string ReadLiteral(char? previousCharacter = null)
        {
            literalBuffer.Clear();
            if (previousCharacter.HasValue)
            {
                literalBuffer.Append(previousCharacter);
            }

            do
            {
                var b = bytes.Peek();
                if (!b.HasValue)
                {
                    break;
                }

                var c = (char)b;

                if (char.IsWhiteSpace(c) || c == '(' || c == ')' || c == '<' || c == '>' ||
                    c == '[' || c == ']' || c == '{' || c == '}' || c == '/' || c == '%')
                {
                    break;
                }

                literalBuffer.Append(c);
            } while (bytes.MoveNext());

            var literal = literalBuffer.ToString();
            return literal.Length == 0 ? null : literal;
        }

        private string ReadComment()
        {
            commentBuffer.Clear();

            while (bytes.MoveNext())
            {
                var c = (char)bytes.CurrentByte;
                if (ReadHelper.IsEndOfLine(c))
                {
                    break;
                }

                commentBuffer.Append(c);
            }

            return commentBuffer.ToString();
        }

        private Type1DataToken ReadCharString(int length)
        {
            // Skip preceding space.
            bytes.MoveNext();
            // TODO: may be wrong
           // bytes.MoveNext();

            byte[] data = new byte[length];
            for (int i = 0; i < length; i++)
            {
                bytes.MoveNext();
                data[i] = bytes.CurrentByte;
            }

            return new Type1DataToken(Type1Token.TokenType.Charstring, data);
        }
    }
}
