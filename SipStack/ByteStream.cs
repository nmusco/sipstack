namespace SipStack
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class ByteStream
    {
        private readonly byte[] buffer;

        private int offset;

        public ByteStream(byte[] buffer, int startOffset)
        {
            this.buffer = buffer;
            this.offset = startOffset;
        }

        public int Length
        {
            get
            {
                return this.buffer.Length;
            }
        }

        public int Position
        {
            get
            {
                return this.offset;
            }
        }

        public byte Read()
        {
            return this.buffer[this.offset++];
        }

        public byte[] Read(int len)
        {
            var data = new byte[len];

            Buffer.BlockCopy(this.buffer, this.offset, data, 0, len);
            this.offset += len;
            return data;
        }

        public string ReadLine(string endOfLine = "\r\n")
        {
            var byteCmp = 0;
            var readPos = 0;
            for (var i = this.offset; i < this.buffer.Length; i++)
            {
                if (this.buffer[i] == endOfLine[byteCmp])
                {
                    byteCmp++;
                    if (byteCmp == endOfLine.Length)
                    {
                        readPos++;
                        break;
                    }
                }
                else
                {
                    byteCmp = 0;
                }

                readPos++;
            }

            if (readPos > 0)
            {
                // need to discard endOfLine
                return Encoding.Default.GetString(this.Read(readPos)).Substring(0, readPos - 2);
            }

            return null;
        }

        public IEnumerable<string> Lines(string lineFeed = "\r\n")
        {
            string l;
            while ((l = this.ReadLine(lineFeed)) != null)
            {
                yield return l;
            }
        }
    }
}