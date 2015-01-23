namespace SipStack
{
    using System.Net;

    using SipStack.Media;

    public class AlawMediaCodecCodec : MediaCodec
    {
        public AlawMediaCodecCodec(IPEndPoint localEndpoint)
            : base(localEndpoint)
        {
        }

        protected override void OnPacketReceived(byte[] buffer, long time)
        {
            var rtp = RtpPayload.Parse(buffer);

            // TODO: send to wav device
        }
    }
}