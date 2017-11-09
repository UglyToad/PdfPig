using System;
using System.Collections.Generic;
using System.Text;

namespace UglyToad.Pdf.Fonts.Cmap
{
    public class CMap
    {
        private int wmode = 0;
        private string cmapName = null;
        private string cmapVersion = null;
        private int cmapType = -1;

        private string registry = null;
        private string ordering = null;
        private int supplement = 0;

        private int minCodeLength = 4;
        private int maxCodeLength;

        // code lengths
        private readonly List<CodespaceRange> codespaceRanges = new List<CodespaceRange>();

        // Unicode mappings
        private readonly Dictionary<int, string> charToUnicode = new Dictionary<int, string>();

        // CID mappings
        private readonly Dictionary<int, int> codeToCid = new Dictionary<int, int>();
        private readonly List<CidRange> codeToCidRanges = new List<CidRange>();

        private static readonly string SPACE = " ";
        private int spaceMapping = -1;

        /**
         * Creates a new instance of CMap.
         */
        public CMap()
        {
        }

        /**
         * This will tell if this cmap has any CID mappings.
         * 
         * @return true If there are any CID mappings, false otherwise.
         */
        public bool hasCIDMappings()
        {
            return codeToCid.Count > 0 || codeToCidRanges.Count > 0;
        }

        /**
         * This will tell if this cmap has any Unicode mappings.
         *
         * @return true If there are any Unicode mappings, false otherwise.
         */
        public bool hasUnicodeMappings()
        {
            return charToUnicode.Count > 0;
        }

        /**
         * Returns the sequence of Unicode characters for the given character code.
         *
         * @param code character code
         * @return Unicode characters (may be more than one, e.g "fi" ligature)
         */
        public string toUnicode(int code)
        {
            charToUnicode.TryGetValue(code, out var result);

            return result;
        }

        /**
         * Reads a character code from a string in the content stream.
         * <p>See "CMap Mapping" and "Handling Undefined Characters" in PDF32000 for more details.
         *
         * @param in string stream
         * @return character code
         * @throws IOException if there was an error reading the stream or CMap
         */
        //public int readCode(InputStream input)
        //{
        //    byte[] bytes = new byte[maxCodeLength];
        //    input.read(bytes, 0, minCodeLength);
        //    for (int i = minCodeLength - 1; i < maxCodeLength; i++)
        //    {
        //        var byteCount = i + 1;
        //        foreach (var range in codespaceRanges)
        //        {
        //            if (range.isFullMatch(bytes, byteCount))
        //            {
        //                return toInt(bytes, byteCount);
        //            }
        //        }
        //        if (byteCount < maxCodeLength)
        //        {
        //            bytes[byteCount] = (byte)input.read();
        //        }
        //    }

        //    throw new InvalidOperationException("CMap is invalid");
        //}

        /**
         * Returns an int for the given byte array
         */
        static int toInt(byte[] data, int dataLen)
        {
            int code = 0;
            for (int i = 0; i < dataLen; ++i)
            {
                code <<= 8;
                code |= (data[i] & 0xFF);
            }
            return code;
        }

        /**
         * Returns the CID for the given character code.
         *
         * @param code character code
         * @return CID
         */
        public int toCID(int code)
        {
            if (codeToCid.TryGetValue(code, out var cid))
            {
                return cid;
            }

            foreach (CidRange range in codeToCidRanges)
            {
                int ch = range.Map((char)code);
                if (ch != -1)
                {
                    return ch;
                }
            }
            return 0;
        }

        /**
         * Convert the given part of a byte array to an int.
         * @param data the byte array
         * @param offset The offset into the byte array.
         * @param length The length of the data we are getting.
         * @return the resulting int
         */
        private int getCodeFromArray(byte[] data, int offset, int length)
        {
            int code = 0;
            for (int i = 0; i < length; i++)
            {
                code <<= 8;
                code |= (data[offset + i] + 256) % 256;
            }
            return code;
        }

        /**
         * This will add a character code to Unicode character sequence mapping.
         *
         * @param codes The character codes to map from.
         * @param unicode The Unicode characters to map to.
         */
        void addCharMapping(byte[] codes, string unicode)
        {
            int code = getCodeFromArray(codes, 0, codes.Length);
            charToUnicode[code] = unicode;

            // fixme: ugly little hack
            if (SPACE.Equals(unicode))
            {
                spaceMapping = code;
            }
        }

