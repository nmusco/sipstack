namespace SipStack
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.Remoting.Messaging;
    using System.Text;
    using System.Threading;

    using SipStack.Isup;

    public class Dialog
    {
        private static long sdpIds = 1000;

        private readonly IPEndPoint remoteHost;

        private readonly UdpClient connection;

        private Dialog(string callId, IPEndPoint remoteHost)
        {
            this.remoteHost = remoteHost;
            this.CallId = callId;
            this.connection = new UdpClient(5060);
        }

        public string CallId { get; set; }

        public static Dialog InitSipCall(IPEndPoint remoteHost, Contact to, Contact @from, Contact callerContact, string localIp)
        {
            var dlg = new Dialog(Guid.NewGuid().ToString() + "@" + localIp, remoteHost);
            var invite = new InviteMessage(dlg.CallId, to, @from, @from);

            invite.ContactParameters.Add(new KeyValuePair<string, string>("user", "phone"));

            var media = MediaGateway.CreateMedia(MediaGateway.AudioCodec.G711Alaw, localIp);

            invite.ContactParameters.Add(new KeyValuePair<string, string>("user", "phone"));

            invite.SdpData = new Sdp();
            invite.SdpData.AddParameter("o", string.Format("- {0} 0 IN IP4 {1}", Interlocked.Increment(ref sdpIds), media.LocalEndpoint.Address))
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
            isup.NatureOfConnectionIndicator.EchoControlIncluded = false;
            isup.NatureOfConnectionIndicator.SatelliteIndicator =NatureOfConnection.SatelliteIndicatorFlags.One;
            isup.ForwardCallIndicator.LoadParameterData(new byte[] { 0x20, 0x01 });
            isup.CallingPartyCategory.LoadParameterData(new byte[] { 0xe0 });


            isup.CalledNumber.Number = new string(invite.To.Address.TakeWhile(a => a != '@').ToArray());

            isup.CalledNumber.NumberingFlags = NAIFlags.RoutingNotAllowed | NAIFlags.Isdn;
            isup.CalledNumber.Flags = PhoneFlags.NAINationalNumber;
            ;

            var callingNumber = invite.IsupData.AddOptionalParameter(new IsupPhoneNumberParameter(IsupParameterType.CallingPartyNumber) { Number = invite.From.Address.Split('@').FirstOrDefault() });

            callingNumber.NumberingFlags |= NAIFlags.ScreeningVerifiedAndPassed | NAIFlags.NetworProvided;
            isup.AddOptionalParameter(new IsupPhoneNumberParameter(IsupParameterType.OriginalCalledNumber)
                                          {
                                              Number = callerContact.Address.Split('@').FirstOrDefault(), 
                                              Flags = callingNumber.Flags, NumberingFlags = NAIFlags.PresentationRestricted |NAIFlags.Isdn
                                          });

            isup.AddOptionalParameter(new IsupPhoneNumberParameter(IsupParameterType.RedirectingNumber) { Number = callerContact.Address.Split('@').FirstOrDefault(), Flags = callingNumber.Flags,
                                                                                                          NumberingFlags = NAIFlags.PresentationRestricted | NAIFlags.Isdn
            });

            isup.AddOptionalParameter(new RedirectInfo() { RedirectReason = RedirReason.NoReply, RedirectCounter = 1, RedirectIndicatorFlags = RedirectInfo.RedirectIndicator.CallDiverted });

            invite.Headers["Via"] = string.Format(
                "SIP/2.0/UDP {0}:5060;branch=z9hG4bK7fe{1}",
                media.LocalEndpoint.Address,
                DateTime.Now.Ticks.ToString("X8").ToLowerInvariant());
            var response = dlg.SendAndWaitResponse(invite);
            return dlg;
        }

        private SipMessage SendAndWaitResponse(InviteMessage invite)
        {
            var dgram = Encoding.Default.GetBytes(invite.Serialize());
            this.connection.Send(dgram, dgram.Length, this.remoteHost);

            var resp = this.connection.ReceiveAsync();

            if (!resp.Wait(5000))
            {
                // generate 408 timeout
                throw new TimeoutException();
            }

            // TODO: copy custom headers as record route
            var data = resp.Result.Buffer;

            var result = SipResponse.Parse(data);
            Console.WriteLine(Encoding.Default.GetString(data));

            throw new NotImplementedException();
        }
    }
}