using System;
using System.Collections.Generic;

namespace UglyToad.Pdf.IO
{
    using System.IO;

    public class RandomAccessBuffer : RandomAccess
    {
        // default chunk size is 1kb
        private static readonly int DefaultChunkSize = 1024;

        // use the default chunk size
        private int chunkSize = DefaultChunkSize;

        // list containing all chunks
        private List<byte[]> bufferList;

        // current chunk
        private byte[] currentBuffer;

        // current pointer to the whole buffer
        private long pointer;

        // current pointer for the current chunk
        private int currentBufferPointer;

        // size of the whole buffer
        private long size;

        // current chunk list index
        private int bufferListIndex;

        // maximum chunk list index
        private int bufferListMaxIndex;

        /**
         * Default constructor.
         */
        public RandomAccessBuffer() : this(DefaultChunkSize)
        {
        }

        /**
         * Default constructor.
         */
        private RandomAccessBuffer(int definedChunkSize)
        {
            // starting with one chunk
            bufferList = new List<byte[]>();
            chunkSize = definedChunkSize;
            currentBuffer = new byte[chunkSize];
            bufferList.Add(currentBuffer);
            pointer = 0;
            currentBufferPointer = 0;
            size = 0;
            bufferListIndex = 0;
            bufferListMaxIndex = 0;
        }

        /**
         * Create a random access buffer using the given byte array.
         * 
         * @param input the byte array to be read
         */
        public RandomAccessBuffer(byte[] input)
        {
            // this is a special case. The given byte array is used as the one
            // and only chunk.
            bufferList = new List<byte[]>(1);
            chunkSize = input.Length;
            currentBuffer = input;
            bufferList.Add(currentBuffer);
            pointer = 0;
            currentBufferPointer = 0;
            size = chunkSize;
            bufferListIndex = 0;
            bufferListMaxIndex = 0;
        }

        /**
         * Create a random access buffer of the given input stream by copying the data.
         * 
         * @param input the input stream to be read
         * @throws IOException if something went wrong while copying the data
         */
        public RandomAccessBuffer(BinaryReader input)
        {
            //this();
            byte[]
                byteBuffer = new byte[8192];
            int bytesRead = 0;
            while ((bytesRead = input.Read(byteBuffer, 0, 8192)) > -1)
            {
                write(byteBuffer, 0, bytesRead);
            }
            Seek(0);
        }


        public RandomAccessBuffer Clone()
        {
            RandomAccessBuffer copy = new RandomAccessBuffer(chunkSize)
                {
                    bufferList = new List<byte[]>(bufferList.Count)
                };

            foreach (byte[] buffer in bufferList)
            {
                byte[] newBuffer = new byte[buffer.Length];
                Array.Copy(buffer, 0, newBuffer, 0, buffer.Length);

                copy.bufferList.Add(newBuffer);
            }
            if (currentBuffer != null)
            {
                copy.currentBuffer = copy.bufferList[copy.bufferList.Count - 1];
            }
            else
            {
                copy.currentBuffer = null;
            }
            copy.pointer = pointer;
            copy.currentBufferPointer = currentBufferPointer;
            copy.size = size;
            copy.bufferListIndex = bufferListIndex;
            copy.bufferListMaxIndex = bufferListMaxIndex;

            return copy;
        }

/**
 * {@inheritDoc}
 */

        public void Dispose()
        {
            currentBuffer = null;
            bufferList.Clear();
            pointer = 0;
            currentBufferPointer = 0;
            size = 0;
            bufferListIndex = 0;
        }

/**
 * {@inheritDoc}
 */

        public void clear()
        {
            bufferList.Clear();
            currentBuffer = new byte[chunkSize];
            bufferList.Add(currentBuffer);
            pointer = 0;
            currentBufferPointer = 0;
            size = 0;
            bufferListIndex = 0;
            bufferListMaxIndex = 0;
        }

/**
 * {@inheritDoc}
 */

