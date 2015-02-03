namespace SipStack
{
    using System.Collections.Generic;

    public class MediaCodecDescriptor
    {
        public interface IMediaCodecInfo
        {
            int[] Identifiers { get; }

            IEnumerable<KeyValuePair<string, string>> Parameters { get; }
        }

        public static IMediaCodecInfo Describe(MediaGateway.AudioCodec codec)
        {
            switch (codec)
            {
                case MediaGateway.AudioCodec.G711Alaw:
                    return new AlawCodecInfo();
            }

            return null;
        }

        internal sealed class AlawCodecInfo : IMediaCodecInfo
        {
            public AlawCodecInfo()
            {
                this.Identifiers = new[] { 8, 101 };

                var parameters = new List<KeyValuePair<string, string>>();
                parameters.Add("a", "rtpmap:8 PCMA/8000")
                    .Add("a", "rtpmap:101 telephone-event/8000")
                    .Add("a", "fmtp:101 0-15");
                this.Parameters = parameters;
            }

            public int[] Identifiers { get; private set; }

            public IEnumerable<KeyValuePair<string, string>> Parameters { get; private set; }
        }
    }
}