using System;
using System.Collections.Generic;
using System.Text;

namespace UglyToad.Pdf.Cos
{
    using System.IO;
    using Filters;
    using IO;

    /*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 
    /**
     * This class represents a stream object in a PDF document.
     *
     * @author Ben Litchfield
     */
    public class COSStream : CosDictionary, IDisposable
    {
        private RandomAccess randomAccess;      // backing store, in-memory or on-disk
        private bool isWriting;              // true if there's an open OutputStream


        /**
         * Creates a new stream with an empty dictionary.
         * <p>
         * Try to avoid using this constructor because it creates a new scratch file in memory. Instead,
         * use {@link COSDocument#createCOSStream() document.getDocument().createCOSStream()} which will
         * use the existing scratch file (in memory or in temp file) of the document.
         * </p>
         */

        /**
         * Creates a new stream with an empty dictionary. Data is stored in the given scratch file.
         *
         * @param scratchFile Scratch file for writing stream data.
         */
        public COSStream()
        {
            setInt(CosName.LENGTH, 0);
        }

        /**
         * Throws if the random access backing store has been closed. Helpful for catching cases where
         * a user tries to use a COSStream which has outlived its COSDocument.
         */
        private void checkClosed()
        {
            if ((randomAccess != null) && randomAccess.IsClosed())
            {
                throw new IOException("COSStream has been closed and cannot be read. " +
                                      "Perhaps its enclosing PDDocument has been closed?");
            }
        }

        /**
         * Ensures {@link #randomAccess} is not <code>null</code> by creating a
         * buffer from {@link #scratchFile} if needed.
         * 
         * @param forInputStream  if <code>true</code> and {@link #randomAccess} is <code>null</code>
         *                        a debug message is logged - input stream should be retrieved after
         *                        data being written to stream
         * @throws IOException
         */
        private void ensureRandomAccessExists(bool forInputStream)
        {
            if (randomAccess == null)
            {
                randomAccess = new RandomAccessBuffer();
            }
        }

        /**
         * Returns a new InputStream which reads the encoded PDF stream data. Experts only!
         * 
         * @return InputStream containing raw, encoded PDF stream data.
         * @throws IOException If the stream could not be read.
         */
        public RandomAccessInputStream createRawInputStream()
        {
            checkClosed();
            if (isWriting)
            {
                throw new InvalidOperationException("Cannot read while there is an open stream writer");
            }
            ensureRandomAccessExists(true);
            return new RandomAccessInputStream(randomAccess);
        }

        /**
         * Returns a new InputStream which reads the decoded stream data.
         * 
         * @return InputStream containing decoded stream data.
         * @throws IOException If the stream could not be read.
         */
        public COSInputStream createInputStream()
        {
            checkClosed();
            if (isWriting)
            {
                throw new InvalidOperationException("Cannot read while there is an open stream writer");
            }
            ensureRandomAccessExists(true);
            var input = new RandomAccessInputStream(randomAccess);
            return COSInputStream.create(getFilterList(), this, input);
        }

        /**
         * Returns a new OutputStream for writing stream data, using the current filters.
         *
         * @return OutputStream for un-encoded stream data.
         * @throws IOException If the output stream could not be created.
         */
        public IOutputStream createOutputStream()
        {
            return createOutputStream(null);
        }

        /**
         * Returns a new OutputStream for writing stream data, using and the given filters.
         * 
         * @param filters COSArray or COSName of filters to be used.
         * @return OutputStream for un-encoded stream data.
         * @throws IOException If the output stream could not be created.
         */
        public IOutputStream createOutputStream(CosBase filters)
        {
            checkClosed();
            if (isWriting)
            {
                throw new InvalidOperationException("Cannot have more than one open stream writer.");
            }
            // apply filters, if any
            if (filters != null)
            {
                setItem(CosName.FILTER, filters);
            }
            randomAccess = new RandomAccessBuffer(); // discards old data - TODO: close existing buffer?
            IOutputStream randomOut = new RandomAccessOutputStream(randomAccess);
            IOutputStream cosOut = new COSOutputStream(getFilterList(), this, randomOut);
            isWriting = true;
            //        return new FilterOutputStream(cosOut)
            //{

            //            public void write(byte[] b, int off, int len)
            //            {
            //        this.out.write(b, off, len);
            //    }


            //            public void close()
            //            {
            //        super.close();
            //        setInt(COSName.LENGTH, (int)randomAccess.length());
            //        isWriting = false;
            //    }
            //};
            throw new NotImplementedException();
        }


        /**
         * Returns a new OutputStream for writing encoded PDF data. Experts only!
         * 
         * @return OutputStream for raw PDF stream data.
         * @throws IOException If the output stream could not be created.
         */
        public IOutputStream createRawOutputStream()
        {
            checkClosed();
            if (isWriting)
            {
                throw new InvalidOperationException("Cannot have more than one open stream writer.");
            }
            randomAccess = new RandomAccessBuffer(); // discards old data - TODO: close existing buffer?
            IOutputStream output = new RandomAccessOutputStream(randomAccess);
            isWriting = true;
            return output;
            //        return new FilterOutputStream(out)
            //{

            //            public void write(byte[] b, int off, int len)
            //            {
            //        this.out.write(b, off, len);
            //    }


            //            public void close()
            //            {
            //        super.close();
            //        setInt(COSName.LENGTH, (int)randomAccess.length());
            //        isWriting = false;
            //    }
            //};
            throw new NotImplementedException();
        }

