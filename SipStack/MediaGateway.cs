namespace SipStack
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Threading;

    using SipStack.Media;

    public class MediaGateway
    {
        private static readonly int initialPort;

        private static readonly IDictionary<AudioCodec, Func<IMediaCodec>> CodecFactory = new Dictionary<AudioCodec, Func<IMediaCodec>>();

        private static int currentPort = initialPort;

        public enum AudioCodec
        {
            G711Alaw
        }

        static MediaGateway()
        {
            initialPort = (int)(10000 + (DateTime.Now.Ticks * 999));
        }

        public static int GetNextPort()
        {
            var nextPort = Interlocked.Increment(ref currentPort);
            Interlocked.CompareExchange(ref currentPort, initialPort, initialPort);
            return nextPort;
        }

        public static IMediaCodec CreateMedia(AudioCodec codec)
        {
            if (!CodecFactory.ContainsKey(codec))
            {
                throw new ArgumentOutOfRangeException("codec", string.Format("Factory for codec {0} not found", codec));
            }

            Interlocked.CompareExchange(ref currentPort, initialPort, initialPort);

            return CodecFactory[codec]();
        }

        public static void RegisterCodec(AudioCodec codec, Func<IMediaCodec> factory)
        {
            CodecFactory[codec] = factory;
        }
    }
}