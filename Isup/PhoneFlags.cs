namespace SipStack.Isup
{
    using System;

    [Flags]
    public enum PhoneFlags
    {
        IsOdd = 128,
        IsEven = 0,
        NAINationalNumber = 3,
    }
}