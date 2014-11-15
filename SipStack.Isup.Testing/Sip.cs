namespace SipStack.Tests.Sip
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    using NUnit.Framework;

    using SipStack.Isup;

    [TestFixture]
    public class Sip
    {
        [Test]
        public void DeserializeInviteMessage()
        {
            var callId = "ABC";
            Contact to = "15499@10.0.8.44:5060;user=phone";
            var @from = new Contact(
                "11992971721@10.0.5.25:5060",
                null,
                new KeyValuePair<string, string>("user", "phone"));

            var localIp = "127.0.0.1";

            Contact callerContact = "11972527144@10.0.5.25:5060";

            var invite = new InviteMessage(callId, to, @from, @from);

            var media = MediaGateway.CreateMedia(MediaGateway.AudioCodec.G711Alaw, localIp);

            invite.SdpData = new Sdp();
            invite.SdpData.AddParameter("o", string.Format("- {0} 0 IN IP4 {1}", 10, media.LocalEndpoint.Address))
                .AddParameter("s", "-")
                .AddParameter("c", "IN IP4 " + localIp)
                .AddParameter("t", "0 0")
                .AddParameter("m", string.Format("audio {0} RTP/AVP 8 101", media.LocalEndpoint.Port))
                .AddParameter("a", "rtpmap:8 PCMA/8000")
                .AddParameter("a", "rtpmap:101 telephone-event/8000")
                //.AddParameter("a", "fmtp:8 mode-set=3,6; mode-change-period=2; mode-change-neighbor=1; max-red=0")
                .AddParameter("a", "fmtp:101 0-15")
                .AddParameter("a", "sendrecv");

            var isup = invite.IsupData = new IsupInitialAddress();
            isup.ForwardCallIndicator.LoadParameterData(new byte[] { 0x20, 0x01 });
            isup.CallingPartyCategory.CategoryFlags = CallingPartyCategory.Category.Unknown;
            isup.NatureOfConnectionIndicator.EchoControlIncluded = false;
            isup.NatureOfConnectionIndicator.ContinuityCheckIndicator = NatureOfConnection.ContinuityCheckIndicatorFlags.NotRequired;
            isup.NatureOfConnectionIndicator.SatelliteIndicator = NatureOfConnection.SatelliteIndicatorFlags.One;
            


            isup.CalledNumber.Number = new string(invite.To.Address.TakeWhile(a => a != '@').ToArray());

            isup.CalledNumber.NumberingFlags = NAIFlags.RoutingNotAllowed | NAIFlags.Isdn;
            isup.CalledNumber.Flags = PhoneFlags.NAINationalNumber;
            ;

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

            isup.AddOptionalParameter(new RedirectInfo() { RedirectReason = RedirReason.NoReply, RedirectCounter = 1, RedirectIndicatorFlags = RedirectInfo.RedirectIndicator.CallDiverted });

            invite.Headers["Via"] = string.Format(
                "SIP/2.0/UDP {0}:5060;branch=z9hG4bK7fe{1}",
                media.LocalEndpoint.Address,
                DateTime.Now.Ticks.ToString("X8").ToLowerInvariant());

            var bytes = invite.Serialize();

            var deserialized = SipMessage.Parse(bytes) as InviteMessage;


            Assert.IsNotNull(deserialized, "message isn't an invite");

            Assert.That(invite.Contact, Is.EqualTo(deserialized.Contact));
            Assert.AreEqual(invite.Contact.ToString(), deserialized.Contact.ToString());

            Assert.AreEqual(invite.From, deserialized.From);
            Assert.AreEqual(invite.Method, deserialized.Method);
            Assert.AreEqual(invite.To, deserialized.To);

            Assert.AreEqual(invite.SdpData.ContentText, deserialized.SdpData.ContentText);
        }

        [TestCase("username", "abcd@10.0.0.1", "user=phone", null)]
        [TestCase("\"username\"", "abcd@10.0.0.1", "user=phone", null)]
        [TestCase("\"username\"", "abcd@10.0.0.1", "user=phone", "sip")]
        [TestCase(null, "abcd@10.0.0.1", null, null)]
        [TestCase(null, "11992971721@10.0.5.25:5060", "user=phone", null)]
        [TestCase(null, null, null, null)]
        public void TestContactParsing(string name, string address, string kvps, string protocol)
        {
            string str = string.Empty;

            if (name != null)
            {
                str += name + " ";
            }
            if (address != null)
            {
                if (protocol != null)
                {
                    str += "sip:";
                }
                str += address;
            }

            if (kvps != null)
            {
                str += ";" + kvps;
            }
            Contact contact;
            if (str == string.Empty)
            {
                var argsNull = Assert.Throws<ArgumentNullException>(() => contact = str);
                return;
            }
            contact = str;
            Assert.AreEqual(name, contact.Name);

            Assert.AreEqual(address, contact.Address);

            Assert.AreEqual(protocol, contact.Protocol);

            var parameters = contact.GetParameters();
            Assert.IsNotNull(parameters);
            var kvpJoined = string.Join(";", parameters.Select(a => string.Format("{0}={1}", a.Key, a.Value)));
            Assert.AreEqual(kvps ?? string.Empty, kvpJoined);

        }
    }
}