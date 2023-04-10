﻿// ReSharper disable CompareOfFloatsByEqualityOperator
namespace UglyToad.PdfPig.Fonts.CompactFontFormat.CharStrings
{
    using Charsets;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Decodes the commands and numbers making up a Type 2 CharString. A Type 2 CharString extends on the Type 1 CharString format.
    /// Compared to the Type 1 format, the Type 2 encoding offers smaller size and an opportunity for better rendering quality and
    /// performance. The Type 2 charstring operators are (with one exception) a superset of the Type 1 operators.
    /// </summary>
    /// <remarks>
    /// A Type 2 charstring program is a sequence of unsigned 8-bit bytes that encode numbers and operators.
    /// The byte value specifies a operator, a number, or subsequent bytes that are to be interpreted in a specific manner
    /// </remarks>
    internal static class Type2CharStringParser
    {
        private const byte HstemByte = 1;
        private const byte VstemByte = 3;
        private const byte HstemhmByte = 18;
        private const byte HintmaskByte = 19;
        private const byte CntrmaskByte = 20;
        private const byte VstemhmByte = 23;

        private static readonly HashSet<byte> HintingCommandBytes = new HashSet<byte>
        {
            HstemByte,
            VstemByte,
            HstemhmByte,
            VstemhmByte
        };

