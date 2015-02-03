namespace SipStack
{
    using NAudio.Wave;

    using SipStack.Media;

    public class NAudioPlaybackDevice : IPlaybackDevice
    {
        private static readonly NAudioPlaybackDevice CurrentInstance;

        private readonly DirectSoundOut device;

        private readonly BufferedWaveProvider bwp;

        static NAudioPlaybackDevice()
        {
            CurrentInstance = new NAudioPlaybackDevice();
        }

        private NAudioPlaybackDevice()
        {
            this.device = new DirectSoundOut { Volume = 1 };
            var outFormat = new WaveFormat(8000, 16, 1);
            this.bwp = new BufferedWaveProvider(outFormat) { BufferLength = 8000 * 32, DiscardOnBufferOverflow = true };

            this.device.Init(this.bwp);
            this.device.Play();
        }

        public static IPlaybackDevice Instance
        {
            get { return CurrentInstance; }
        }

        public void PlaySample(byte[] buffer)
        {
            this.bwp.AddSamples(buffer, 0, buffer.Length);
        }

        public void SetVolume(float vol)
        {
            this.device.Volume = vol;
        }
    }
}