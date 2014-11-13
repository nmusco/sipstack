namespace SipStack.Isup
{
    using System;

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
    }
}