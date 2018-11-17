namespace UglyToad.PdfPig.Fonts.CompactFontFormat.CharStrings
{
    using System;
    using System.Collections.Generic;
    using Geometry;
    using Util;
    using Util.JetBrains.Annotations;

    /// <summary>
    /// Decodes the commands and numbers making up a Type 2 CharString. A Type 2 CharString extends on the Type 1 CharString format.
    /// Compared to the Type 1 format, the Type 2 encoding offers smaller size and an opportunity for better rendering quality and
    /// performance. The Type 2 charstring operators are (with one exception) a superset of the Type 1 operators.
    /// </summary>
    /// <remarks>
    /// A Type 2 charstring program is a sequence of unsigned 8-bit bytes that encode numbers and operators.
    /// The byte value specifies a operator, a number, or subsequent bytes that are to be interpreted in a specific manner
    /// </remarks>
    internal class Type2CharStringParser
    {
        private static readonly IReadOnlyDictionary<int, LazyType2Command> SingleByteCommandStore = new Dictionary<int, LazyType2Command>
        {
            { 1,  new LazyType2Command("hstem", ctx =>
                {
                    var numberOfEdgeHints = ctx.Stack.Length / 2;
                    var hints = new (decimal, decimal)[numberOfEdgeHints];

                    var firstStartY = ctx.Stack.PopBottom();
                    var endY = firstStartY + ctx.Stack.PopBottom();

                    hints[0] = (firstStartY, endY);

                    var currentY = endY;

                    for (var i = 1; i < numberOfEdgeHints; i++)
                    {
                        var dyStart = ctx.Stack.PopBottom();
                        var dyEnd = ctx.Stack.PopBottom();

                        hints[i] = (currentY + dyStart, currentY + dyStart + dyEnd);
                        currentY = currentY + dyStart + dyEnd;
                    }

                    ctx.AddHorizontalStemHints(hints);

                    ctx.Stack.Clear();
                })
            },
            {
                3,  new LazyType2Command("vstem", ctx =>
                {
                    var numberOfEdgeHints = ctx.Stack.Length / 2;
                    var hints = new (decimal, decimal)[numberOfEdgeHints];

                    var firstStartX = ctx.Stack.PopBottom();
                    var endX = firstStartX + ctx.Stack.PopBottom();

                    hints[0] = (firstStartX, endX);

                    var currentX = endX;

                    for (var i = 1; i < numberOfEdgeHints; i++)
                    {
                        var dxStart = ctx.Stack.PopBottom();
                        var dxEnd = ctx.Stack.PopBottom();

                        hints[i] = (currentX + dxStart, currentX + dxStart + dxEnd);
                        currentX = currentX + dxStart + dxEnd;
                    }

                    ctx.AddVerticalStemHints(hints);

                    ctx.Stack.Clear();
                })
            },
            { 4,
                new LazyType2Command("vmoveto", ctx =>
                {
                    var dy = ctx.Stack.PopBottom();

                    ctx.Path.MoveTo(ctx.CurrentLocation.X, ctx.CurrentLocation.Y + dy);
                    ctx.CurrentLocation = ctx.CurrentLocation.MoveY(dy);

                    ctx.Stack.Clear();
                })
            },
            { 5,
                new LazyType2Command("rlineto", ctx =>
                {
                    var numberOfLines = ctx.Stack.Length / 2;

                    for (var i = 0; i < numberOfLines; i++)
                    {
                        var dxa = ctx.Stack.PopBottom();
                        var dya = ctx.Stack.PopBottom();

                        ctx.AddRelativeLine(dxa, dya);
                    }

                    ctx.Stack.Clear();
                })
            },
            { 6,
                new LazyType2Command("hlineto", ctx =>
                {
                    /*
                     * Appends a horizontal line of length dx1 to the current point.
                     * With an odd number of arguments, subsequent argument pairs are interpreted as alternating values of dy and dx.
                     * With an even number of arguments, the arguments are interpreted as alternating horizontal and vertical lines (dx and dy).
                     * The number of lines is determined from the number of arguments on the stack.
                     */
                    var isOdd = ctx.Stack.Length % 2 != 0;

                    var dx1 = ctx.Stack.PopBottom();
                    ctx.AddRelativeHorizontalLine(dx1);

                    var numberOfAdditionalLines = ctx.Stack.Length;
                    for (var i = 0; i < numberOfAdditionalLines; i++)
                    {
                        var isDeltaY = (isOdd && i % 2 == 0) || (!isOdd && i % 2 != 0);
                        if (isDeltaY)
                        {
                            var dya = ctx.Stack.PopBottom();
                            ctx.AddRelativeVerticalLine(dya);
                        }
                        else
                        {
                            var dxa = ctx.Stack.PopBottom();
                            ctx.AddRelativeHorizontalLine(dxa);
                        }
                    }

                    ctx.Stack.Clear();
                })
            },
            { 7,
                new LazyType2Command("vlineto", ctx =>
                {
                    var isOdd = ctx.Stack.Length % 2 != 0;

                    var dy1 = ctx.Stack.PopBottom();
                    ctx.AddRelativeVerticalLine(dy1);

                    var numberOfAdditionalLines = ctx.Stack.Length;
                    for (var i = 0; i < numberOfAdditionalLines; i++)
                    {
                        var isDeltaY = (isOdd && i % 2 != 0) || (!isOdd && i % 2 == 0);

                        if (isDeltaY)
                        {
                            var dya = ctx.Stack.PopBottom();
                            ctx.AddRelativeVerticalLine(dya);
                        }
                        else
                        {
                            var dxa = ctx.Stack.PopBottom();
                            ctx.AddRelativeHorizontalLine(dxa);
                        }
                    }

                    ctx.Stack.Clear();
                })
            },
            { 8,
                new LazyType2Command("rrcurveto", ctx =>
                {
                    var curveCount = ctx.Stack.Length / 6;
                    for (var i = 0; i < curveCount; i++)
                    {
                        ctx.AddRelativeBezierCurve(ctx.Stack.PopBottom(), ctx.Stack.PopBottom(), ctx.Stack.PopBottom(),
                            ctx.Stack.PopBottom(),
                            ctx.Stack.PopBottom(),
                            ctx.Stack.PopBottom());
                    }

                    ctx.Stack.Clear();
                })
            },
            { 10,  new LazyType2Command("callsubr", x => { })},
            { 11,  new LazyType2Command("return", x => { })},
            { 14,  new LazyType2Command("endchar", ctx =>
                {
                    ctx.Stack.Clear();
                })
            },
            { 18,  new LazyType2Command("hstemhm", ctx =>
                {
                    // Same as vstem except the charstring contains hintmask
                    var numberOfEdgeHints = ctx.Stack.Length / 2;
                    var hints = new (decimal, decimal)[numberOfEdgeHints];

                    var firstStartY = ctx.Stack.PopBottom();
                    var endY = firstStartY + ctx.Stack.PopBottom();

                    hints[0] = (firstStartY, endY);

                    var currentY = endY;

                    for (var i = 1; i < numberOfEdgeHints; i++)
                    {
                        var dyStart = ctx.Stack.PopBottom();
                        var dyEnd = ctx.Stack.PopBottom();

                        hints[i] = (currentY + dyStart, currentY + dyStart + dyEnd);
                        currentY = currentY + dyStart + dyEnd;
                    }

                    ctx.AddHorizontalStemHints(hints);

                    ctx.Stack.Clear();
                })
            },
            {
                19,  new LazyType2Command("hintmask", ctx =>
                {
                    // TODO: record this mask somewhere
                    ctx.Stack.Clear();
                })
            },
            {
                20,  new LazyType2Command("cntrmask", ctx =>
                {
                    // TODO: record this mask somewhere
                    ctx.Stack.Clear();
                })
            },
            { 21,
                new LazyType2Command("rmoveto", ctx =>
                {
                    var dx = ctx.Stack.PopBottom();
                    var dy = ctx.Stack.PopBottom();

                    var newLocation = new PdfPoint(ctx.CurrentLocation.X + dx,
                        ctx.CurrentLocation.Y + dy);

                    ctx.Path.MoveTo(newLocation.X, newLocation.Y);
                    ctx.CurrentLocation = newLocation;

                    ctx.Stack.Clear();
                })
            },
            { 22,
                new LazyType2Command("hmoveto", ctx =>
                {
                    var dx = ctx.Stack.PopBottom();

                    ctx.Path.MoveTo(ctx.CurrentLocation.X + dx, ctx.CurrentLocation.Y);
                    ctx.CurrentLocation = ctx.CurrentLocation.MoveX(dx);

                    ctx.Stack.Clear();
                })
            },
            { 23,  new LazyType2Command("vstemh", ctx =>
                {
                    // Same as vstem except the charstring contains hintmask
                    var numberOfEdgeHints = ctx.Stack.Length / 2;
                    var hints = new (decimal, decimal)[numberOfEdgeHints];

                    var firstStartX = ctx.Stack.PopBottom();
                    var endX = firstStartX + ctx.Stack.PopBottom();

                    hints[0] = (firstStartX, endX);

                    var currentX = endX;

                    for (var i = 1; i < numberOfEdgeHints; i++)
                    {
                        var dxStart = ctx.Stack.PopBottom();
                        var dxEnd = ctx.Stack.PopBottom();

                        hints[i] = (currentX + dxStart, currentX + dxStart + dxEnd);
                        currentX = currentX + dxStart + dxEnd;
                    }

                    ctx.AddVerticalStemHints(hints);

                    ctx.Stack.Clear();
                })
            },
            {
                24,
                new LazyType2Command("rcurveline", ctx =>
                {
                    var numberOfCurves = (ctx.Stack.Length - 2) / 6;
                    for (var i = 0; i < numberOfCurves; i++)
                    {
                        ctx.AddRelativeBezierCurve(ctx.Stack.PopBottom(), ctx.Stack.PopBottom(), ctx.Stack.PopBottom(),
                            ctx.Stack.PopBottom(),
                            ctx.Stack.PopBottom(),
                            ctx.Stack.PopBottom());
                    }

                    ctx.AddRelativeLine(ctx.Stack.PopBottom(), ctx.Stack.PopBottom());
                    ctx.Stack.Clear();
                })
            },
            { 25,
                new LazyType2Command("rlinecurve", ctx =>
                {
                    var numberOfLines = (ctx.Stack.Length - 6) / 2;
                    for (var i = 0; i < numberOfLines; i++)
                    {
                        ctx.AddRelativeLine(ctx.Stack.PopBottom(), ctx.Stack.PopBottom());
                    }

                    ctx.AddRelativeBezierCurve(ctx.Stack.PopBottom(), ctx.Stack.PopBottom(), ctx.Stack.PopBottom(),
                        ctx.Stack.PopBottom(),
                        ctx.Stack.PopBottom(),
                        ctx.Stack.PopBottom());

                    ctx.Stack.Clear();
                })
            },
            { 26,
                new LazyType2Command("vvcurveto", ctx =>
                {
                    // dx1? {dya dxb dyb dyc}+
                    var hasDeltaXFirstCurve = ctx.Stack.Length % 4 != 0;

                    var numberOfCurves = ctx.Stack.Length / 4;
                    for (var i = 0; i < numberOfCurves; i++)
                    {
                        var dx1 = 0m;
                        if (i == 0 && hasDeltaXFirstCurve)
                        {
                            dx1 = ctx.Stack.PopBottom();
                        }

                        var dy1 = ctx.Stack.PopBottom();
                        var dx2 = ctx.Stack.PopBottom();
                        var dy2 = ctx.Stack.PopBottom();
                        var dy3 = ctx.Stack.PopBottom();

                        ctx.AddRelativeBezierCurve(dx1, dy1, dx2, dy2, 0, dy3);
                    }

                    ctx.Stack.Clear();
                })
            },
            { 27,  new LazyType2Command("hhcurveto", ctx =>
                {
                    var hasDeltaYFirstCurve = ctx.Stack.Length % 4 != 0;

                    if (hasDeltaYFirstCurve)
                    {
                        var dy1 = ctx.Stack.PopBottom();
                        var dx1 = ctx.Stack.PopBottom();
                        var dx2 = ctx.Stack.PopBottom();
                        var dy2 = ctx.Stack.PopBottom();
                        var dx3 = ctx.Stack.PopBottom();

                        ctx.AddRelativeBezierCurve(dx1, dy1, dx2, dy2, dx3, 0);
                    }

                    var numberOfCurves = ctx.Stack.Length / 4;
                    for (var i = 0; i < numberOfCurves; i++)
                    {
                        var dx1 = ctx.Stack.PopBottom();
                        var dx2 = ctx.Stack.PopBottom();
                        var dy2 = ctx.Stack.PopBottom();
                        var dx3 = ctx.Stack.PopBottom();

                        ctx.AddRelativeBezierCurve(dx1, 0, dx2, dy2, dx3, 0);
                    }

                    ctx.Stack.Clear();
                })
            },
            { 29,  new LazyType2Command("callgsubr", x => { })},
            { 30,
                new LazyType2Command("vhcurveto", ctx =>
                {
                    var remainder = ctx.Stack.Length % 8;

                    if (remainder <= 1)
                    {
                        // {dya dxb dyb dxc dxd dxe dye dyf}+ dxf?
                        // 2 curves, 1st starts vertical ends horizontal, second starts horizontal ends vertical

                        var numberOfCurves = (ctx.Stack.Length - remainder)/8;
                        for (var i = 0; i < numberOfCurves; i++)
                        {
                            // First curve
                            {
                                var dy1 = ctx.Stack.PopBottom();
                                var dx2 = ctx.Stack.PopBottom();
                                var dy2 = ctx.Stack.PopBottom();
                                var dx3 = ctx.Stack.PopBottom();
                                ctx.AddRelativeBezierCurve(0, dy1, dx2, dy2, dx3, 0);
                            }
                            // Second curve
                            {
                                var dx1 = ctx.Stack.PopBottom();
                                var dx2 = ctx.Stack.PopBottom();
                                var dy2 = ctx.Stack.PopBottom();
                                var dy3 = ctx.Stack.PopBottom();
                                var dx3 = 0m;

                                if (i == numberOfCurves - 1 && remainder == 1)
                                {
                                    dx3 = ctx.Stack.PopBottom();
                                }

                                ctx.AddRelativeBezierCurve(dx1, 0, dx2, dy2, dx3, dy3);
                            }
                        }
                    }
                    else if (remainder == 4 || remainder == 5)
                    {
                        // dy1 dx2 dy2 dx3 {dxa dxb dyb dyc dyd dxe dye dxf}* dyf?
                        var numberOfCurves = (ctx.Stack.Length - remainder) / 8;

                        {
                            var dy1 = ctx.Stack.PopBottom();
                            var dx2 = ctx.Stack.PopBottom();
                            var dy2 = ctx.Stack.PopBottom();
                            var dx3 = ctx.Stack.PopBottom();
                            ctx.AddRelativeBezierCurve(0, dy1, dx2, dy2, dx3, 0);
                        }

                        for (var i = 0; i < numberOfCurves; i++)
                        {
                            // First curve
                            {
                                var dx1 = ctx.Stack.PopBottom();
                                var dx2 = ctx.Stack.PopBottom();
                                var dy2 = ctx.Stack.PopBottom();
                                var dy3 = ctx.Stack.PopBottom();
                                ctx.AddRelativeBezierCurve(dx1, 0, dx2, dy2, 0, dy3);
                            }
                            // Second curve
                            {
                                var dy1 = ctx.Stack.PopBottom();
                                var dx2 = ctx.Stack.PopBottom();
                                var dy2 = ctx.Stack.PopBottom();
                                var dx3 = ctx.Stack.PopBottom();
                                var dy3 = 0m;

                                if (i == numberOfCurves - 1 && remainder == 5)
                                {
                                    dy3 = ctx.Stack.PopBottom();
                                }

                                ctx.AddRelativeBezierCurve(0, dy1, dx2, dy2, dx3, dy3);
                            }
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected number of arguments for vhcurve to: {ctx.Stack.Length}.");
                    }

                    ctx.Stack.Clear();
                })
            },
            { 31,
                new LazyType2Command("hvcurveto", ctx =>
                {
                    var remainder = ctx.Stack.Length % 8;

                    if (remainder <= 1)
                    {
                        // {dxa dxb dyb dyc dyd dxe dye dxf}+ dyf?
                        // 2 curves, 1st starts horizontal ends vertical, second starts vertical ends horizontal

                        var numberOfCurves = (ctx.Stack.Length - remainder)/8;
                        for (var i = 0; i < numberOfCurves; i++)
                        {
                            // First curve
                            {
                                var dx1 = ctx.Stack.PopBottom();
                                var dx2 = ctx.Stack.PopBottom();
                                var dy2 = ctx.Stack.PopBottom();
                                var dy3 = ctx.Stack.PopBottom();
                                ctx.AddRelativeBezierCurve(dx1, 0, dx2, dy2, 0, dy3);
                            }
                            // Second curve
                            {
                                var dy1 = ctx.Stack.PopBottom();
                                var dx2 = ctx.Stack.PopBottom();
                                var dy2 = ctx.Stack.PopBottom();
                                var dx3 = ctx.Stack.PopBottom();
                                var dy3 = 0m;

                                if (i == numberOfCurves - 1 && remainder == 1)
                                {
                                    dy3 = ctx.Stack.PopBottom();
                                }

                                ctx.AddRelativeBezierCurve(0, dy1, dx2, dy2, dx3, dy3);
                            }
                        }
                    }
                    else if (remainder == 4 || remainder == 5)
                    {
                        // dx1 dx2 dy2 dy3 {dya dxb dyb dxc dxd dxe dye dyf}* dxf?
                        var numberOfCurves = (ctx.Stack.Length - remainder) / 8;

                        {
                            var dx1 = ctx.Stack.PopBottom();
                            var dx2 = ctx.Stack.PopBottom();
                            var dy2 = ctx.Stack.PopBottom();
                            var dy3 = ctx.Stack.PopBottom();
                            ctx.AddRelativeBezierCurve(dx1, 0, dx2, dy2, 0, dy3);
                        }

                        for (var i = 0; i < numberOfCurves; i++)
                        {
                            // First curve
                            {
                                var dy1 = ctx.Stack.PopBottom();
                                var dx2 = ctx.Stack.PopBottom();
                                var dy2 = ctx.Stack.PopBottom();
                                var dx3 = ctx.Stack.PopBottom();
                                ctx.AddRelativeBezierCurve(0, dy1, dx2, dy2, dx3, 0);
                            }
                            // Second curve
                            {
                                var dx1 = ctx.Stack.PopBottom();
                                var dx2 = ctx.Stack.PopBottom();
                                var dy2 = ctx.Stack.PopBottom();
                                var dy3 = ctx.Stack.PopBottom();
                                var dx3 = 0m;

                                if (i == numberOfCurves - 1 && remainder == 5)
                                {
                                    dx3 = ctx.Stack.PopBottom();
                                }

                                ctx.AddRelativeBezierCurve(dx1, 0, dx2, dy2, dx3, dy3);
                            }
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unexpected number of arguments for hvcurve to: {ctx.Stack.Length}.");
                    }

                    ctx.Stack.Clear();
                })
            }
        };

        private static readonly IReadOnlyDictionary<int, LazyType2Command> TwoByteCommandStore = new Dictionary<int, LazyType2Command>
        {
            { 3,  new LazyType2Command("and", ctx => ctx.Stack.Push(ctx.Stack.PopTop() != 0 && ctx.Stack.PopTop() != 0 ? 1 : 0))},
            { 4,  new LazyType2Command("or", ctx =>
            {
                var arg1 = ctx.Stack.PopTop();
                var arg2 = ctx.Stack.PopTop();
                ctx.Stack.Push(arg1 != 0 || arg2 != 0 ? 1 : 0);
            })},
            { 5,  new LazyType2Command("not", ctx => ctx.Stack.Push(ctx.Stack.PopTop() == 0 ? 1 : 0))},
            { 9,  new LazyType2Command("abs", ctx => ctx.Stack.Push(Math.Abs(ctx.Stack.PopTop())))},
            { 10,  new LazyType2Command("add", ctx => ctx.Stack.Push(ctx.Stack.PopTop() + ctx.Stack.PopTop()))},
            {
                11,  new LazyType2Command("sub", ctx =>
                {
                    var num1 = ctx.Stack.PopTop();
                    var num2 = ctx.Stack.PopTop();
                    ctx.Stack.Push(num2 - num1);
                })
            },
            { 12,  new LazyType2Command("div", ctx => ctx.Stack.Push(ctx.Stack.PopTop()/ctx.Stack.PopTop()))},
            { 14,  new LazyType2Command("neg", ctx => ctx.Stack.Push(-1 * Math.Abs(ctx.Stack.PopTop())))},
            // ReSharper disable once EqualExpressionComparison
            { 15,  new LazyType2Command("eq", ctx => ctx.Stack.Push(ctx.Stack.PopTop() == ctx.Stack.PopTop() ? 1 : 0))},
            { 18,  new LazyType2Command("drop", ctx => ctx.Stack.PopTop())},
            { 20,  new LazyType2Command("put", ctx => ctx.AddToTransientArray(ctx.Stack.PopTop(), (int)ctx.Stack.PopTop()))},
            { 21,  new LazyType2Command("get", ctx => ctx.Stack.Push(ctx.GetFromTransientArray((int)ctx.Stack.PopTop())))},
            { 22,  new LazyType2Command("ifelse", x => { })},
            // TODO: Random, do we want to support this?
            { 23,  new LazyType2Command("random", ctx => ctx.Stack.Push(0.5m))},
            { 24,  new LazyType2Command("mul", ctx => ctx.Stack.Push(ctx.Stack.PopTop() * ctx.Stack.PopTop()))},
            { 26,  new LazyType2Command("sqrt", ctx => ctx.Stack.Push((decimal)Math.Sqrt((double)ctx.Stack.PopTop())))},
            {
                27,  new LazyType2Command("dup", ctx =>
                {
                    var val = ctx.Stack.PopTop();
                    ctx.Stack.Push(val);
                    ctx.Stack.Push(val);
                })
            },
            { 28,  new LazyType2Command("exch", ctx =>
            {
                var num1 = ctx.Stack.PopTop();
                var num2 = ctx.Stack.PopTop();
                ctx.Stack.Push(num1);
                ctx.Stack.Push(num2);
            })},
            { 29,  new LazyType2Command("index", ctx =>
            {
                var index = ctx.Stack.PopTop();
                var val = ctx.Stack.CopyElementAt((int) index);
                ctx.Stack.Push(val);
            })},
            {
                30,  new LazyType2Command("roll", ctx =>
                {
                    // TODO: roll
                })
            },
            {
                34,  new LazyType2Command("hflex", ctx =>
                {
                    //  dx1 dx2 dy2 dx3 dx4 dx5 dx6 
                    // Two Bezier curves with an fd of 50

                    // TODO: implement
                    ctx.Stack.Clear();
                })
            },
            {
                35,  new LazyType2Command("flex", ctx =>
                {
                    //  dx1 dy1 dx2 dy2 dx3 dy3 dx4 dy4 dx5 dy5 dx6 dy6 fd
                    // Two Bezier curves will be represented as a straight line when depth less than fd character space units
                    ctx.AddRelativeBezierCurve(ctx.Stack.PopBottom(), ctx.Stack.PopBottom(), ctx.Stack.PopBottom(),
                        ctx.Stack.PopBottom(),
                        ctx.Stack.PopBottom(),
                        ctx.Stack.PopBottom());

                    ctx.AddRelativeBezierCurve(ctx.Stack.PopBottom(), ctx.Stack.PopBottom(), ctx.Stack.PopBottom(),
                        ctx.Stack.PopBottom(),
                        ctx.Stack.PopBottom(),
                        ctx.Stack.PopBottom());

                    ctx.Stack.PopBottom();
                    // TODO: record flex depth for this Bezier pair

                    ctx.Stack.Clear();
                })
            },
            { 36,  new LazyType2Command("hflex1", ctx =>
            {
                // TODO: implement
                ctx.Stack.Clear();
            })},
            { 37,  new LazyType2Command("flex1", ctx =>
            {
                // dx1 dy1 dx2 dy2 dx3 dy3 dx4 dy4 dx5 dy5 d6 
                // d6 is either dx or dy

                var dx1 = ctx.Stack.PopBottom();
                var dy1 = ctx.Stack.PopBottom();
                var dx2 = ctx.Stack.PopBottom();
                var dy2 = ctx.Stack.PopBottom();
                var dx3 = ctx.Stack.PopBottom();
                var dy3 = ctx.Stack.PopBottom();

                var dx4 = ctx.Stack.PopBottom();
                var dy4 = ctx.Stack.PopBottom();
                var dx5 = ctx.Stack.PopBottom();
                var dy5 = ctx.Stack.PopBottom();
                var d6 = ctx.Stack.PopBottom();

                var dx = dx1 + dx2 + dx3 + dx4 + dx5;
                var dy = dy1 + dy2 + dy3 + dy4 + dy5;

                var lastPointIsX = Math.Abs(dx) > Math.Abs(dy);
                ctx.AddRelativeBezierCurve(dx1, dy1, dx2, dy2, dx3, dy3);
                ctx.AddRelativeBezierCurve(dx4, dy4, dx5, dy5, lastPointIsX ? d6 : 0, lastPointIsX ? 0 : d6);
                ctx.Stack.Clear();
            })},
        };

        public static Type2CharStrings Parse([NotNull] IReadOnlyList<IReadOnlyList<byte>> charStringBytes,
            [NotNull] CompactFontFormatIndex localSubroutines,
            [NotNull] CompactFontFormatIndex globalSubroutines)
        {
            if (charStringBytes == null)
            {
                throw new ArgumentNullException(nameof(charStringBytes));
            }

            if (localSubroutines == null)
            {
                throw new ArgumentNullException(nameof(localSubroutines));
            }

            if (globalSubroutines == null)
            {
                throw new ArgumentNullException(nameof(globalSubroutines));
            }

            var charStrings = new Dictionary<int, Type2CharStrings.CommandSequence>();
            for (var i = 0; i < charStringBytes.Count; i++)
            {
                var charString = charStringBytes[i];
                var sequence = ParseSingle(charString);
                charStrings[i] = new Type2CharStrings.CommandSequence(sequence);
            }

            return new Type2CharStrings(charStrings, new Dictionary<int, Type2CharStrings.CommandSequence>());
        }

        private static IReadOnlyList<Union<decimal, LazyType2Command>> ParseSingle(IReadOnlyList<byte> bytes)
        {
            var instructions = new List<Union<decimal, LazyType2Command>>();
            for (var i = 0; i < bytes.Count; i++)
            {
                var b = bytes[i];
                if (b <= 31 && b != 28)
                {
                    var command = GetCommand(b, bytes, ref i);
                    instructions.Add(Union<decimal, LazyType2Command>.Two(command));
                }
                else
                {
                    var number = InterpretNumber(b, bytes, ref i);
                    instructions.Add(Union<decimal, LazyType2Command>.One(number));
                }
            }

            return instructions;
        }

        /// <summary>
        /// The Type 2 interpretation of a number with an initial byte value of 255 differs from how it is interpreted in the Type 1 format
        /// and 28 has a special meaning.
        /// </summary>
        private static decimal InterpretNumber(byte b, IReadOnlyList<byte> bytes, ref int i)
        {
            if (b == 28)
            {
                return bytes[++i] << 8 | bytes[++i];
            }

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

            /*
             * If the charstring byte contains the value 255, the next four bytes indicate a two's complement signed number.
             * The first of these the four bytes contains the highest order bits, the second byte contains the next higher order bits
             * and the fourth byte contains the lowest order bits.
             * This number is interpreted as a Fixed; that is, a signed number with 16 bits of fraction
             */
            var lead = bytes[++i] << 8 | bytes[++i];
            var fractionalPart = bytes[++i] << 8 | bytes[++i];

            return lead + (fractionalPart / 65535m);
        }

        private static LazyType2Command GetCommand(byte b, IReadOnlyList<byte> bytes, ref int i)
        {
            if (b == 12)
            {
                var b2 = bytes[++i];
                if (TwoByteCommandStore.TryGetValue(b2, out var commandTwoByte))
                {
                    return commandTwoByte;
                }

                return new LazyType2Command($"unknown: {b} {b2}", x => { });
            }

            if (SingleByteCommandStore.TryGetValue(b, out var command))
            {
                return command;
            }

            return new LazyType2Command($"unknown: {b}", x => { });
        }
    }
}
