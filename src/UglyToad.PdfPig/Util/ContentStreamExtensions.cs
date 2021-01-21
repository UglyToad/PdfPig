namespace UglyToad.PdfPig.Util
{
    using Content;
    using Core;
    using Filters;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Tokens;

    /// <summary>
    /// Experimental
    /// </summary>
    public static class ContentStreamExtensions
    {
        /// <summary>
        /// EXPERIMENTAL
        /// Strips all non-text content from the content stream.
        /// </summary>
        /// <param name="token">Input stream</param>
        /// <param name="provider">optional providers</param>
        /// <returns></returns>
        public static StreamToken StripNonText(this StreamToken token, IFilterProvider provider=null)
        {
            if (provider == null)
            {
                provider = DefaultFilterProvider.Instance;
            }

            // create copy without filter
            var copy = token.StreamDictionary.Data
                .Where(x => x.Key != NameToken.Filter)
                .ToDictionary(k => NameToken.Create(k.Key), v => v.Value);
            var dict = new DictionaryToken(copy);
            return new StreamToken(dict, TrimNonTextBytes(token.Decode(provider)));
        }

        private static IReadOnlyList<byte> TrimNonTextBytes(IReadOnlyList<byte> input)
        {
            // Op - Previous tokens needed
            // q 0
            // Q 0
            // gs 1
            // Do 1
            // cm 6
            // BT -> ET
            var depth = 0;
            var output = new List<byte>();
            for (var i = 0; i < input.Count; i++)
            {
                if (input[i] == '(' && !IsEscaped(i))
                {
                    depth++;
                } else if (input[i] == ')' && !IsEscaped(i))
                {
                    depth--;
                } else if (depth == 0 && IsEndOfToken(i))
                {
                    if (input[i] == 'q' || input[i] == 'Q')
                    {
                        output.Add(input[i]);
                        output.Add((byte)'\n');
                    }
                    else if (i > 0)
                    {
                        if (
                            (input[i-1] == 'D' && input[i] == 'o')
                         || (input[i-1] == 'g' && input[i] == 's')
                            )
                        {
                            AddTokens(i, 2);

                        } else if (input[i-1] == 'c' && input[i] == 'm')
                        {
                            AddTokens(i, 7);
                        } else if (input[i - 1] == 'B' && input[i] == 'T')
                        {
                            i = CopyTillEt(i-1); // include BT in copy
                            if (i == -1)
                            {
                                return input;
                            }
                        }
                    }
                }
            }
            return output;

            bool IsEndOfToken(int pos)
            {
                var next = pos + 1;
                return next >= input.Count || IsWhiteSpace(next);
            }

            int CopyTillEt(int init)
            {
                var etDepth = 0;
                var end = -1;
                for (var i = init; i < input.Count; i++)
                {
                    if (input[i] == '(' && !IsEscaped(i))
                    {
                        etDepth++;
                    } else if (input[i] == ')' && !IsEscaped(i))
                    {
                        etDepth--;
                    } else if (etDepth == 0)
                    {
                        if (input[i - 1] == 'E' && input[i] == 'T')
                        {
                            for (var p = init; p <= i; p++) { output.Add(input[p]); }
                            output.Add((byte)'\n');
                            end = i;
                            break;
                        }
                    }
                }

                return end;
            }

            void AddTokens(int pos, int count)
            {
                var lastWhite = 0;
                var whiteCount = 0;
                for (var i = pos; i>=0; i--)
                {
                    if (i == 0)
                    {
                        for (var p = i; p <= pos; p++) { output.Add(input[p]); }
                        break;
                    }

                    if (IsWhiteSpace(i))
                    {
                        if (lastWhite != i + 1)
                        {
                            whiteCount++;
                        }

                        lastWhite = i;
                    }

                    if (whiteCount == count)
                    {
                        for (var p = i+1; p <= pos; p++) { output.Add(input[p]); }
                        break;
                    }
                }
                output.Add((byte)'\n');
            }
            bool IsWhiteSpace(int pos)
            {
                var ch = input[pos];
                return ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n';
            }
            bool IsEscaped(int pos)
            {
                var escapes = 0;
                for (var i = pos; i + 1 > 0; i--)
                {
                    if (input[i] == (byte) '\\')
                    {
                        escapes++;
                    } else {
                        break;
                    }
                }

                if (escapes == 0 || escapes % 2 == 2)
                {
                    return false;
                }
                return true;
            }
        }
    }
}
