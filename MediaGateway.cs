namespace SipStack
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    public class MediaGateway
    {
        private const int InitialPort = 10115;

        private static int currentPort = InitialPort;

        public enum AudioCodec
        {
            G711Alaw
        }

        public static Media CreateMedia(AudioCodec codec, string localAddress)
        {
            var nextPort = Interlocked.Increment(ref currentPort);
            Interlocked.CompareExchange(ref currentPort, InitialPort, InitialPort);

            var localEp = new IPEndPoint(IPAddress.Parse(localAddress), nextPort);

            return new Media(localEp);
        }

        public class Media
        {
            private readonly UdpClient receiveSocket;

            private UdpClient sendSocket;

            internal Media(IPEndPoint localEndpoint)
            {
                this.LocalEndpoint = localEndpoint;
                this.receiveSocket = new UdpClient(this.LocalEndpoint);
            }

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

                if (!udpData.Result.RemoteEndPoint.Equals(this.RemoteEndpoint))
                {
                    throw new InvalidOperationException("sender remote endpoint not same as initial remote endpoint");
                }

                return udpData.Result.Buffer;
            }

            public void SetRemoteEndpoint(string address, int port)
            {
                this.RemoteEndpoint = new IPEndPoint(IPAddress.Parse(address), port);
            }
        }
    }
}