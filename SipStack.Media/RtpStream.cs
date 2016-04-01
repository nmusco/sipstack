namespace SipStack.Media
{
    using System;
    using System.Net;
    using System.Net.Sockets;

    public sealed class RtpStream : IDisposable
    {
        private readonly Action<RtpPayload, int> receiveCallback;

        private readonly UdpClient receiveSocket;
        
        private bool disposed;

        public RtpStream(IPEndPoint localEndpoint, IMediaCodec mediaCodec)
        {
            this.LocalEndpoint = localEndpoint;
            this.receiveSocket = new UdpClient(this.LocalEndpoint);
            this.receiveCallback = mediaCodec.OnPacketReceived;

            this.receiveSocket.BeginReceive(this.OnReceived, null);
            mediaCodec.SetRecordingDelegate(this.AddSample);
        }

        public IPEndPoint RemoteEndpoint { get; private set; }

        public IPEndPoint LocalEndpoint { get; private set; }

        public void Dispose()
        {
            this.disposed = true;
            this.receiveSocket.Close();
        }

        public void SetRemoteEndpoint(IPEndPoint remoteEndpoint)
        {
            this.RemoteEndpoint = remoteEndpoint;
        }

        public void AddSample(RtpPayload rtpPacket)
        {
            if (this.RemoteEndpoint == null)
            {
                return;
            }

            var data = rtpPacket.ToArray();
            
            this.receiveSocket.Send(data, data.Length, this.RemoteEndpoint);
        }

        private void OnReceived(IAsyncResult ar)
        {
            var ep = this.RemoteEndpoint;

            var buffer = this.receiveSocket.EndReceive(ar, ref ep);
            if (!this.disposed)
            {
                this.receiveSocket.BeginReceive(this.OnReceived, null);
            }

            if (buffer != null && buffer.Length > 0)
            {
                var packet = new RtpPayload(buffer);
                this.receiveCallback.BeginInvoke(packet, 0, null, null);
            }
        }
    }
}