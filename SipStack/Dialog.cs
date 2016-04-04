namespace SipStack
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;

    using SipStack.Media;

    public enum DialogState
    {
        Dialing,

        PreAck,
        Answered,
        Hanging,
        Hungup
    }

    public class SipConnection
    {
        private readonly UdpClient client;

        private readonly IPEndPoint remoteHost;

        public SipConnection(UdpClient client, IPEndPoint remoteHost)
        {
            this.client = client;
            this.remoteHost = remoteHost;
        }

        public void Send(SipMessage msg)
        {
            var dgram = msg.Serialize();
            this.client.Send(dgram, dgram.Length, this.remoteHost);
        }

        public bool TryReceive(CancellationTokenSource tks, out SipMessage msg)
        {
            msg = null;

            var resp = this.client.ReceiveAsync();
            UdpReceiveResult result;

            if (resp.IsCanceled)
            {
                Console.WriteLine("resp is cancelled");

                // generate 408 timeout
                return false;
            }

            try
            {
                result = resp.Result;
                resp.Wait(tks.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("operation cancelled");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }

            // TODO: copy custom headers as record route
            var data = result.Buffer;
            msg = SipMessage.Parse(data);
            if (msg != null)
            {
                Console.WriteLine("message received: {0}", msg.Method);
            }

            return true;
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

        public Dialog(string callId, IPEndPoint remoteHost, int sipPort, EventHandler<DialogState> stateChanged)
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

            dlg.WaitForMessage(invite, dlg.HandleInviteResponse, 100, 500, 1000, 2000, 5000);

            dlg.Send(invite);
            dlg.stateChanged(dlg, DialogState.Dialing);


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
                        this.WaitForMessage(response, this.Handle183, 100, 500, 1000, 2000, 5000);
                        break;

                    default:
                        throw new InvalidOperationException("Cant handle this message: " + response.Method);
                }
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
            
            var resp = current as SipResponse;
            if (resp == null)
            {
                Console.WriteLine("sip response is null. expected a 183 response");
                return;
            }

            Console.WriteLine(resp.StatusCode);

            if (resp.StatusCode == 183)
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
                if(this.TryGetNextMessage(60000, out response))
                {
                    this.Handle200Ack(ack, response);
                }
            }
        }

        private void Handle200Ack(SipMessage request, SipMessage response)
        {
            if (response.Method == "200")
            {
                this.stateChanged(this, DialogState.Answered);
                var ack = new AckMessage(request.CallId, request.From, request.To, 70, request.Supported, request.Via);
                ack.Headers["CSeq"] = request.Headers["CSeq"].ToString().Split(' ')[0] + " ACK";
                this.Send(ack);
            }

            while (!this.TryGetNextMessage(100, out response))
            {
                Console.WriteLine("waiting for bye");
                if (this.byeRequest.IsCancellationRequested)
                {
                    response = null;
                    break;
                }
            }

            if (response == null)
            {
                Console.WriteLine("but message was null");
                this.stateChanged(this, DialogState.Hanging);

                var bye = new SipMessage(request.To, "BYE")
                              {
                                  CallId = request.CallId,
                                  From = request.From,
                                  MaxForwards = 70,
                                  Contact = request.Contact,
                                  Via = request.Via
                              };
                bye.Headers["Allow"] = "INVITE,BYE,REGISTER,ACK,OPTIONS,CANCEL,INFO,SUBSCRIBE,NOTIFY,REFER,UPDATE";

                bye.Headers["CSeq"] = Interlocked.Increment(ref this.callSequence) + " BYE";

                bye.Headers["Session-ID"] = request.Headers["Session-ID"];

                bye.Headers["Content-Length"] = "0";
                this.Send(bye);
                this.stateChanged(this, DialogState.Hungup);

                // TODO: check this response is a 200 OK
                //this.TryGetNextMessage(this.byeRequest.Token, out response);
                this.byeRequest.Cancel();
            }
            else
            {
                this.stateChanged(this, DialogState.Hanging);

                // this is a bye request
                var msg = new OkResponse(response.To)
                              {
                                  CallId = response.CallId,
                                  From = response.From,
                                  MaxForwards = 70,
                                  Via = response.Via
                              };

                msg.Headers["Allow"] = "INVITE,BYE,REGISTER,ACK,OPTIONS,CANCEL,INFO,SUBSCRIBE,NOTIFY,REFER,UPDATE";

                msg.Contact = request.Contact;

                // TODO: maybe this sequence is not correct
                msg.Headers["CSeq"] = response.Headers["CSeq"];

                msg.Headers["Session-ID"] = response.Headers["Session-ID"];

                msg.Headers["Content-Length"] = "0";

                this.Send(msg);
                this.stateChanged(this, DialogState.Hungup);

                this.byeRequest.Cancel();
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

        private void WaitForMessage(SipMessage currentMessage, MessageReceivedEventHandler handler, int timer, params int[] timers)
        {
            Task.Factory.StartNew(
                () =>
                {
                    foreach (var i in new[] { timer }.Concat(timers))
                    {
                        SipMessage msg;
                        if (this.TryGetNextMessage(i, out msg))
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

        private bool TryGetNextMessage(CancellationToken cancel, out SipMessage msg)
        {
            msg = null;

            var resp = this.connection.ReceiveAsync();
            UdpReceiveResult result;

            if (resp.IsCanceled)
            {
                Console.WriteLine("resp is cancelled");

                // generate 408 timeout
                return false;
            }

            try
            {
                result = resp.Result;
                resp.Wait(cancel);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("operation cancelled");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return false;
            }

            // TODO: copy custom headers as record route
            var data = result.Buffer;
            msg = SipMessage.Parse(data);
            if (msg != null)
            {
                Console.WriteLine("message received: {0}", msg.Method);
            }

            return true;
        }

        private bool TryGetNextMessage(int timeout, out SipMessage msg)
        {
            return this.TryGetNextMessage(new CancellationTokenSource(timeout).Token, out msg);
        }
    }
}