namespace SipStack
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;

    using SipStack.Media;

    public class MediaGateway
    {
        private const int InitialPort = 10115;

        private static readonly IDictionary<AudioCodec, Func<IPEndPoint, MediaCodec>> CodecFactory = new Dictionary<AudioCodec, Func<IPEndPoint, MediaCodec>>();

        private static int currentPort = InitialPort;

        public enum AudioCodec
        {
            G711Alaw
        }

        public static MediaCodec CreateMedia(AudioCodec codec, string localAddress)
        {
            if (!CodecFactory.ContainsKey(codec))
            {
                throw new ArgumentOutOfRangeException("codec", string.Format("Factory for codec {0} not found", codec));
            }

            var nextPort = Interlocked.Increment(ref currentPort);
            Interlocked.CompareExchange(ref currentPort, InitialPort, InitialPort);

            var localEp = new IPEndPoint(IPAddress.Parse(localAddress), nextPort);

            return CodecFactory[codec](localEp);
        }

        public static void RegisterCodec(AudioCodec codec, Func<IPEndPoint, MediaCodec> factory)
        {
            CodecFactory[codec] = factory;
        }
    }
}