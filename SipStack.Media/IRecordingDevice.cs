namespace SipStack.Media
{
    using System;

    public enum Digit
    {
        One = 1,

        Two = 2,

        Three = 3,

        Four = 4,

        Five = 5,

        Six = 6,

        Seven = 7,

        Eight = 8,

        Nine = 9,

        Zero = 0,

        Star = 10,

        Hash = 11,

        A = 12,

        B = 13,

        C = 14,

        D = 15
    }

    public interface IRecordingDevice
    {
        void PlayDtmf(Digit digit, int duration);

        void SetSendDelegate(Action<RtpPayload> delegateMethod);
    }
}