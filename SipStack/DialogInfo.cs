namespace SipStack
{
    using System.Linq;
    using System.Net;

    using SipStack.Isup;

    public class DialogInfo
    {
        public Contact From { get; set; }

        public Contact To { get; set; }

        public Contact OriginalCalledNumber { get; set; }

        public IPEndPoint RemoteEndpoint { get; set; }

        public IPEndPoint LocalEndpoint { get; set; }

        public IPEndPoint LocalRtpEndpoint { get; set; }

        public void Fill(InviteMessage invite)
        {
            var isup = new IsupInitialAddress();
            invite.IsupData = isup;
            isup.NatureOfConnectionIndicator.EchoControlIncluded = false;
            isup.NatureOfConnectionIndicator.SatelliteIndicator = NatureOfConnection.SatelliteIndicatorFlags.One;
            isup.ForwardCallIndicator.LoadParameterData(new byte[] { 0x20, 0x01 });
            isup.CallingPartyCategory.LoadParameterData(new byte[] { 0xe0 });

            isup.CalledNumber.Number = new string(invite.To.Address.TakeWhile(a => a != '@').ToArray());

            isup.CalledNumber.NumberingFlags = NAIFlags.RoutingNotAllowed | NAIFlags.Isdn;
            isup.CalledNumber.Flags = PhoneFlags.NAINationalNumber;

            var callingNumber = invite.IsupData.AddOptionalParameter(new IsupPhoneNumberParameter(IsupParameterType.CallingPartyNumber) { Number = invite.From.Address.Split('@').FirstOrDefault() });

            callingNumber.NumberingFlags |= NAIFlags.ScreeningVerifiedAndPassed | NAIFlags.NetworProvided;

            if (this.OriginalCalledNumber != null)
            {
                isup.AddOptionalParameter(IsupParameter.OriginalCalledNumber(this.OriginalCalledNumber, callingNumber.Flags));

                isup.AddOptionalParameter(IsupParameter.RedirectingNumber(this.OriginalCalledNumber, callingNumber.Flags));

                isup.AddRedirInfo();
            }
        }
    }
}