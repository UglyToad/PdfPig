//namespace UglyToad.PdfPig.Fonts.CharStrings
//{
//    using System;
//    using System.Collections.Generic;

//    internal class Type2CharStringParser
//    {
//        private int hstemCount = 0;
//        private int vstemCount = 0;
//        private List<Object> sequence = null;


//        private List<Object> Parse(byte[] bytes, byte[][] globalSubrIndex, byte[][] localSubrIndex)
//        {
//            DataInput input = new DataInput(bytes);
//            boolean localSubroutineIndexProvided = localSubrIndex != null && localSubrIndex.length > 0;
//            boolean globalSubroutineIndexProvided = globalSubrIndex != null && globalSubrIndex.length > 0;

//            while (input.hasRemaining())
//            {
//                int b0 = input.readUnsignedByte();
//                if (b0 == 10 && localSubroutineIndexProvided)
//                {
//                    // process subr command
//                    Integer operand = (Integer) sequence.remove(sequence.size() - 1);
//                    //get subrbias
//                    int bias = 0;
//                    int nSubrs = localSubrIndex.length;

//                    if (nSubrs < 1240)
//                    {
//                        bias = 107;
//                    }
//                    else if (nSubrs < 33900)
//                    {
//                        bias = 1131;
//                    }
//                    else
//                    {
//                        bias = 32768;
//                    }

//                    int subrNumber = bias + operand;
//                    if (subrNumber < localSubrIndex.length)
//                    {
//                        byte[] subrBytes = localSubrIndex[subrNumber];
//                        parse(subrBytes, globalSubrIndex, localSubrIndex, false);
//                        Object lastItem = sequence.get(sequence.size() - 1);
//                        if (lastItem is CharStringCommand && ((CharStringCommand) lastItem).getKey().getValue()[0] == 11)
//                        {
//                            sequence.remove(sequence.size() - 1); // remove "return" command
//                        }
//                    }

//                }
//                else if (b0 == 29 && globalSubroutineIndexProvided)
//                {
//                    // process globalsubr command
//                    Integer operand = (Integer) sequence.remove(sequence.size() - 1);
////get subrbias
//                    int bias;
//                    int nSubrs = globalSubrIndex.length;

//                    if (nSubrs < 1240)
//                    {
//                        bias = 107;
//                    }
//                    else if (nSubrs < 33900)
//                    {
//                        bias = 1131;
//                    }
//                    else
//                    {
//                        bias = 32768;
//                    }

//                    int subrNumber = bias + operand;
//                    if (subrNumber < globalSubrIndex.length)
//                    {
//                        byte[] subrBytes = globalSubrIndex[subrNumber];
//                        parse(subrBytes, globalSubrIndex, localSubrIndex, false);
//                        Object lastItem = sequence.get(sequence.size() - 1);
//                        if (lastItem is CharStringCommand && ((CharStringCommand) lastItem).getKey().getValue()[0] == 11)
//                        {
//                            sequence.remove(sequence.size() - 1); // remove "return" command
//                        }
//                    }

//                }
//                else if (b0 >= 0 && b0 <= 27)
//                {
//                    sequence.add(readCommand(b0, input));
//                }
//                else if (b0 == 28)
//                {
//                    sequence.add(readNumber(b0, input));
//                }
//                else if (b0 >= 29 && b0 <= 31)
//                {
//                    sequence.add(readCommand(b0, input));
//                }
//                else if (b0 >= 32 && b0 <= 255)
//                {
//                    sequence.add(readNumber(b0, input));
//                }
//                else
//                {
//                    throw new IllegalArgumentException();
//                }
//            }

//            return sequence;
//        }

//        private CharStringCommand readCommand(int b0, DataInput input)
//        {

//            if (b0 == 1 || b0 == 18)
//            {
//                hstemCount += peekNumbers().size() / 2;
//            }
//            else if (b0 == 3 || b0 == 19 || b0 == 20 || b0 == 23)
//            {
//                vstemCount += peekNumbers().size() / 2;
//            } // End if

//            if (b0 == 12)
//            {
//                int b1 = input.readUnsignedByte();

//                return new CharStringCommand(b0, b1);
//            }
//            else if (b0 == 19 || b0 == 20)
//            {
//                int[] value = new int[1 + getMaskLength()];
//                value[0] = b0;

//                for (int i = 1; i < value.length; i++)
//                {
//                    value[i] = input.readUnsignedByte();
//                }

//                return new CharStringCommand(value);
//            }

//            return new CharStringCommand(b0);
//        }

//        private Number readNumber(int b0, DataInput input)
//        {

//            if (b0 == 28)
//            {
//                return (int) input.readShort();
//            }
//            else if (b0 >= 32 && b0 <= 246)
//            {
//                return b0 - 139;
//            }
//            else if (b0 >= 247 && b0 <= 250)
//            {
//                int b1 = input.readUnsignedByte();

//                return (b0 - 247) * 256 + b1 + 108;
//            }
//            else if (b0 >= 251 && b0 <= 254)
//            {
//                int b1 = input.readUnsignedByte();

//                return -(b0 - 251) * 256 - b1 - 108;
//            }
//            else if (b0 == 255)
//            {
//                short value = input.readShort();
//                // The lower bytes are representing the digits after the decimal point
//                double fraction = input.readUnsignedShort() / 65535d;
//                return value + fraction;
//            }
//            else
//            {
//                throw new IllegalArgumentException();
//            }
//        }

//        private int getMaskLength()
//        {
//            int hintCount = hstemCount + vstemCount;
//            int length = hintCount / 8;
//            if (hintCount % 8 > 0)
//            {
//                length++;
//            }

//            return length;
//        }

//        private List<Number> peekNumbers()
//        {
//            List<Number> numbers = new ArrayList<>();
//            for (int i = sequence.size() - 1; i > -1; i--)
//            {
//                Object object = sequence.get(i);

//                if (!(object instanceof Number))
//                {
//                    return numbers;
//                }
//                numbers.add(0, (Number) object);
//            }

//            return numbers;
//        }
//    }
//}
