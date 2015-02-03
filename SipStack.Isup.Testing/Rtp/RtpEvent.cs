namespace SipStack.Tests.Rtp
{
    using System;
    using System.Linq;

    using NUnit.Framework;

    using SipStack.Media;

    [TestFixture]
    public class RtpEventTest
    {
        [TestCase(Digit.Zero, 320, 1000, 0xa, 2)]
        [TestCase(Digit.One, 320, 1000, 0xa, 2)]
        [TestCase(Digit.Two, 320, 1000, 0xa, 2)]
        [TestCase(Digit.Three, 320, 1000, 0xa, 2)]
        [TestCase(Digit.Four, 320, 1000, 0xa, 2)]
        [TestCase(Digit.A, 320, 1000, 0xa, 2)]
        [TestCase(Digit.Hash, 320, 1000, 0xa, 2)]
        [TestCase(Digit.Hash, 3200, 1000, 0xa, 20)]
        [TestCase(Digit.Hash, 160, 1000, 0xa, 1)]
        public void TestRtpEvent(Digit digit, int duration, short sequenceNumber, short identifier, int numberOfPackets)
        {
            var ev = new RtpEvent(digit, duration, sequenceNumber, identifier);
            for (var i = 0; i < numberOfPackets; i++)
            {
                var endOfEvent = i == numberOfPackets - 1;

                var payload = ev.GetData(0).Data;
                var expectedData = new[] { (byte)digit, (byte)(endOfEvent ? 0x8a : 0x0a) }.Concat(BitConverter.GetBytes((short)((i + 1) * 160)).Reverse()).ToArray();
                for (var x = 0; x < payload.Length; x++)
                {
                    Assert.AreEqual(expectedData[x], payload[x]);
                }
            }
        }
    }
}
