namespace SipStack
{
    using System;
    using System.Diagnostics;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    public abstract class Media : IDisposable
    {
        private readonly UdpClient receiveSocket;
        
        private readonly Task receiveTask;

        private bool disposed;

        internal Media(IPEndPoint localEndpoint)
        {
            this.LocalEndpoint = localEndpoint;
            this.receiveSocket = new UdpClient(this.LocalEndpoint);

            this.receiveTask = Task.Factory.StartNew(
                () =>
                    {
                        var w = Stopwatch.StartNew();
                        while (!this.disposed)
                        {
                            w.Restart();
                            var udpData = this.receiveSocket.ReceiveAsync();
                            if (!udpData.Wait(10))
                            {
                                // this could be a packet lose
                                continue;
                            }

                            if (this.RemoteEndpoint != null && !udpData.Result.RemoteEndPoint.Equals(this.RemoteEndpoint))
                            {
                                // this is wrong
                                continue;
                            }

                            this.OnPacketReceived(udpData.Result.Buffer, w.ElapsedMilliseconds);
                        }
                    }, 
                    TaskCreationOptions.LongRunning);
        }

        public IPEndPoint RemoteEndpoint { get; private set; }

        public IPEndPoint LocalEndpoint { get; private set; }

        public void Dispose()
        {
            this.disposed = true;
            this.receiveSocket.Close();
            if (!this.receiveTask.Wait(1000))
            {
            }
        }

        internal void SetRemoteEndpoint(IPEndPoint remoteEndpoint)
        {
            this.RemoteEndpoint = remoteEndpoint;
        }

        protected virtual void AddSample(byte[] data, int timeout, out bool packetLost)
        {
            using (var r = this.receiveSocket.SendAsync(data, data.Length, this.RemoteEndpoint))
            {
                packetLost = r.Wait(timeout);
            }
        }

        protected abstract void OnPacketReceived(byte[] buffer, long time);
    }
}