        /**
         * Returns the list of filters.
         */
        private List<Filter> getFilterList()
        {
            List<Filter> filterList = new List<Filter>();
            throw new NotImplementedException();
            CosBase filters = getFilters();
            if (filters is CosName name)
            {
                //filterList.Add(FilterFactory.INSTANCE.getFilter(name));
            }
            else if (filters is COSArray filterArray)
            {
                for (int i = 0; i < filterArray.size(); i++)
                {
                    CosName filterName = (CosName)filterArray.get(i);
                    //filterList.Add(FilterFactory.INSTANCE.getFilter(filterName));
                }
            }
            return filterList;
        }

        /**
         * Returns the length of the encoded stream.
         *
         * @return length in bytes
         */
        public long getLength()
        {
            if (isWriting)
            {
                throw new InvalidOperationException("There is an open OutputStream associated with " +
                                                "this COSStream. It must be closed before querying" +
                                                "length of this COSStream.");
            }
            return getInt(CosName.LENGTH, 0);
        }

        /**
         * This will return the filters to apply to the byte stream.
         * The method will return
         * - null if no filters are to be applied
         * - a COSName if one filter is to be applied
         * - a COSArray containing COSNames if multiple filters are to be applied
         *
         * @return the COSBase object representing the filters
         */
        public CosBase getFilters()
        {
            return getDictionaryObject(CosName.FILTER);
        }

        /**
         * Returns the contents of the stream as a PDF "text string".
         */
        public String toTextString()
        {
            return string.Empty;
            //ByteArrayOutputStream out = new ByteArrayOutputStream();
            //InputStream input = null;
            //try
            //{
            //    input = createInputStream();
            //    IOUtils.copy(input, out);
            //}
            //catch (IOException e)
            //{
            //    return "";
            //}
            //finally
            //{
            //    IOUtils.closeQuietly(input);
            //}
            //CosString str = new CosString(out.toByteArray());
        }


        public override object Accept(ICosVisitor visitor)
        {
            return visitor.VisitFromStream(this);
        }

        public void Dispose()
        {
            randomAccess?.Dispose();
        }
    }

}


namespace UglyToad.Pdf.Cos
{
    using Filters;
    using IO;

    public class COSInputStream : IInputStream
    {
    /**
     * Creates a new COSInputStream from an encoded input stream.
     *
     * @param filters Filters to be applied.
     * @param parameters Filter parameters.
     * @param in Encoded input stream.
     * @param scratchFile Scratch file to use, or null.
     * @return Decoded stream.
     * @throws IOException If the stream could not be read.
     */
    internal static COSInputStream create(List<Filter> filters, CosDictionary parameters, IInputStream input)
    {
        List<DecodeResult> results = new List<DecodeResult>();
        IInputStream inputTemp = input;
        if (filters.Count == 0)
        {
            inputTemp = input;
        }
        else
        {
            // apply filters
            for (int i = 0; i<filters.Count; i++)
            {
                    // in-memory
                    var output = new BinaryOutputStream();
DecodeResult result = filters[i].decode(input, output, parameters, i);
results.Add(result);
                    input = new BinaryInputStream(output.ToArray());
                
            }
        }
        return new COSInputStream(input, results);
    }

        private readonly IInputStream input;
        private readonly List<DecodeResult> decodeResults;

/**
 * Constructor.
 * 
 * @param input decoded stream
 * @param decodeResults results of decoding
 */
private COSInputStream(IInputStream input, List<DecodeResult> decodeResults)
{
    this.input = input;
    this.decodeResults = decodeResults;
}

/**
 * Returns the result of the last filter, for use by repair mechanisms.
 */
public DecodeResult getDecodeResult()
{
    if (decodeResults.Count == 0)
    {
        return DecodeResult.DEFAULT;
    }
    else
    {
        return decodeResults[decodeResults.Count - 1];
    }
}

        public void Dispose()
        {
            input.Dispose();
        }

        public int read()
        {
            return input.read();
        }

        public int read(byte[] b)
        {
            return input.read(b);
        }

        public int read(byte[] b, int off, int len)
        {
            return input.read(b, off, len);
        }

        public long available()
        {
            return 0;
        }
    }


    public class COSOutputStream : IOutputStream
    {
        private readonly List<Filter> filters;
        private readonly CosDictionary parameters;

        private readonly IOutputStream output;

        // todo: this is an in-memory buffer, should use scratch file (if any) instead
        private BinaryOutputStream buffer = new BinaryOutputStream();

        /**
         * Creates a new COSOutputStream writes to an encoded COS stream.
         * 
         * @param filters Filters to apply.
         * @param parameters Filter parameters.
         * @param output Encoded stream.
         * @param scratchFile Scratch file to use, or null.
         */
        internal COSOutputStream(List<Filter> filters, CosDictionary parameters, IOutputStream output)
        {
            this.filters = filters;
            this.parameters = parameters;
            this.output = output;
        }

        public void write(byte[] b)
        {
            buffer.write(b);
        }

        public void write(byte[] b, int off, int len)
        {
            buffer.write(b, off, len);
        }

        public void write(int b)
        {
            buffer.write(b);
        }

        public void flush() { }

        public void Dispose()
        {
            // apply filters in reverse order
            for (int i = filters.Count - 1; i >= 0; i--)
            {
                // todo: this is an in-memory buffer, should use scratch file (if any) instead
                var input = new BinaryInputStream(buffer.ToArray());
                buffer = new BinaryOutputStream();
                filters[i].encode(input, buffer, parameters, i);
            }
            // flush the entire stream
            output.write(buffer.ToArray());
        }
    }


}