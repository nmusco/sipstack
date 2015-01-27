namespace SipStack.Media
{
    using System;

    public interface IMediaCodec
    {
        void OnPacketReceived(RtpPayload payload, int timeTaken);

        void SetRecordingDelegate(Action<RtpPayload> method);
    }
}