        public void Seek(long position)
        {
            CheckClosed();
            if (position < 0)
            {
                throw new InvalidOperationException($"Invalid position {position}");
            }
            pointer = position;
            if (pointer < size)
            {
                // calculate the chunk list index
                bufferListIndex = (int) (pointer / chunkSize);
                currentBufferPointer = (int) (pointer % chunkSize);
                currentBuffer = bufferList[bufferListIndex];
            }
            else
            {
                // it is allowed to jump beyond the end of the file
                // jump to the end of the buffer
                bufferListIndex = bufferListMaxIndex;
                currentBuffer = bufferList[bufferListIndex];
                currentBufferPointer = (int) (size % chunkSize);
            }
        }

/**
 * {@inheritDoc}
 */

        public long GetPosition()
        {
            CheckClosed();
            return pointer;
        }

/**
 * {@inheritDoc}
 */

        public int Read()
        {
            CheckClosed();
            if (pointer >= this.size)
            {
                return -1;
            }
            if (currentBufferPointer >= chunkSize)
            {
                if (bufferListIndex >= bufferListMaxIndex)
                {
                    return -1;
                }
                else
                {
                    currentBuffer = bufferList[++bufferListIndex];
                    currentBufferPointer = 0;
                }
            }
            pointer++;
            return currentBuffer[currentBufferPointer++] & 0xff;
        }

/**
 * {@inheritDoc}
 */

        public int Read(byte[] b, int offset, int length)
        {
            CheckClosed();
            if (pointer >= size)
            {
                return 0;
            }
            int bytesRead = ReadRemainingBytes(b, offset, length);
            while (bytesRead < length && Available() > 0)
            {
                bytesRead += ReadRemainingBytes(b, offset + bytesRead, length - bytesRead);
                if (currentBufferPointer == chunkSize)
                {
                    NextBuffer();
                }
            }
            return bytesRead;
        }

        private int ReadRemainingBytes(byte[] b, int offset, int length)
        {
            if (pointer >= size)
            {
                return 0;
            }
            int maxLength = (int) Math.Min(length, size - pointer);
            int remainingBytes = chunkSize - currentBufferPointer;
            // no more bytes left
            if (remainingBytes == 0)
            {
                return 0;
            }
            if (maxLength >= remainingBytes)
            {
                // copy the remaining bytes from the current buffer
                Array.Copy(currentBuffer, currentBufferPointer, b, offset, remainingBytes);
                // end of file reached
                currentBufferPointer += remainingBytes;
                pointer += remainingBytes;
                return remainingBytes;
            }
            else
            {
                // copy the remaining bytes from the whole buffer
                Array.Copy(currentBuffer, currentBufferPointer, b, offset, maxLength);
                // end of file reached
                currentBufferPointer += maxLength;
                pointer += maxLength;
                return maxLength;
            }
        }

/**
 * {@inheritDoc}
 */

        public long Length()
        {
            CheckClosed();
            return size;
        }

/**
 * {@inheritDoc}
 */

        public void write(int b)
        {
            CheckClosed();
            // end of buffer reached?
            if (currentBufferPointer >= chunkSize)
            {
                if (pointer + chunkSize >= int.MaxValue)
                {
                    throw new OutOfMemoryException("RandomAccessBuffer overflow");
                }
                ExpandBuffer();
            }
            currentBuffer[currentBufferPointer++] = (byte) b;
            pointer++;
            if (pointer > this.size)
            {
                this.size = pointer;
            }
            // end of buffer reached now?
            if (currentBufferPointer >= chunkSize)
            {
                if (pointer + chunkSize >= int.MaxValue)
                {
                    throw new OutOfMemoryException("RandomAccessBuffer overflow");
                }
                ExpandBuffer();
            }
        }


/**
 * {@inheritDoc}
 */

        public void write(byte[] b)
        {
            write(b, 0, b.Length);
        }

/**
 * {@inheritDoc}
 */

