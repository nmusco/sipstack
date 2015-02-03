namespace SipStack.Media
{
    using System;
    using System.Linq;

    public class RtpPayload
    {
        private readonly int identifier;

        private readonly byte[] data;

        private readonly long timestamp;

        private readonly bool isFirst;

        private readonly byte payloadType;

        private readonly short sequenceNumber;

        public RtpPayload(byte[] data, short sequenceNumber, int identifier, long timestamp, bool isFirst, byte payloadType = 0x08)
        {
            this.data = data;
            this.timestamp = timestamp;
            this.isFirst = isFirst;
            this.payloadType = payloadType;
            this.sequenceNumber = sequenceNumber;
            this.identifier = identifier;
        }

        public RtpPayload(byte[] data)
        {
            var idx = 1;

            this.isFirst = data[idx++] == (0x80 | 0x08); // 0x08 is the codec identifier

            // RtpEvent is not supported yet
            var shortData = new[] { data[idx++], data[idx++] }.ToArray();
            this.sequenceNumber = (short)(shortData[1] | shortData[0] << 8);

            this.timestamp = BitConverter.ToInt32(data.Skip(idx).Take(4).Reverse().ToArray(), 0);
            idx += 4;
            this.identifier = BitConverter.ToInt32(data.Skip(idx).Take(4).Reverse().ToArray(), 0);
            idx += 4;
            this.data = data.Skip(idx).ToArray();
        }

        public byte[] Data
        {
            get
            {
                return this.data;
            }
        }

        public byte[] ToArray()
        {
            var total = new byte[12 + this.data.Length];
            total[0] = 0x80; // version without padding without extension
            total[1] = (byte)((this.isFirst ? 0x80 : 0x0) | this.payloadType);
            var seq = BitConverter.GetBytes(this.sequenceNumber);
            var ts = BitConverter.GetBytes((int)(this.timestamp % int.MaxValue));
            var id = BitConverter.GetBytes(this.identifier);
            Array.Reverse(seq);
            Array.Reverse(ts);
            Array.Reverse(id);

            Buffer.BlockCopy(seq, 0, total, 2, 2);
            Buffer.BlockCopy(ts, 0, total, 4, 4);
            Buffer.BlockCopy(id, 0, total, 8, 4);
            Buffer.BlockCopy(this.data, 0, total, 12, this.data.Length);
            return total;
        }
    }
}