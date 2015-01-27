namespace SipStack
{
    using System;
    using System.IO;
    using System.Linq;

    using SipStack.Media;

    public class DebugCodec : IMediaCodec
    {
        private readonly IMediaCodec baseCodec;

        private readonly Stream rawStream;

        private readonly Stream wavStream;

        public DebugCodec(IMediaCodec baseCodec, Stream rawStream, Stream wavStream)
        {
            this.baseCodec = baseCodec;
            this.rawStream = rawStream;
            this.wavStream = wavStream;
        }

        public void OnPacketReceived(RtpPayload payload, int time)
        {
            this.rawStream.Write(payload.Data, 0, payload.Data.Length);
            var allBytes = payload.Data
                .Select(AlawMediaCodec.AlawEncoder.Alaw2Linear)
                .Select(b => BitConverter.GetBytes(b).Reverse())
                .SelectMany(a => a.ToArray()).ToArray();

            this.wavStream.Write(allBytes, 0, allBytes.Length);
            this.baseCodec.OnPacketReceived(payload, time);
        }

        public void SetRecordingDelegate(Action<RtpPayload> method)
        {
            this.baseCodec.SetRecordingDelegate(method);
        }
    }

    public class AlawMediaCodec : IMediaCodec
    {
        private readonly IPlaybackDevice playbackDevice;

        private readonly IRecordingDevice recordingDevice;

        public AlawMediaCodec(IPlaybackDevice playbackDevice, IRecordingDevice recordingDevice)
        {
            this.playbackDevice = playbackDevice;
            this.recordingDevice = recordingDevice;
        }

        public void OnPacketReceived(RtpPayload payload, int time)
        {
            var allBytes = payload.Data
                .Select(AlawEncoder.Alaw2Linear)
                .Select(BitConverter.GetBytes)
                .SelectMany(a => a.ToArray())
                .ToArray();
            this.playbackDevice.PlaySample(allBytes);
        }

        public void SetRecordingDelegate(Action<RtpPayload> method)
        {
            this.recordingDevice.SetSendDelegate(method);
        }

        public static class AlawEncoder
        {
            /* Sign bit for a A-law byte. */

            private const byte SignBit = 0x80;

            /* Quantization field mask. */

            private const byte QuantMask = 0xf;

            /* Number of A-law segments. */

            private const byte Nsegs = 8;

            /* Left shift for segment number. */

            private const byte SegShift = 4;

            /* Segment field mask. */

            private const byte SegMask = 0x70;

            private static readonly int[] SegEnd = { 0xFF, 0x1FF, 0x3FF, 0x7FF, 0xFFF, 0x1FFF, 0x3FFF, 0x7FFF };

            private static byte LinearToAlaw(int pcmVal)
            {
                int mask;
                int seg;

                if (pcmVal >= 0)
                {
                    /* sign (7th) bit = 1 */
                    mask = 0xD5;
                }
                else
                {
                    /* sign bit = 0 */
                    mask = 0x55;
                    pcmVal = -pcmVal - 8;

                    /* https://trac.pjsip.org/repos/ticket/1301 
                     * Thank you K Johnson - Zetron - 27 May 2011
                     */
                    if (pcmVal < 0)
                    {
                        pcmVal = 0;
                    }
                }


                seg = Search(pcmVal, SegEnd, 8);


                /* Combine the sign, segment, and quantization bits. */

                /* out of range, return maximum value. */
                if (seg >= 8)
                {
                    return (byte)(0x7F ^ mask);
                }

                var aval = seg << SegShift;
                if (seg < 2)
                {
                    aval |= (pcmVal >> 4) & QuantMask;
                }
                else
                {
                    aval |= (pcmVal >> (seg + 3)) & QuantMask;
                }

                return (byte)(aval ^ mask);
            }

            private static unsafe int Search(int val, int[] table, int size)
            {
                int i;

                for (i = 0; i < size; i++)
                {
                    if (val <= table[i])
                    {
                        return i;
                    }
                }

                return size;
            }


            public static short Alaw2Linear(byte value)
            {
                int t;
                int seg;

                value ^= 0x55;

                t = (value & QuantMask) << 4;
                seg = (value & SegMask) >> SegShift;
                switch (seg)
                {
                    case 0:
                        t += 8;
                        break;
                    case 1:
                        t += 0x108;
                        break;
                    default:
                        t += 0x108;
                        t <<= seg - 1;
                        break;
                }

                return (short)((value & SignBit) > 0 ? t : -t);
            }
        }
    }
}