        public void write(byte[] b, int offset, int length)
        {
            CheckClosed();
            long newSize = pointer + length;
            int remainingBytes = chunkSize - currentBufferPointer;
            if (length >= remainingBytes)
            {
                if (newSize > int.MaxValue)
                {
                    throw new OutOfMemoryException("RandomAccessBuffer overflow");
                }
                // copy the first bytes to the current buffer
                Array.Copy(b, offset, currentBuffer, currentBufferPointer, remainingBytes);
                int newOffset = offset + remainingBytes;
                long remainingBytes2Write = length - remainingBytes;
                // determine how many buffers are needed for the remaining bytes
                int numberOfNewArrays = (int) remainingBytes2Write / chunkSize;
                for (int i = 0; i < numberOfNewArrays; i++)
                {
                    ExpandBuffer();
                    Array.Copy(b, newOffset, currentBuffer, currentBufferPointer, chunkSize);
                    newOffset += chunkSize;
                }
                // are there still some bytes to be written?
                remainingBytes2Write -= numberOfNewArrays * (long) chunkSize;
                if (remainingBytes2Write >= 0)
                {
                    ExpandBuffer();
                    if (remainingBytes2Write > 0)
                    {
                        Array.Copy(b, newOffset, currentBuffer, currentBufferPointer, (int) remainingBytes2Write);
                    }
                    currentBufferPointer = (int) remainingBytes2Write;
                }
            }
            else
            {
                Array.Copy(b, offset, currentBuffer, currentBufferPointer, length);
                currentBufferPointer += length;
            }
            pointer += length;
            if (pointer > this.size)
            {
                this.size = pointer;
            }
        }

/**
 * create a new buffer chunk and adjust all pointers and indices.
 */
        private void ExpandBuffer()
        {
            if (bufferListMaxIndex > bufferListIndex)
            {
                // there is already an existing chunk
                NextBuffer();
            }
            else
            {
                // create a new chunk and add it to the buffer
                currentBuffer = new byte[chunkSize];
                bufferList.Add(currentBuffer);
                currentBufferPointer = 0;
                bufferListMaxIndex++;
                bufferListIndex++;
            }
        }

/**
 * switch to the next buffer chunk and reset the buffer pointer.
 */
        private void NextBuffer()
        {
            if (bufferListIndex == bufferListMaxIndex)
            {
                throw new InvalidOperationException("No more chunks available, end of buffer reached");
            }
            currentBufferPointer = 0;
            currentBuffer = bufferList[++bufferListIndex];
        }

        /**
         * Ensure that the RandomAccessBuffer is not closed
         * @throws IOException
         */
        private void CheckClosed()
        {
            if (currentBuffer == null)
            {
                // consider that the rab is closed if there is no current buffer
                throw new ObjectDisposedException("RandomAccessBuffer already closed");
            }

        }

/**
 * {@inheritDoc}
 */

        public bool IsClosed()
        {
            return currentBuffer == null;
        }

/**
 * {@inheritDoc}
 */

        public bool IsEof()
        {
            CheckClosed();
            return pointer >= size;
        }

/**
 * {@inheritDoc}
 */

        public int Available()
        {
            return (int) Math.Min(Length() - GetPosition(), int.MaxValue);
        }

        public void ReturnToBeginning()
        {
            Seek(0);
        }

/**
 * {@inheritDoc}
 */

        public int Peek()
        {
            int result = Read();
            if (result != -1)
            {
                Rewind(1);
            }
            return result;
        }

/**
 * {@inheritDoc}
 */

        public void Rewind(int bytes)
        {
            CheckClosed();
            Seek(GetPosition() - bytes);
        }

/**
 * {@inheritDoc}
 */

        public byte[] ReadFully(int length)
        {
            byte[]
                b = new byte[length];
            int bytesRead = Read(b);
            while (bytesRead < length)
            {
                bytesRead += Read(b, bytesRead, length - bytesRead);
            }
            return b;
        }

        /**
         * {@inheritDoc}
         */

        public int Read(byte[] b)
        {
            return Read(b, 0, b.Length);
        }

        public void Unread(int b)
        {
            Rewind(1);
        }

        public void Unread(byte[] bytes)
        {
            Rewind(bytes.Length);
        }

        public void Unread(byte[] bytes, int start, int len)
        {
            Rewind(len - start);
        }
    }
}
