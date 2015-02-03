namespace SipStack.Isup
{
    public static class IsupInitialAddressExtensions
    {
        public static IsupInitialAddress AddRedirInfo(
            this IsupInitialAddress isup,
            RedirReason reason = RedirReason.NoReply,
            byte counter = 1,
            RedirectInfo.RedirectIndicator indicator = RedirectInfo.RedirectIndicator.CallDiverted)
        {
            isup.AddOptionalParameter(
                new RedirectInfo { RedirectReason = reason, RedirectCounter = counter, RedirectIndicatorFlags = indicator });
            return isup;
        }
    }
}