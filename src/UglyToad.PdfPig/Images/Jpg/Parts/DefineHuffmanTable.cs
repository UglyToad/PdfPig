namespace UglyToad.PdfPig.Images.Jpg.Parts
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using static Helpers.Context;
   

    /// <summary>
    /// Define Huffman Table(s)
    /// </summary>
    internal static class DefineHuffmanTable
    {

         
        internal static void ParseDht(JpgBinaryStreamReader reader, ContextForDefineHuffmanTables context)
        {
            var vlctab = context.vlctab;

            
            var frameHeader = reader.DecodeFrameHeader();  
            var frameHeaderLength = frameHeader.length;
           
            byte[] counts = new byte[16];
            while (frameHeader.remaining >= 17)
            {
                // Table Selector (1 byte)
                // Tc: Table class (4 bits) Possible Values: 0 or 1
                // 0 = DC table or lossless table, 1 = AC table.
                //
                // Th: Huffman table destination identifier (4 bits) Possible Values: 0 or 1 (DCT Baseline) otherwise 0-3
                // Specifies one of four possible destinations at the decoder into which
                // the Huffman table shall be installed.

                var tableSelector = frameHeader.ReadByte();
                
                if ((tableSelector & 0xEC) != 0) { throw new Exception(); } // Syntax Error
                if ((tableSelector & 0x02) != 0) { throw new Exception(); } // Unsupported
                
                tableSelector = (byte)((tableSelector | (tableSelector >> 3)) & 3);  // combined DC/AC + tableid value

                // Li: Number of Huffman codes of length i (16 x 8 bytes) Possible Values: 0-255
                // Specifies the number of Huffman codes for each of the 16 possible lengths 
                // Li’s are the elements of the list BITS.
                for (int codelen = 0; codelen < 16; codelen++)
                {
                    counts[codelen] = frameHeader.ReadByte();                     
                }
                 
                var vlc = vlctab[tableSelector];

                int remain = 65536;
                int spread = 65536;
                int vlcc = 0;


                // Vi,j : Vi,j: Value associated with each Huffman code
                // Specifies, for each i, the value associated with each Huffman
                // code of length i. The meaning of each value is determined by the Huffman coding model. The Vi, j’s are the
                // elements of the list HUFFVAL.
                //  
                // Specifies, for each i, the value associated with each Huffman
                // code of length i. The meaning of each value is determined by the Huffman coding model. The Vi, j’s are the
                //elements of the list HUFFVAL.
                for (int codelen = 0; codelen < 16; codelen++)
                {
                    spread >>= 1;
                    var currcnt = counts[codelen];
                    if (currcnt == 0)
                    {
                        continue;   //skip
                    }
                    if (frameHeader.length < currcnt) { new Exception(); } //Syntax Error
                    remain -= currcnt << (16 - (codelen + 1));
                    
                    if (remain < 0) { new Exception(); } // Syntax Error
                    
                    for (var i = 0; i < currcnt; ++i)
                    {
                        byte code = frameHeader.ReadByte();
                        for (int j = spread; j != 0; --j)
                        {
                            if (vlcc < 65536)
                            {                                 
                                vlc[vlcc].bits = (byte)(codelen+1);
                                vlc[vlcc].code = code;
                                vlcc++;
                            }
                        }
                    }                    
                }
                
                while (remain-- != 0)
                {
                    if (vlcc < 65536)
                    {
                        vlc[vlcc].bits = 0;
                        vlcc++;
                    }
                }
            }
            context.vlctab = vlctab;             
        }
    }
}
