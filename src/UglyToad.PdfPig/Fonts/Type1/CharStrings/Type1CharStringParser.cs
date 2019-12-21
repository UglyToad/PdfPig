namespace UglyToad.PdfPig.Fonts.Type1.CharStrings
{
    using System;
    using System.Collections.Generic;
    using Commands;
    using Commands.Arithmetic;
    using Commands.Hint;
    using Commands.PathConstruction;
    using Commands.StartFinishOutline;
    using Parser;
    using Util;

    /// <summary>
    /// Decodes a set of CharStrings to their corresponding Type 1 BuildChar operations.
    /// </summary>
    /// <remarks>
    /// A charstring is an encrypted sequence of unsigned 8-bit bytes that encode integers and commands. 
    /// Type 1 BuildChar, when interpreting a charstring, will first decrypt it and then will decode
    /// its bytes one at a time in sequence. 
    /// 
    /// The value in a byte indicates a command, a number, or subsequent bytes that are to be interpreted
    /// in a special way.
    /// 
    /// Once the bytes are decoded into numbers and commands, the execution of these numbers and commands proceeds in a
    /// manner similar to the operation of the PostScript language. Type 1 BuildChar uses its own operand stack, 
    /// called the Type 1 BuildChar operand stack, that is distinct from the PostScript interpreter operand stack.
    ///  
    /// This stack holds up to 24 numeric entries. A number, decoded from a charstring, is pushed onto the Type 1
    /// BuildChar operand stack. A command expects its arguments in order on this operand stack with all arguments generally taken
    /// from the bottom of the stack (first argument bottom-most); 
    /// however, some commands, particularly the subroutine commands, normally work from the top of the stack. If a command returns
    /// results, they are pushed onto the Type 1 BuildChar operand stack (last result topmost).
    /// </remarks>
    internal static class Type1CharStringParser
    {
        public static Type1CharStrings Parse(IReadOnlyList<Type1CharstringDecryptedBytes> charStrings, IReadOnlyList<Type1CharstringDecryptedBytes> subroutines)
        {
            if (charStrings == null)
            {
                throw new ArgumentNullException(nameof(charStrings));
            }

            if (subroutines == null)
            {
                throw new ArgumentNullException(nameof(subroutines));
            }

            var charStringResults = new Dictionary<string, Type1CharStrings.CommandSequence>(charStrings.Count);
            var charStringIndexToName = new Dictionary<int, string>();

            for (var i = 0; i < charStrings.Count; i++)
            {
                var charString = charStrings[i];
                var commandSequence = ParseSingle(charString.Bytes);

                charStringResults[charString.Name] = new Type1CharStrings.CommandSequence(commandSequence);
                charStringIndexToName[i] = charString.Name;
            }

            var subroutineResults = new Dictionary<int, Type1CharStrings.CommandSequence>(subroutines.Count);

            foreach (var subroutine in subroutines)
            {
                var commandSequence = ParseSingle(subroutine.Bytes);

                subroutineResults[subroutine.Index] = new Type1CharStrings.CommandSequence(commandSequence);
            }

            return new Type1CharStrings(charStringResults, charStringIndexToName, subroutineResults);
        }

        private static IReadOnlyList<Union<double, LazyType1Command>> ParseSingle(IReadOnlyList<byte> charStringBytes)
        {
            var interpreted = new List<Union<double, LazyType1Command>>();

            for (var i = 0; i < charStringBytes.Count; i++)
            {
                var b = charStringBytes[i];

                if (b <= 31)
                {
                    var command = GetCommand(b, charStringBytes, ref i);

                    if (command == null)
                    {
                        // Treat as invalid but skip
                        continue;
                    }

                    interpreted.Add(new Union<double, LazyType1Command>.Case2(command));
                }
                else
                {
                    var val = InterpretNumber(b, charStringBytes, ref i);

                    interpreted.Add(new Union<double, LazyType1Command>.Case1(val));
                }
            }

            return interpreted;
        }

        private static int InterpretNumber(byte b, IReadOnlyList<byte> bytes, ref int i)
        {
            if (b >= 32 && b <= 246)
            {
                return b - 139;
            }

            if (b >= 247 && b <= 250)
            {
                var w = bytes[++i];
                return ((b - 247) * 256) + w + 108;
            }

            if (b >= 251 && b <= 254)
            {
                var w = bytes[++i];
                return -((b - 251) * 256) - w - 108;
            }

            var result = bytes[++i] << 24 | bytes[++i] << 16 | bytes[++i] << 8 | bytes[++i];

            return result;
        }

        public static LazyType1Command GetCommand(byte v, IReadOnlyList<byte> bytes, ref int i)
        {
            switch (v)
            {
                case 1:
                    return HStemCommand.Lazy;
                case 3:
                    return VStemCommand.Lazy;
                case 4:
                    return VMoveToCommand.Lazy;
                case 5:
                    return RLineToCommand.Lazy;
                case 6:
                    return HLineToCommand.Lazy;
                case 7:
                    return VLineToCommand.Lazy;
                case 8:
                    return RelativeRCurveToCommand.Lazy;
                case 9:
                    return ClosePathCommand.Lazy;
                case 10:
                    return CallSubrCommand.Lazy;
                case 11:
                    return ReturnCommand.Lazy;
                case 13:
                    return HsbwCommand.Lazy;
                case 14:
                    return EndCharCommand.Lazy;
                case 21:
                    return RMoveToCommand.Lazy;
                case 22:
                    return HMoveToCommand.Lazy;
                case 30:
                    return VhCurveToCommand.Lazy;
                case 31:
                    return HvCurveToCommand.Lazy;
                case 12:
                    {
                        var next = bytes[++i];

                        switch (next)
                        {
                            case 0:
                                return DotSectionCommand.Lazy;
                            case 1:
                                return VStem3Command.Lazy;
                            case 2:
                                return HStem3Command.Lazy;
                            case 6:
                                return SeacCommand.Lazy;
                            case 7:
                                return SbwCommand.Lazy;
                            case 12:
                                return DivCommand.Lazy;
                            case 16:
                                return CallOtherSubrCommand.Lazy;
                            case 17:
                                return PopCommand.Lazy;
                            case 33:
                                return SetCurrentPointCommand.Lazy;
                        }

                        break;
                    }
            }

            return null;
        }
    }
}
