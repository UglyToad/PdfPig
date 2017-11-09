using System;

namespace UglyToad.Pdf.IO
{
    public class EndstreamOutputStream : IOutputStream
    {
        private readonly IOutputStream output;
        //TODO: replace this class with a PullBackOutputStream class if there ever is one

        private bool hasCR = false;
        private bool hasLF = false;
        private int pos = 0;
        private bool mustFilter = true;

        public EndstreamOutputStream(IOutputStream output)
        {
            this.output = output;
        }

        /**
         * Write CR and/or LF that were kept, then writes len bytes from the 
         * specified byte array starting at offset off to this output stream,
         * except trailing CR, CR LF, or LF. No filtering will be done for the
         * entire stream if the beginning is assumed to be ASCII.
         * @param b byte array.
         * @param off offset.
         * @param len length of segment to write.
         * @throws IOException 
         */
        public void write(byte[] b)
        {
            throw new NotImplementedException();
        }

        public void write(byte[] b, int off, int len)
        {
            if (pos == 0 && len > 10)
            {
                // PDFBOX-2120 Don't filter if ASCII, i.e. keep a final CR LF or LF
                mustFilter = false;
                for (int i = 0; i < 10; ++i)
                {
                    // Heuristic approach, taken from PDFStreamParser, PDFBOX-1164
                    if ((b[i] < 0x09) || ((b[i] > 0x0a) && (b[i] < 0x20) && (b[i] != 0x0d)))
                    {
                        // control character or > 0x7f -> we have binary data
                        mustFilter = true;
                        break;
                    }
                }
            }
            if (mustFilter)
            {
                // first write what we kept last time
                if (hasCR)
                {
                    // previous buffer ended with CR
                    hasCR = false;
                    if (!hasLF && len == 1 && b[off] == '\n')
                    {
                        // actual buffer contains only LF so it will be the last one
                        // => we're done
                        // reset hasCR done too to avoid CR getting written in the flush
                        return;
                    }
                    output.write('\r');
                }
                if (hasLF)
                {
                    output.write('\n');
                    hasLF = false;
                }
                // don't write CR, LF, or CR LF if at the end of the buffer
                if (len > 0)
                {
                    if (b[off + len - 1] == '\r')
                    {
                        hasCR = true;
                        --len;
                    }
                    else if (b[off + len - 1] == '\n')
                    {
                        hasLF = true;
                        --len;
                        if (len > 0 && b[off + len - 1] == '\r')
                        {
                            hasCR = true;
                            --len;
                        }
                    }
                }
            }
            output.write(b, off, len);
            pos += len;
        }

        public void write(int b)
        {
            output.write(b);
        }

        /**
     * write out a single CR if one was kept. Don't write kept CR LF or LF, 
     * and then call the base method to flush.
     * 
     * @throws IOException 
     */
        public void flush()
        {
            // if there is only a CR and no LF, write it
            if (hasCR && !hasLF)
            {
                output.write('\r');
                ++pos;
            }
            hasCR = false;
            hasLF = false;
            output.flush();
        }
        public void Dispose()
        {
            output.Dispose();
        }
    }
}