        private static readonly IReadOnlyDictionary<byte, LazyType2Command> SingleByteCommandStore = new Dictionary<byte, LazyType2Command>
        {
            { HstemByte,  new LazyType2Command("hstem", 2, ctx =>
                {
                    var numberOfEdgeHints = ctx.Stack.Length / 2;
                    var hints = new (double, double)[numberOfEdgeHints];

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
                VstemByte,  new LazyType2Command("vstem", 2, ctx =>
                {
                    var numberOfEdgeHints = ctx.Stack.Length / 2;
                    var hints = new (double, double)[numberOfEdgeHints];

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
                new LazyType2Command("vmoveto", 1, ctx =>
                {
                    var dy = ctx.Stack.PopBottom();
                    ctx.AddVerticallMoveTo(dy);
                    ctx.Stack.Clear();
                })
            },
            { 5,
                new LazyType2Command("rlineto", 2, ctx =>
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
                new LazyType2Command("hlineto", 1, ctx =>
                {
                    /*
                     * Appends a horizontal line of length dx1 to the current point.
                     * With an odd number of arguments, subsequent argument pairs are interpreted as alternating values of dy and dx.
                     * With an even number of arguments, the arguments are interpreted as alternating horizontal and vertical lines (dx and dy).
                     * The number of lines is determined from the number of arguments on the stack.
                     */
                    var isOdd = ctx.Stack.Length % 2 != 0;

                    var numberOfAdditionalLines = ctx.Stack.Length - (isOdd ? 1 : 0);

                    if (isOdd)
                    {
                        var dx1 = ctx.Stack.PopBottom();
                        ctx.AddRelativeHorizontalLine(dx1);

                        for (var i = 0; i < numberOfAdditionalLines; i+= 2)
                        {
                            ctx.AddRelativeVerticalLine(ctx.Stack.PopBottom());
                            ctx.AddRelativeHorizontalLine(ctx.Stack.PopBottom());
                        }
                    }
                    else
                    {
                        for (var i = 0; i < numberOfAdditionalLines; i+= 2)
                        {
                            ctx.AddRelativeHorizontalLine(ctx.Stack.PopBottom());
                            ctx.AddRelativeVerticalLine(ctx.Stack.PopBottom());
                        }
                    }

                    ctx.Stack.Clear();
                })
            },
            { 7,
                new LazyType2Command("vlineto", 1, ctx =>
                {
                    var isOdd = ctx.Stack.Length % 2 != 0;

                    var numberOfAdditionalLines = ctx.Stack.Length - (isOdd ? 1 : 0);

                    if (isOdd)
                    {
                    var dy1 = ctx.Stack.PopBottom();
                    ctx.AddRelativeVerticalLine(dy1);

                        for (var i = 0; i < numberOfAdditionalLines; i+=2)
                        {
                            ctx.AddRelativeHorizontalLine(ctx.Stack.PopBottom());
                            ctx.AddRelativeVerticalLine(ctx.Stack.PopBottom());
                        }
                    }
                    else
                    {
                        for (var i = 0; i < numberOfAdditionalLines; i+=2)
                        {
                            ctx.AddRelativeVerticalLine(ctx.Stack.PopBottom());
                            ctx.AddRelativeHorizontalLine(ctx.Stack.PopBottom());
                        }
                    }

                    ctx.Stack.Clear();
                })
            },
            { 8,
                new LazyType2Command("rrcurveto", 6, ctx =>
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
            { 10,  new LazyType2Command("callsubr", 1, ctx => {})},
            { 11,  new LazyType2Command("return", 0, ctx => {})},
            { 14,  new LazyType2Command("endchar", 0, ctx =>
                {
                    ctx.Stack.Clear();
                })
            },
            { HstemhmByte,  new LazyType2Command("hstemhm", 2, ctx =>
                {
                    // Same as vstem except the charstring contains hintmask
                    var numberOfEdgeHints = ctx.Stack.Length / 2;
                    var hints = new (double, double)[numberOfEdgeHints];

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
                HintmaskByte,  new LazyType2Command("hintmask", 0, ctx =>
                {
                    // TODO: record this mask somewhere
                    ctx.Stack.Clear();
                })
            },
            {
                CntrmaskByte,  new LazyType2Command("cntrmask", 0,ctx =>
                {
                    // TODO: record this mask somewhere
                    ctx.Stack.Clear();
                })
            },
            { 21,
                new LazyType2Command("rmoveto", 2, ctx =>
                {
                    var dx = ctx.Stack.PopBottom();
                    var dy = ctx.Stack.PopBottom();
                    ctx.AddRelativeMoveTo(dx,dy);
                    ctx.Stack.Clear();
                })
            },
            { 22,
                new LazyType2Command("hmoveto", 1, ctx =>
                {
                    var dx = ctx.Stack.PopBottom();
                    ctx.AddHorizontalMoveTo(dx);
                    ctx.Stack.Clear();
                })
            },
            { VstemhmByte,  new LazyType2Command("vstemhm", 2, ctx =>
                {
                    // Same as vstem except the charstring contains hintmask
                    var numberOfEdgeHints = ctx.Stack.Length / 2;
                    var hints = new (double, double)[numberOfEdgeHints];

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
                new LazyType2Command("rcurveline", 8, ctx =>
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
                new LazyType2Command("rlinecurve", 8, ctx =>
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
                new LazyType2Command("vvcurveto", 4, ctx =>
                {
                    // dx1? {dya dxb dyb dyc}+
                    var hasDeltaXFirstCurve = ctx.Stack.Length % 4 != 0;

                    var numberOfCurves = ctx.Stack.Length / 4;
                    for (var i = 0; i < numberOfCurves; i++)
                    {
                        var dx1 = 0.0;
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
            { 27,  new LazyType2Command("hhcurveto", 4, ctx =>
                {
                    //  dy1? {dxa dxb dyb dxc}+
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
            { 29,  new LazyType2Command("callgsubr", 1, ctx => {})
            },
            { 30,
                new LazyType2Command("vhcurveto", 4, ctx =>
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
                                var dx3 = 0.0;

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
                            var dy3 = ctx.Stack.Length == 1 ? ctx.Stack.PopBottom() : 0;
                            ctx.AddRelativeBezierCurve(0, dy1, dx2, dy2, dx3, dy3);
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
                                var dy3 = 0.0;

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
                new LazyType2Command("hvcurveto", 4, ctx =>
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
                                var dy3 = 0.0;

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
                            var dx3 = ctx.Stack.Length == 1 ? ctx.Stack.PopBottom() : 0;
                            ctx.AddRelativeBezierCurve(dx1, 0, dx2, dy2, dx3, dy3);
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
                                var dx3 = 0.0;

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
            },
            { 255, new LazyType2Command("unknown", -1, x => {}) }
        };



        private static readonly IReadOnlyDictionary<byte, LazyType2Command> TwoByteCommandStore = new Dictionary<byte, LazyType2Command>
        {
            { 3,  new LazyType2Command("and", 2, ctx => ctx.Stack.Push(ctx.Stack.PopTop() != 0 && ctx.Stack.PopTop() != 0 ? 1 : 0))},
            { 4,  new LazyType2Command("or", 2,ctx =>
            {
                var arg1 = ctx.Stack.PopTop();
                var arg2 = ctx.Stack.PopTop();
                ctx.Stack.Push(arg1 != 0 || arg2 != 0 ? 1 : 0);
            })},
            { 5,  new LazyType2Command("not", 1,ctx => ctx.Stack.Push(ctx.Stack.PopTop() == 0 ? 1 : 0))},
            { 9,  new LazyType2Command("abs", 1, ctx => ctx.Stack.Push(Math.Abs(ctx.Stack.PopTop())))},
            { 10,  new LazyType2Command("add", 2, ctx => ctx.Stack.Push(ctx.Stack.PopTop() + ctx.Stack.PopTop()))},
            {
                11,  new LazyType2Command("sub", 2, ctx =>
                {
                    var num1 = ctx.Stack.PopTop();
                    var num2 = ctx.Stack.PopTop();
                    ctx.Stack.Push(num2 - num1);
                })
            },
            { 12,  new LazyType2Command("div", 2, ctx => ctx.Stack.Push(ctx.Stack.PopTop()/ctx.Stack.PopTop()))},
            { 14,  new LazyType2Command("neg", 1, ctx => ctx.Stack.Push(-1 * Math.Abs(ctx.Stack.PopTop())))},
            // ReSharper disable once EqualExpressionComparison
            { 15,  new LazyType2Command("eq", 2, ctx => ctx.Stack.Push(ctx.Stack.PopTop() == ctx.Stack.PopTop() ? 1 : 0))},
            { 18,  new LazyType2Command("drop", 1, ctx => ctx.Stack.PopTop())},
            { 20,  new LazyType2Command("put", 2, ctx => ctx.AddToTransientArray(ctx.Stack.PopTop(), (int)ctx.Stack.PopTop()))},
            { 21,  new LazyType2Command("get", 1, ctx => ctx.Stack.Push(ctx.GetFromTransientArray((int)ctx.Stack.PopTop())))},
            { 22,  new LazyType2Command("ifelse", 4, x => { })},
            // TODO: Random, do we want to support this?
            { 23,  new LazyType2Command("random", 0, ctx => ctx.Stack.Push(0.5))},
            { 24,  new LazyType2Command("mul", 2, ctx => ctx.Stack.Push(ctx.Stack.PopTop() * ctx.Stack.PopTop()))},
            { 26,  new LazyType2Command("sqrt", 1, ctx => ctx.Stack.Push(Math.Sqrt(ctx.Stack.PopTop())))},
            {
                27,  new LazyType2Command("dup", 1, ctx =>
                {
                    var val = ctx.Stack.PopTop();
                    ctx.Stack.Push(val);
                    ctx.Stack.Push(val);
                })
            },
            { 28,  new LazyType2Command("exch", 2, ctx =>
            {
                var num1 = ctx.Stack.PopTop();
                var num2 = ctx.Stack.PopTop();
                ctx.Stack.Push(num1);
                ctx.Stack.Push(num2);
            })},
            { 29,  new LazyType2Command("index", 2, ctx =>
            {
                var index = ctx.Stack.PopTop();
                var val = ctx.Stack.CopyElementAt((int) index);
                ctx.Stack.Push(val);
            })},
            {
                30,  new LazyType2Command("roll", 3, ctx =>
                {
                    // TODO: roll
                })
            },
            {
                34,  new LazyType2Command("hflex", 7, ctx =>
                {
                    //  dx1 dx2 dy2 dx3 dx4 dx5 dx6 
                    // Two Bezier curves with an fd of 50

                    // TODO: implement
                    ctx.Stack.Clear();
                })
            },
            {
                35,  new LazyType2Command("flex", 13, ctx =>
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
            { 36,  new LazyType2Command("hflex1", 9, ctx =>
            {
                // TODO: implement
                ctx.Stack.Clear();
            })},
            { 37,  new LazyType2Command("flex1", 11, ctx =>
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

        public static LazyType2Command GetCommand(Type2CharStrings.CommandSequence.CommandIdentifier identifier)
        {
            if (identifier.IsMultiByteCommand)
            {
                return TwoByteCommandStore[identifier.CommandId];
            }

            return SingleByteCommandStore[identifier.CommandId];
        }

        public static Type2CharStrings Parse(IReadOnlyList<IReadOnlyList<byte>> charStringBytes,
            CompactFontFormatSubroutinesSelector subroutinesSelector, ICompactFontFormatCharset charset)
        {
            if (charStringBytes == null)
            {
                throw new ArgumentNullException(nameof(charStringBytes));
            }

            if (subroutinesSelector == null)
            {
                throw new ArgumentNullException(nameof(subroutinesSelector));
            }

            var charStrings = new Dictionary<string, Type2CharStrings.CommandSequence>();
            for (var i = 0; i < charStringBytes.Count; i++)
            {
                var charString = charStringBytes[i];
                var name = charset.GetNameByGlyphId(i);
                var (globalSubroutines, localSubroutines) = subroutinesSelector.GetSubroutines(i);
                var sequence = ParseSingle(charString.ToList(), localSubroutines, globalSubroutines);
                charStrings[name] = sequence;
            }

            return new Type2CharStrings(charStrings);
        }

        private static Type2CharStrings.CommandSequence ParseSingle(List<byte> bytes,
            CompactFontFormatIndex localSubroutines,
            CompactFontFormatIndex globalSubroutines)
        {
            var values = new List<float>();
            var commandIdentifiers = new List<Type2CharStrings.CommandSequence.CommandIdentifier>();

            for (var i = 0; i < bytes.Count; i++)
            {
                var b = bytes[i];
                if (b <= 31 && b != 28)
                {
                    var command = GetCommand(b, bytes,
                        values,
                        commandIdentifiers,
                        localSubroutines,
                        globalSubroutines,
                        ref i);

                    if (command != null)
                    {
                        commandIdentifiers.Add(command.Value);
                    }
                }
                else
                {
                    var number = InterpretNumber(b, bytes, ref i);
                    values.Add(number);
                }
            }

            return new Type2CharStrings.CommandSequence(values, commandIdentifiers);
        }

        /// <summary>
        /// The Type 2 interpretation of a number with an initial byte value of 255 differs from how it is interpreted in the Type 1 format
        /// and 28 has a special meaning.
        /// </summary>
        private static float InterpretNumber(byte b, IReadOnlyList<byte> bytes, ref int i)
        {
            if (b == 28)
            {
                var num = bytes[++i] << 8 | bytes[++i];
                // Next 2 bytes are a 16-bit two's complement number.
                return (short)(num);
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
            var lead = (short)(bytes[++i] << 8) + bytes[++i];
            var fractionalPart = (bytes[++i] << 8) + bytes[++i];

            return lead + (fractionalPart / 65535.0f);
        }

        private static Type2CharStrings.CommandSequence.CommandIdentifier? GetCommand(byte b, List<byte> bytes,
            List<float> precedingValues,
            List<Type2CharStrings.CommandSequence.CommandIdentifier> precedingCommands,
            CompactFontFormatIndex localSubroutines,
            CompactFontFormatIndex globalSubroutines, ref int i)
        {
            const byte returnCommand = 11;

            if (b == 12)
            {
                var b2 = bytes[++i];
                if (TwoByteCommandStore.ContainsKey(b2))
                {
                    return new Type2CharStrings.CommandSequence.CommandIdentifier(precedingValues.Count, true, b2);
                }

                return new Type2CharStrings.CommandSequence.CommandIdentifier(precedingValues.Count, false, 255);
            }

            // Invoke a subroutine, substitute the subroutine bytes into this sequence.
            if (b == 10 || b == 29)
            {
                var isLocal = b == 10;
                int precedingNumber = (int)precedingValues[precedingValues.Count - 1];

                var bias = Type2BuildCharContext.CountToBias(isLocal ? localSubroutines.Count : globalSubroutines.Count);
                var index = precedingNumber + bias;
                var subroutineBytes = isLocal ? localSubroutines[index] : globalSubroutines[index];
                bytes.RemoveRange(i - 1, 2);
                bytes.InsertRange(i - 1, subroutineBytes);

                // Remove the subroutine index
                precedingValues.RemoveAt(precedingValues.Count - 1);
                i -= 2;
                return null;
            }

            if (b == 19 || b == 20)
            {
                // hintmask and cntrmask
                var minimumFullBytes = CalculatePrecedingHintBytes(precedingValues, precedingCommands);
                // Skip the following hintmask or cntrmask data bytes
                i += minimumFullBytes;
            }

            if (SingleByteCommandStore.ContainsKey(b))
            {
                // Ignore return
                if (b == returnCommand)
                {
                    return null;
                }

                return new Type2CharStrings.CommandSequence.CommandIdentifier(precedingValues.Count, false, b);
            }

            return new Type2CharStrings.CommandSequence.CommandIdentifier(precedingValues.Count, false, 255);
        }

        private static int CalculatePrecedingHintBytes(List<float> precedingValues,
    List<Type2CharStrings.CommandSequence.CommandIdentifier> precedingCommands)
        {
            int SafeStemCount(int counts)
            {
                // Where there an odd number of stem arguments take only as many as the even number requires.
                if (counts % 2 == 0)
                {
                    return counts / 2;
                }

                return (counts - 1) / 2;
            }

            /*
             * The hintmask operator is followed by one or more data bytes that specify the stem hints which are to be active for the
             * subsequent path construction. The number of data bytes must be exactly the number needed to represent the number of
             * stems in the original stem list (those stems specified by the hstem, vstem, hstemhm, or vstemhm commands), using one bit
             * in the data bytes for each stem in the original stem list.
             */
            var stemCount = 0;
            var precedingNumbers = 0;
            var hasEncounteredInitialHintMask = false;

            for (var i = -1; i < precedingValues.Count; i++)
            {
                if (i >= 0)
                {
                    precedingNumbers++;
                }

                for (var j = 0; j < precedingCommands.Count; j++)
                {
                    var identifier = precedingCommands[j];
                    if (identifier.CommandIndex != i + 1)
                    {
                        continue;
                    }

                    if (!identifier.IsMultiByteCommand
                        && (identifier.CommandId == HintmaskByte || identifier.CommandId == CntrmaskByte)
                        && !hasEncounteredInitialHintMask)
                    {
                        hasEncounteredInitialHintMask = true;
                        stemCount += SafeStemCount(precedingNumbers);
                    }
                    else if (!identifier.IsMultiByteCommand && !HintingCommandBytes.Contains(identifier.CommandId))
                    {
                        precedingNumbers = 0;
                    }
                    else if (identifier.IsMultiByteCommand && identifier.CommandId > 35)
                    {
                        precedingNumbers = 0;
                    }
                    else
                    {
                        stemCount += SafeStemCount(precedingNumbers);
                        precedingNumbers = 0;
                    }

                    if (hasEncounteredInitialHintMask)
                    {
                        break;
                    }
                }

                if (hasEncounteredInitialHintMask)
                {
                    break;
                }
            }

            var fullStemCount = stemCount;
            // The vstem command can be left out, e.g. for 12 20 hstemhm 4 6 hintmask, 4 and 6 act as the vertical hints
            if (precedingNumbers > 0 && !hasEncounteredInitialHintMask)
            {
                fullStemCount += SafeStemCount(precedingNumbers);
            }

            var minimumFullBytes = (int)Math.Ceiling(fullStemCount / 8d);

            return minimumFullBytes;
        }
    }
}
