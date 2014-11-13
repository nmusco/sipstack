namespace SipStack
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;

    using SipStack.Isup;

    public class Dialog
    {
        private readonly IPEndPoint remoteHost;

        private UdpClient connection;

        public string CallId { get; set; }

        private Dialog(string callId, IPEndPoint remoteHost)
        {
            this.remoteHost = remoteHost;
            this.CallId = callId;
            this.connection = new UdpClient(5060);
        }

        private static long sdpIds = 1000;
        public static Dialog InitSipCall(IPEndPoint remoteHost, Contact to, Contact @from, Contact callerContact, string localIp)
        {
            var dlg = new Dialog(Guid.NewGuid().ToString(), remoteHost);
            var invite = new InviteMessage(dlg.CallId, to, @from, callerContact);

            invite.ContactParameters.Add(new KeyValuePair<string, string>("user", "phone"));

            var media = MediaGateway.CreateMedia(MediaGateway.AudioCodec.G711Alaw, localIp);

            invite.ContactParameters.Add(new KeyValuePair<string, string>("user", "phone"));


            invite.SdpData = new Sdp();
            invite.SdpData
                .AddParameter("o", string.Format("- {0} 0 IN IP4 {1}", Interlocked.Increment(ref sdpIds), media.LocalEndpoint.Address))
                .AddParameter("s", "IMSS")
                .AddParameter("c", "IN IP4 10.0.5.25")
                .AddParameter("t", "0 0")
                .AddParameter("m", string.Format("audio {0} RTP/AVP 8 101", media.LocalEndpoint.Port))
                .AddParameter("a", "rtpmap:101 telephone-event/8000")
                .AddParameter("a", "fmtp:101 0-15")
                .AddParameter("a", "recvonly");
            // TODO: insert isup body here

            invite.IsupData = new IsupInitialAddress()
                                  {
                                      CalledNumber = new string(invite.To.Address.TakeWhile(a => a != '@').ToArray()),
                                      CallingNumber = new string(invite.From.Address.TakeWhile(a => a != '@').ToArray()),
                                      RedirectCounter = 1,
                                      RedirectingNumber = new string(invite.From.Address.TakeWhile(a => a != '@').ToArray()),
                                      OriginalCalledNumber = new string(invite.From.Address.TakeWhile(a => a != '@').ToArray()),
                                      RedirectReason = RedirReason.NoReply,
                                  };

            invite.Headers["Via"] = string.Format(
                "SIP/2.0/UDP {0}:5060;branch=z9hG4bK{1}",
                media.LocalEndpoint.Address,
                DateTime.Now.Ticks.ToString("X8"));
            var response = dlg.SendAndWaitResponse(invite);
            return dlg;
        }

        private SipMessage SendAndWaitResponse(InviteMessage invite)
        {
            var dgram = Encoding.Default.GetBytes(invite.Serialize());
            this.connection.Send(dgram, dgram.Length, this.remoteHost);

            var resp = this.connection.ReceiveAsync();

            if (!resp.Wait(1000))
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

    public abstract class Body
    {
        public string ContentType { get; protected set; }

        public abstract string ContentText { get; }

        public IDictionary<string, string> Headers { get; private set; }

        public Body()
        {
            this.Headers = new Dictionary<string, string>();
        }

    }

    class Program
    {
        static void Main(string[] args)
        {
            var responseText = Encoding.Default.GetBytes(@"BYE sip:15499@10.208.126.162:5060;user=phone SIP/2.0
Via: SIP/2.0/UDP 10.216.99.173:5060;branch=z9hG4bK7a95db40
To: <sip:15499@ntch1s.cta;user=phone>;tag=A731-9C13
From: ""4599082206"" <sip:4599082206@timbr1S.com;user=phone>;tag=7a95d4df-0050-004b-0000-0000
Call-ID: 7a95d4547a95d43-0050-004b-0000-0000@10.216.99.141
CSeq:  3 BYE
Route:  <sip:10.208.126.170;lr;did=d6.644f5eb4>
Max-Forwards: 70
Supported: 100rel
Reason: Q.850; cause=31
Content-Type: application/isup; version=itu-t92+; base=itu-t92+
Content-Disposition: signal; handling=optional
Content-Length: 6

").Concat(new byte[] { 0x0c, 0x02, 0x00, 0x02, 0x83, 0x9f }).ToArray();
            //var response = SipResponse.Parse(responseText);
            var @from = new Contact()
                            {
                                Address = "11992971271@localhost",
                                Name = "11992971271",
                                Parameters =
                                    new List<KeyValuePair<string, string>>
                                        {
                                            new KeyValuePair<string, string>(
                                                "user",
                                                "phone")
                                        }
                            };
            var remoteHost = new IPEndPoint(Dns.GetHostEntry("vpn.ti24h.net").AddressList.First(), 5060);
            var dialog = Dialog.InitSipCall(remoteHost, "15499@ntch2s.spo", @from, "119929712171@localhost:5060", "10.8.0.113");

            Console.Read();
        }
    }

    public class MediaGateway
    {
        private const int InitialPort = 10000;

        private static int CurrentPort = InitialPort;
        public enum AudioCodec
        {
            G711Alaw
        }

        public static Media CreateMedia(AudioCodec codec, string localAddress)
        {
            var nextPort = Interlocked.Increment(ref CurrentPort);
            Interlocked.CompareExchange(ref CurrentPort, InitialPort, InitialPort);

            var localEp = new IPEndPoint(IPAddress.Parse(localAddress), nextPort);

            return new Media(localEp);
        }

        public class Media
        {
            private UdpClient receiveSocket;

            private UdpClient sendSocket;

            public IPEndPoint RemoteEndpoint { get; private set; }

            public IPEndPoint LocalEndpoint { get; private set; }
            public void AddSample(byte[] data)
            {
                var r = this.sendSocket.SendAsync(data, data.Length, this.RemoteEndpoint);

                if (!r.Wait(50))
                {
                    // TODO: generate packet loss
                }

                r.Dispose();
            }

            public byte[] GetSample()
            {
                var udpData = this.receiveSocket.ReceiveAsync();
                if (!udpData.Wait(50))
                {
                    // TODO: get codec info
                    return new byte[160];
                }
                if (udpData.Result.RemoteEndPoint != this.RemoteEndpoint)
                {
                    throw new InvalidOperationException("sender remote endpoint not same as initial remote endpoint");
                }
                return udpData.Result.Buffer;
            }

            internal Media(IPEndPoint localEndpoint)
            {
                this.LocalEndpoint = localEndpoint;
                this.receiveSocket = new UdpClient(this.LocalEndpoint);
            }

            public void SetRemoteEndpoint(string address, int port)
            {
                this.RemoteEndpoint = new IPEndPoint(IPAddress.Parse(address), port);
            }
        }
    }
}
