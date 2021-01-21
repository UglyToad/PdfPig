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
    internal static class ContentStreamExtensions
    {
        /// <summary>
        /// EXPERIMENTAL
        /// Strips all non-text content from the content stream.
        /// </summary>
        /// <param name="token">Input stream</param>
        /// <param name="provider">optional providers</param>
        /// <returns></returns>
        internal static StreamToken StripNonText(this StreamToken token, IFilterProvider provider=null)
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

        /// <summary>
        /// This iterates over the uncompressed input stream and returns the text only operations.
        /// This is a very quick process that ends up reducing page processing time significantly if all
        /// you want to do is extract text and there can be tens of thousands of painting operations that
        /// don't affect text at all.
        /// </summary>
        /// <param name="data">Uncompressed stream content</param>
        /// <returns>Uncompressed text only stream content</returns>
        private static IReadOnlyList<byte> TrimNonTextBytes(IReadOnlyList<byte> data)
        {
            var input = data.ToArray();
            // Op - Previous tokens needed
            // q 0
            // Q 0
            // gs 1
            // Do 1
            // cm 6
            // BT -> ET
            var depth = 0;
            var output = new byte[input.Length];
            var curPos = 0;
            for (var i = 0; i < input.Length; i++)
            {
                var cc = input[i];
                if (cc == '(' && !IsEscaped(i))
                {
                    depth++;
                } else if (cc == ')' && !IsEscaped(i))
                {
                    depth--;
                } else if (depth == 0 )
                {

                    if ((cc == 'q' || cc == 'Q') && IsEndOfToken(i))
                    {
                        output[curPos++] = cc;
                        output[curPos++] = (byte)'\n';
                    }
                    else if (i > 0)
                    {
                        var pc = input[i-1];
                        if (((pc == 'D' && cc == 'o') || (pc == 'g' && cc == 's')) 
                             && IsEndOfToken(i)
                            )
                        {
                            AddTokens(i, 2);

                        } else if ((pc == 'c' && cc == 'm') && IsEndOfToken(i))
                        {
                            AddTokens(i, 7);
                        } else if ((pc == 'B' && cc == 'T') && IsEndOfToken(i))
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
            return new ArraySegment<byte>(output, 0, curPos);

            bool IsEndOfToken(int pos)
            {
                var next = pos + 1;
                return next >= input.Length || IsWhiteSpace(next);
            }

            int CopyTillEt(int init)
            {
                var etDepth = 0;
                var end = -1;
                for (var i = init; i < input.Length; i++)
                {
                    var cc = input[i];
                    if (cc == '(' && !IsEscaped(i))
                    {
                        etDepth++;
                    } else if (cc == ')' && !IsEscaped(i))
                    {
                        etDepth--;
                    } else if (etDepth == 0)
                    {
                        if (input[i - 1] == 'E' && cc == 'T')
                        {
                            CopyData(init, i);
                            end = i;
                            break;
                        }
                    }
                }

                return end;
            }

            void CopyData(int start, int end)
            {
                var len = end - start + 1;
                Array.Copy(input, start, output, curPos, len);
                curPos += len;
                output[curPos++] = (byte) '\n';
            }

            void AddTokens(int pos, int count)
            {
                var lastWhite = 0;
                var whiteCount = 0;
                for (var i = pos; i>=0; i--)
                {
                    if (i == 0)
                    {
                        CopyData(i, pos);
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
                        CopyData(i+1, pos);
                        break;
                    }
                }
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
