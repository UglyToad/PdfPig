namespace UglyToad.PdfPig.Fonts.Type1.CharStrings
{
    using System.Collections.Generic;
    using Commands.Hint;

    internal static class Type1CharStringCommandFactory
    {
        public static object GetCommand(List<int> arguments, byte v, IReadOnlyList<byte> bytes, ref int i)
        {
            switch (v)
            {
                case 1:
                {
                    return new HStemCommand(arguments[0], arguments[1]);
                }
                case 12:
                {
                    var next = bytes[++i];
                    switch (next)
                    {
                            
                    }

                    break;
                }
            }
            return null;
        }
    }
}
