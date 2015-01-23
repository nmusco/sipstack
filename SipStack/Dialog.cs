namespace SipStack
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    using SipStack.Isup;

    public class Dialog
    {
        private static long sdpIds = 1000;

        private readonly IDictionary<string, Action<SipMessage, SipMessage>> messageHandlers = new Dictionary<string, Action<SipMessage, SipMessage>>(StringComparer.InvariantCultureIgnoreCase);

        private readonly IPEndPoint remoteHost;

        private readonly UdpClient connection;

        private int callSequence = 1;

        private Media media;

        private Dialog(string callId, IPEndPoint remoteHost)
        {
            this.remoteHost = remoteHost;
            this.CallId = callId;
            this.connection = new UdpClient(5060);
            this.messageHandlers["INVITE"] = this.HandleInviteSend;
            this.messageHandlers["100"] = this.Handle100Trying;
            this.messageHandlers["183"] = this.Handle183;
        }

        private void HandleInviteSend(SipMessage last, SipMessage current)
        {
            if (last != null)
            {
                throw new InvalidOperationException("invite message is the first");
            }

            current.Headers["CSeq"] = Interlocked.Increment(ref this.callSequence) + " INVITE";

            this.Send(current);
            var next = this.WaitAndResend(current, new[] { 1000, 2000, 5000 });

            if (next == null)
            {
                // TODO: Generate 408 or throw exception
                throw new TimeoutException("cant send invite after specified period");
            }

            this.PostMessage(current, next);
        }

        private void Handle100Trying(SipMessage last, SipMessage current)
        {
            // do nothing, wait for 183
            var range = new[] { 1000, 2000, 5000 };

            // does not send message, only waits for provisional message
            var next = this.WaitAndResend(null, range) as SipResponse;

            if (next == null)
            {
                throw new TimeoutException("dead while wait for 183");
            }

            if (next.StatusCode != 183)
            {
                throw new InvalidOperationException(string.Format("Cant handle {0} in this state {1}", next.StatusCode, "100"));
            }

            this.PostMessage(last, next);
        }

        private void Handle183(SipMessage last, SipMessage current)
        {
            var msg = new SipMessage("PRACK");

            var headersToCopy = new[] { "Call-ID", "From", "Max-Forwards", "Supported", "To", "Via" };
            foreach (var c in headersToCopy)
            {
                msg.Headers[c] = current.Headers[c];
            }

            msg.Headers["Max-Forwards"] = "70";
            msg.Headers["Content-Length"] = "0";
            msg.Headers["CSeq"] = Interlocked.Increment(ref this.callSequence).ToString() + " PRACK";
            msg.Headers["RAck"] = string.Format("{0} {1}", current.Headers["RSeq"], current.Headers["CSeq"]);

            this.Send(msg);
        }

        private SipMessage WaitAndResend(SipMessage currentMessage, IEnumerable<int> timers)
        {
            SipMessage next = null;
            foreach (var t1 in timers)
            {
                if (this.TryGetNextMessage(t1, out next))
                {
                    break;
                }

                if (currentMessage != null)
                {
                    // try again
                    this.Send(currentMessage);
                }
            }

            return next;
        }

        private void Send(SipMessage msg)
        {
            var dgram = msg.Serialize();
            Console.WriteLine(Encoding.Default.GetString(dgram));
            this.connection.Send(dgram, dgram.Length, this.remoteHost);
        }

        private void PostMessage(SipMessage lastMessage, SipMessage current)
        {
            var method = current.Method;
            if (!this.messageHandlers.ContainsKey(current.Method))
            {
                throw new InvalidOperationException(string.Format("cant handle {0} method", current.Method));
            }

            this.messageHandlers[method](lastMessage, current);
        }

        public string CallId { get; set; }

        public static Dialog InitSipCall(IPEndPoint remoteHost, Contact to, Contact @from, Contact callerContact, string sipAddress, string rtpAddress = null)
        {
            var dlg = new Dialog(Guid.NewGuid() + "@" + sipAddress, remoteHost);
            var invite = new InviteMessage(dlg.CallId, to, @from, @from);

            dlg.media = MediaGateway.CreateMedia(MediaGateway.AudioCodec.G711Alaw, rtpAddress ?? sipAddress);

            invite.SdpData = new Sdp();
            invite.SdpData.AddParameter("o", string.Format("- {0} 0 IN IP4 {1}", Interlocked.Increment(ref sdpIds), dlg.media.LocalEndpoint.Address))
                .AddParameter("s", "-")
                .AddParameter("c", "IN IP4 " + sipAddress)
                .AddParameter("t", "0 0")
                .AddParameter("m", string.Format("audio {0} RTP/AVP 8 101", dlg.media.LocalEndpoint.Port))
                .AddParameter("a", "rtpmap:8 PCMA/8000")
                .AddParameter("a", "rtpmap:101 telephone-event/8000")
                .AddParameter("a", "fmtp:101 0-15")
                .AddParameter("a", "sendrecv");

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

            if (callerContact != null)
            {
                isup.AddOptionalParameter(IsupParameter.OriginalCalledNumber(callerContact, callingNumber.Flags));

                isup.AddOptionalParameter(IsupParameter.RedirectingNumber(callerContact, callingNumber.Flags));

                isup.AddRedirInfo();
            }

            invite.Headers["Via"] = string.Format("SIP/2.0/UDP {0}:5060;rport;branch=z9hG4bK7fe{1}", sipAddress, DateTime.Now.Ticks.ToString("X8").ToLowerInvariant());

            dlg.PostMessage(null, invite);
            return dlg;
        }

        private bool TryGetNextMessage(int timeout, out SipMessage msg)
        {
            var resp = this.connection.ReceiveAsync();

            if (!resp.Wait(timeout))
            {
                msg = null;

                // generate 408 timeout
                return false;
            }

            // TODO: copy custom headers as record route
            var data = resp.Result.Buffer;
            Console.WriteLine(Encoding.Default.GetString(data));
            msg = SipMessage.Parse(data);
            return true;
        }
    }
}