        /**
         * This will add a CID mapping.
         *
         * @param code character code
         * @param cid CID
         */
        void addCIDMapping(int code, int cid)
        {
            codeToCid[cid] = code;
        }

        /**
         * This will add a CID Range.
         *
         * @param from starting charactor of the CID range.
         * @param to ending character of the CID range.
         * @param cid the cid to be started with.
         *
         */
        void addCIDRange(char from, char to, int cid)
        {
            codeToCidRanges.Add(new CidRange(from, to, cid));
        }

        /**
         * This will add a codespace range.
         *
         * @param range A single codespace range.
         */
        void addCodespaceRange(CodespaceRange range)
        {
            codespaceRanges.Add(range);
            maxCodeLength = Math.Max(maxCodeLength, range.CodeLength);
            minCodeLength = Math.Min(minCodeLength, range.CodeLength);
        }

        /**
         * Implementation of the usecmap operator.  This will
         * copy all of the mappings from one cmap to another.
         * 
         * @param cmap The cmap to load mappings from.
         */
        private void useCmap(CMap cmap)
        {
            foreach (CodespaceRange codespaceRange in cmap.codespaceRanges)
            {
                addCodespaceRange(codespaceRange);
            }
            charToUnicode.PutAll(cmap.charToUnicode);
            codeToCid.PutAll(cmap.codeToCid);
            codeToCidRanges.AddRange(cmap.codeToCidRanges);
        }

        /**
         * Returns the WMode of a CMap.
         *
         * 0 represents a horizontal and 1 represents a vertical orientation.
         * 
         * @return the wmode
         */
        public int getWMode()
        {
            return wmode;
        }

        /**
         * Sets the WMode of a CMap.
         * 
         * @param newWMode the new WMode.
         */
        public void setWMode(int newWMode)
        {
            wmode = newWMode;
        }

        /**
         * Returns the name of the CMap.
         * 
         * @return the CMap name.
         */
        public string getName()
        {
            return cmapName;
        }

        /**
         * Sets the name of the CMap.
         * 
         * @param name the CMap name.
         */
        public void setName(string name)
        {
            cmapName = name;
        }

        /**
         * Returns the version of the CMap.
         * 
         * @return the CMap version.
         */
        public string getVersion()
        {
            return cmapVersion;
        }

        /**
         * Sets the version of the CMap.
         * 
         * @param version the CMap version.
         */
        public void setVersion(string version)
        {
            cmapVersion = version;
        }

        /**
         * Returns the type of the CMap.
         * 
         * @return the CMap type.
         */
        public int getType()
        {
            return cmapType;
        }

        /**
         * Sets the type of the CMap.
         * 
         * @param type the CMap type.
         */
        public void setType(int type)
        {
            cmapType = type;
        }

        /**
         * Returns the registry of the CIDSystemInfo.
         * 
         * @return the registry.
         */
        public string getRegistry()
        {
            return registry;
        }

        /**
         * Sets the registry of the CIDSystemInfo.
         * 
         * @param newRegistry the registry.
         */
        public void setRegistry(string newRegistry)
        {
            registry = newRegistry;
        }

        /**
         * Returns the ordering of the CIDSystemInfo.
         * 
         * @return the ordering.
         */
        public string getOrdering()
        {
            return ordering;
        }

        /**
         * Sets the ordering of the CIDSystemInfo.
         * 
         * @param newOrdering the ordering.
         */
        public void setOrdering(string newOrdering)
        {
            ordering = newOrdering;
        }

        /**
         * Returns the supplement of the CIDSystemInfo.
         * 
         * @return the supplement.
         */
        public int getSupplement()
        {
            return supplement;
        }

        /**
         * Sets the supplement of the CIDSystemInfo.
         * 
         * @param newSupplement the supplement.
         */
        public void setSupplement(int newSupplement)
        {
            supplement = newSupplement;
        }

        /** 
         * Returns the mapping for the space character.
         * 
         * @return the mapped code for the space character
         */
        public int getSpaceMapping()
        {
            return spaceMapping;
        }


        public override string ToString()
        {
            return cmapName;
        }
    }

}
