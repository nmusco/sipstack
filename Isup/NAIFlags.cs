namespace SipStack.Isup
{
    using System;

    [Flags]
    public enum NAIFlags
    {
        RoutingNotAllowed = 128,
        RoutingAllowed = 0,
        Isdn = 1 << 4,
        NetworProvided = 0x2,
        ScreeningVerifiedAndPassed = 1,
        PresentationRestricted = 4
    }
}