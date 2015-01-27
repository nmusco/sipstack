namespace SipStack.Tests.Sip
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using NUnit.Framework;

    using SipStack.Isup;
    using SipStack.Media;

    [TestFixture]
    public class Sip
    {
        [TestCase(true, true)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(false, false)]
        public void DeserializeInviteMessageWithIsupAndSdp(bool includeIsup, bool includeSdp)
        {
            MediaGateway.RegisterCodec(MediaGateway.AudioCodec.G711Alaw, () => new AlawMediaCodec(null, null));

            var callId = "ABC";
            Contact to = "b100@10.0.8.44:5060;user=phone";
            var @from = new Contact(
                "11992971721@10.0.5.25:5060",
                null,
                new[] { new KeyValuePair<string, string>("user", "phone") });

            var localIp = "127.0.0.1";

            Contact callerContact = "11972527144@10.0.5.25:5060";

            var invite = new InviteMessage(callId, to, @from, @from);
            IMediaCodec mediaCodec = null;

            var port = MediaGateway.GetNextPort();

            if (includeSdp)
            {
                mediaCodec = MediaGateway.CreateMedia(MediaGateway.AudioCodec.G711Alaw);
                invite.SdpData = new Sdp();
                invite.SdpData.AddParameter("o", string.Format("- {0} 0 IN IP4 {1}", 10, localIp))
                    .AddParameter("s", "-")
                    .AddParameter("c", "IN IP4 " + localIp)
                    .AddParameter("t", "0 0")
                    .AddParameter("m", string.Format("audio {0} RTP/AVP 8 101", port))
                    .AddParameter("a", "rtpmap:8 PCMA/8000")
                    .AddParameter("a", "rtpmap:101 telephone-event/8000")
                    .AddParameter("a", "fmtp:101 0-15").AddParameter("a", "sendrecv");
            }

            if (includeIsup)
            {
                var isup = new IsupInitialAddress();
                invite.IsupData = isup;
                isup.ForwardCallIndicator.LoadParameterData(new byte[] { 0x20, 0x01 });
                isup.CallingPartyCategory.CategoryFlags = CallingPartyCategory.Category.Unknown;
                isup.NatureOfConnectionIndicator.EchoControlIncluded = false;
                isup.NatureOfConnectionIndicator.ContinuityCheckIndicator =
                    NatureOfConnection.ContinuityCheckIndicatorFlags.NotRequired;
                isup.NatureOfConnectionIndicator.SatelliteIndicator = NatureOfConnection.SatelliteIndicatorFlags.One;

                isup.CalledNumber.Number = new string(invite.To.Address.TakeWhile(a => a != '@').ToArray());

                isup.CalledNumber.NumberingFlags = NAIFlags.RoutingNotAllowed | NAIFlags.Isdn;
                isup.CalledNumber.Flags = PhoneFlags.NAINationalNumber;

                var callingNumber = invite.IsupData.AddOptionalParameter(new IsupPhoneNumberParameter(IsupParameterType.CallingPartyNumber) { Number = invite.From.Address.Split('@').FirstOrDefault() });

                callingNumber.NumberingFlags |= NAIFlags.ScreeningVerifiedAndPassed | NAIFlags.NetworProvided;

                isup.AddOptionalParameter(new IsupPhoneNumberParameter(IsupParameterType.OriginalCalledNumber)
                {
                    Number = callerContact.Address.Split('@').FirstOrDefault(),
                    Flags = callingNumber.Flags,
                    NumberingFlags = NAIFlags.PresentationRestricted | NAIFlags.Isdn
                });

                isup.AddOptionalParameter(new IsupPhoneNumberParameter(IsupParameterType.RedirectingNumber)
                {
                    Number = callerContact.Address.Split('@').FirstOrDefault(),
                    Flags = callingNumber.Flags,
                    NumberingFlags = NAIFlags.PresentationRestricted | NAIFlags.Isdn
                });

                isup.AddOptionalParameter(new RedirectInfo { RedirectReason = RedirReason.NoReply, RedirectCounter = 1, RedirectIndicatorFlags = RedirectInfo.RedirectIndicator.CallDiverted });
            }

            if (includeSdp)
            {
                invite.Headers["Via"] = string.Format(
                "SIP/2.0/UDP {0}:5060;branch=z9hG4bK7fe{1}", localIp,
                DateTime.Now.Ticks.ToString("X8").ToLowerInvariant());
            }

            var bytes = invite.Serialize();

            var deserialized = SipMessage.Parse(bytes) as InviteMessage;

            Assert.IsNotNull(deserialized, "message isn't an invite");

            Assert.That(invite.Contact, Is.EqualTo(deserialized.Contact));
            Assert.AreEqual(invite.Contact.ToString(), deserialized.Contact.ToString());

            Assert.AreEqual(invite.From, deserialized.From);
            Assert.AreEqual(invite.To, deserialized.To);

            if (includeSdp)
            {
                Assert.AreEqual(invite.SdpData.ContentText, deserialized.SdpData.ContentText);
            }

            if (includeIsup)
            {
                Assert.AreEqual(invite.IsupData.GetByteArray().ToHex().ToUpper(), deserialized.IsupData.GetByteArray().ToHex().ToUpper());
            }

            Assert.AreEqual(invite.Method, deserialized.Method);
        }
    }
}