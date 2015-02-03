namespace SipStack
{
    using System;
    using System.Linq;
    using System.Threading;

    using SipStack.Media;

    public class RtpEvent
    {
        private readonly Digit digit;

        private readonly short sequenceNumber;

        private readonly int identifier;

        private long numberOfPackets;

        private long packetsSent;

        public RtpEvent(Digit digit, int duration, short sequenceNumber, int identifier)
        {
            this.digit = digit;
            this.sequenceNumber = sequenceNumber;
            this.identifier = identifier;
            this.numberOfPackets = duration / 160;
            this.packetsSent = 0;
        }

        public RtpPayload GetData(long timestamp)
        {
            Interlocked.Increment(ref this.packetsSent);
            var endOfEvent = Interlocked.Read(ref this.numberOfPackets) == 1;

            // volume = 10 = 0x0a or 0x8a
            var data =
                new[] { (byte)this.digit, (byte)(endOfEvent ? 0x8a : 0x0a) }.Concat(
                    BitConverter.GetBytes((short)(this.packetsSent * 160)).Reverse()).ToArray();

            var payload = new RtpPayload(data, this.sequenceNumber, this.identifier, timestamp, false, 0x65);

            Interlocked.Add(ref this.numberOfPackets, -1);

            return payload;
        }

        public bool IsValid()
        {
            return Interlocked.Read(ref this.numberOfPackets) > 0;
        }
    }
}