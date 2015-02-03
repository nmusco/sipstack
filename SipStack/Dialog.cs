namespace SipStack
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using SipStack.Isup;
    using SipStack.Media;

    public enum DialogState
    {
        Dialing,

        PreAck,
        Answered,
        Hanging,
        Hungup
    }

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

    public class Dialog
    {
        private static long sdpIds = 1000;

        private readonly IPEndPoint remoteHost;

        private readonly UdpClient connection;

        private readonly CancellationTokenSource byeRequest = new CancellationTokenSource();

        private readonly int sipPort;

        private readonly EventHandler<DialogState> stateChanged;

        private int callSequence = (int)(DateTime.Now.Ticks % 1000);

        private RtpStream rtp;

        private Dialog(string callId, IPEndPoint remoteHost, int sipPort, EventHandler<DialogState> stateChanged)
        {
            this.remoteHost = remoteHost;
            this.CallId = callId;
            this.sipPort = sipPort;
            this.stateChanged = stateChanged ?? ((a, b) => { });
            this.connection = new UdpClient(sipPort);
        }

        private delegate void MessageReceivedEventHandler(SipMessage request, SipMessage response);

        public string CallId { get; set; }

        public static Dialog InitSipCall(DialogInfo dialogInfo, EventHandler<DialogState> stateChanged = null)
        {
            var dlg = new Dialog(Guid.NewGuid() + "@" + dialogInfo.LocalEndpoint, dialogInfo.RemoteEndpoint, dialogInfo.LocalEndpoint.Port, stateChanged);
            var invite = new InviteMessage(dlg.CallId, dialogInfo.To, dialogInfo.From, dialogInfo.From);
            dialogInfo.Fill(invite);
            var localEp = dialogInfo.LocalRtpEndpoint ?? dialogInfo.LocalEndpoint;
            localEp = new IPEndPoint(localEp.Address, MediaGateway.GetNextPort());

            // TODO: select which codec will be used based on 183 session progress or prack
            dlg.rtp = new RtpStream(localEp, MediaGateway.CreateMedia(MediaGateway.AudioCodec.G711Alaw));

            var availableCodecs = MediaGateway.GetRegisteredCodecs().Select(MediaCodecDescriptor.Describe).ToList();

            var parameters = availableCodecs.SelectMany(a => a.Parameters).ToList();

            invite.SdpData = new Sdp();
            invite.SdpData.AddParameter("o", string.Format("- {0} 0 IN IP4 {1}", Interlocked.Increment(ref sdpIds), localEp.Address))
                .AddParameter("s", "-")
                .AddParameter("c", string.Format("IN IP4 {0}", localEp.Address))
                .AddParameter("t", "0 0")
                .AddParameter("m", string.Format("audio {0} RTP/AVP {1}", localEp.Port, string.Join(" ", availableCodecs.SelectMany(a => a.Identifiers).OrderBy(a => a))));

            foreach (var kvp in parameters)
            {
                invite.SdpData.AddParameter(kvp.Key, kvp.Value);
            }

            invite.SdpData.AddParameter("a", "sendrecv");


            invite.Headers["Via"] = string.Format("SIP/2.0/UDP {0}:{2};rport;branch=z9hG4bK7fe{1}", dialogInfo.LocalEndpoint.Address, DateTime.Now.Ticks.ToString("X8").ToLowerInvariant(), dlg.sipPort);
            invite.Headers["CSeq"] = Interlocked.Increment(ref dlg.callSequence) + " INVITE";

            dlg.Send(invite);
            dlg.stateChanged(dlg, DialogState.Dialing);

            dlg.WaitForMessage(invite, dlg.HandleInviteResponse, 100, 500, 1000, 2000, 5000);

            return dlg;
        }

        public void Hangup()
        {
            this.byeRequest.Cancel();
        }

        private void HandleInviteResponse(SipMessage request, SipMessage response)
        {
            if (response != null)
            {
                switch (response.Method.ToLowerInvariant())
                {
                    case "100":
                        this.WaitForMessage(response, this.Handle100Trying, 100, 500, 1000, 2000, 5000);
                        break;
                    default:
                        throw new InvalidOperationException("Cant handle this message: " + response.Method);
                }
            }
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

        private void Handle100Trying(SipMessage last, SipMessage current)
        {
            if (current == null)
            {
                // TODO: no message received
                return;
            }

            this.WaitForMessage(current, this.Handle183, 10, 50, 100, 1000, 2000, 5000);
        }

        private void Handle183(SipMessage last, SipMessage current)
        {
            if (current.Method == "180")
            {
                this.WaitForMessage(current, this.Handle183, 100, 500, 1000);
                return;
            }

            if (current.SdpData != null)
            {
                this.SetSdpInformation(current.SdpData);
            }

            this.stateChanged(this, DialogState.PreAck);


            var msg = this.WaitAndResend(null, new[] { 4800, 750, 1000, 2000, 5000, 5000 });
            if (msg == null)
            {
                // could not get 200 OK from response
                return;
            }

            var resp = msg as SipResponse;

            if (resp != null && resp.StatusCode == 200)
            {
                var ack = new AckMessage(current.CallId, last.From, current.To, 70, null, current.Via);
                ack.Headers["Allow"] = "INVITE,BYE,REGISTER,ACK,OPTIONS,CANCEL,INFO,SUBSCRIBE,NOTIFY,REFER,UPDATE";

                ack.Contact = last.Contact;

                ack.Headers["CSeq"] = this.callSequence + " ACK";
                ack.Headers["From"] = last.Headers["From"];

                ack.Headers["Session-ID"] = current.Headers["Session-ID"];

                ack.Headers["Content-Length"] = "0";

                this.Send(ack);

                this.stateChanged(this, DialogState.Answered);

                SipMessage response;

                this.TryGetNextMessage(this.byeRequest.Token, out response);

                if (response == null)
                {
                    this.stateChanged(this, DialogState.Hanging);

                    var bye = new SipMessage(current.To, "BYE")
                                  {
                                      CallId = current.CallId,
                                      From = current.From,
                                      MaxForwards = 70,
                                      Contact = current.Contact,
                                      Via = current.Via
                                  };
                    bye.Headers["Allow"] = "INVITE,BYE,REGISTER,ACK,OPTIONS,CANCEL,INFO,SUBSCRIBE,NOTIFY,REFER,UPDATE";

                    bye.Headers["CSeq"] = Interlocked.Increment(ref this.callSequence) + " BYE";

                    bye.Headers["Session-ID"] = current.Headers["Session-ID"];

                    bye.Headers["Content-Length"] = "0";
                    this.Send(bye);
                    this.stateChanged(this, DialogState.Hungup);

                    // TODO: check this response is a 200 OK
                    this.TryGetNextMessage(this.byeRequest.Token, out response);
                }
                else
                {
                    this.stateChanged(this, DialogState.Hanging);
                    // this is a bye request
                    msg = new OkResponse(response.To)
                    {
                        CallId = response.CallId,
                        From = response.From,
                        MaxForwards = 70,
                        Via = response.Via
                    };

                    msg.Headers["Allow"] = "INVITE,BYE,REGISTER,ACK,OPTIONS,CANCEL,INFO,SUBSCRIBE,NOTIFY,REFER,UPDATE";

                    msg.Contact = current.Contact;

                    // TODO: maybe this sequence is not correct
                    msg.Headers["CSeq"] = response.Headers["CSeq"];

                    msg.Headers["Session-ID"] = response.Headers["Session-ID"];

                    msg.Headers["Content-Length"] = "0";

                    this.Send(msg);
                    this.stateChanged(this, DialogState.Hungup);

                }
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

        private void WaitForMessage(SipMessage currentMessage, MessageReceivedEventHandler handler, int timer, params int[] timers)
        {
            Task.Factory.StartNew(
                () =>
                {
                    foreach (var i in new[] { timer }.Concat(timers))
                    {
                        SipMessage msg;
                        if (TryGetNextMessage(i, out msg))
                        {
                            if (msg.CallId == this.CallId)
                            {
                                handler(currentMessage, msg);
                                return;
                            }
                        }
                    }

                    handler(currentMessage, null);
                });
        }

        private void Send(SipMessage msg)
        {
            var dgram = msg.Serialize();
            this.connection.Send(dgram, dgram.Length, this.remoteHost);
        }

        private void TryGetNextMessage(CancellationToken cancel, out SipMessage msg)
        {
            var resp = this.connection.ReceiveAsync();
            resp.Wait(cancel);

            if (resp.IsCanceled)
            {
                msg = null;

                // generate 408 timeout
                return;
            }

            // TODO: copy custom headers as record route
            var data = resp.Result.Buffer;
            msg = SipMessage.Parse(data);
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
            msg = SipMessage.Parse(data);
            return true;
        }
    }
}