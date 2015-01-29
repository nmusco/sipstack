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
    using SipStack.Media;

    public class Dialog
    {
        private static long sdpIds = 1000;

        private readonly IDictionary<string, Action<SipMessage, SipMessage>> messageHandlers = new Dictionary<string, Action<SipMessage, SipMessage>>(StringComparer.InvariantCultureIgnoreCase);

        private readonly IPEndPoint remoteHost;

        private readonly UdpClient connection;

        private int callSequence = (int)(DateTime.Now.Ticks % 1000);

        private RtpStream rtp;

        private Dialog(string callId, IPEndPoint remoteHost)
        {
            this.remoteHost = remoteHost;
            this.CallId = callId;
            this.connection = new UdpClient(5060);
            this.messageHandlers["INVITE"] = this.HandleInviteSend;
            this.messageHandlers["100"] = this.Handle100Trying;
            this.messageHandlers["180"] = this.Handle100Trying;
            this.messageHandlers["183"] = this.Handle183;
            this.messageHandlers["500"] = this.HandleError;
            this.messageHandlers["PRACK"] = null;    
        }

        public string CallId { get; set; }

        public static Dialog InitSipCall(IPEndPoint remoteHost, Contact to, Contact from, Contact callerContact, string sipAddress, string rtpAddress = null)
        {
            var dlg = new Dialog(Guid.NewGuid() + "@" + sipAddress, remoteHost);
            var invite = new InviteMessage(dlg.CallId, to, @from, @from);
            var localEp = new IPEndPoint(IPAddress.Parse(rtpAddress ?? sipAddress), MediaGateway.GetNextPort());

            dlg.rtp = new RtpStream(localEp, MediaGateway.CreateMedia(MediaGateway.AudioCodec.G711Alaw));

            invite.SdpData = new Sdp();
            invite.SdpData.AddParameter("o", string.Format("- {0} 0 IN IP4 {1}", Interlocked.Increment(ref sdpIds), localEp.Address))
                .AddParameter("s", "-")
                .AddParameter("c", string.Format("IN IP4 {0}", sipAddress))
                .AddParameter("t", "0 0")
                .AddParameter("m", string.Format("audio {0} RTP/AVP 8 101", localEp.Port))
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

        private void HandleError(SipMessage last, SipMessage next)
        {
            var n = next as SipResponse;
            var msg = new AckMessage(last.CallId, last.From, last.To, last.MaxForwards, last.Supported, last.Via);

            if (n == null || n.StatusCode == 500)
            {
                this.Send(msg);
            }
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
            var range = new[] { 1000, 2000, 5000, 10000, 30000 };

            // does not send message, only waits for provisional message
            var next = this.WaitAndResend(null, range) as SipResponse;

            if (next == null)
            {
                throw new TimeoutException("dead while wait for 18x");
            }

            if (next.StatusCode == 180)
            {
                this.PostMessage(last, next);
                return;
            }

            if (next.StatusCode == 183)
            {
                this.PostMessage(last, next);
            }
        }

        private void Handle183(SipMessage last, SipMessage current)
        {
            Console.WriteLine("handling 183 message");
            
            if (current.SdpData != null)
            {
                this.SetSdpInformation(current.SdpData);
            }

            var msg = this.WaitAndResend(null, new[] { 4800, 750, 1000, 2000, 5000, 5000 });
            if (msg == null)
            {
                // could not get 200 OK from response
                return;
            }

            var resp = msg as SipResponse;

            if ((resp != null && resp.StatusCode == 200) || msg.Method == "BYE")
            {
                var ack = new AckMessage(current.CallId, last.From, current.To, 70, null, current.Via);
                ack.Headers["Allow"] = "INVITE,BYE,REGISTER,ACK,OPTIONS,CANCEL,INFO,SUBSCRIBE,NOTIFY,REFER,UPDATE";

                ack.Contact = last.Contact;
                
                ack.Headers["CSeq"] = this.callSequence + " ACK";
                ack.Headers["From"] = last.Headers["From"];
                
                ack.Headers["Session-ID"] = current.Headers["Session-ID"];

                ack.Headers["Content-Length"] = "0";

                this.Send(ack);
            }
        }

        private void SetSdpInformation(Sdp sdp)
        {
            var address = sdp.Parameters.FirstOrDefault(a => a.Key == "c").Value.Split(' ').Last();

            var remotePort =
                sdp.Parameters.FirstOrDefault(a => a.Key == "m" && a.Value.StartsWith("audio"))
                    .Value.Split(' ')
                    .Skip(1)
                    .First();

            this.rtp.SetRemoteEndpoint(new IPEndPoint(IPAddress.Parse(address), int.Parse(remotePort)));
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
            this.connection.Send(dgram, dgram.Length, this.remoteHost);
            Console.WriteLine(Encoding.Default.GetString(dgram));
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