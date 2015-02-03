namespace SipStack
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using SipStack.Media;

    public class MediaGateway
    {
        private static readonly int InitialPort;

        private static readonly IDictionary<AudioCodec, Func<IMediaCodec>> CodecFactory = new Dictionary<AudioCodec, Func<IMediaCodec>>();

        private static int currentPort = InitialPort;

        static MediaGateway()
        {
            InitialPort = (int)(10000 + (DateTime.Now.Ticks % 999));
            currentPort = InitialPort;
        }

        public enum AudioCodec
        {
            G711Alaw = 8
        }

        public static int GetNextPort()
        {
            var nextPort = Interlocked.Increment(ref currentPort);
            Interlocked.CompareExchange(ref currentPort, InitialPort, InitialPort);
            return nextPort;
        }

        public static IMediaCodec CreateMedia(AudioCodec codec)
        {
            if (!CodecFactory.ContainsKey(codec))
            {
                throw new ArgumentOutOfRangeException("codec", string.Format("Factory for codec {0} not found", codec));
            }

            Interlocked.CompareExchange(ref currentPort, InitialPort, InitialPort);

            return CodecFactory[codec]();
        }

        public static void RegisterCodec(AudioCodec codec, Func<IMediaCodec> factory)
        {
            CodecFactory[codec] = factory;
        }

        public static IEnumerable<AudioCodec> GetRegisteredCodecs()
        {
            return CodecFactory.Keys;
        }
    }
}