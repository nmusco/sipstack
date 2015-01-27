namespace SipStack
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Threading;

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

    public class NAudioRecordDevice : IRecordingDevice
    {
        private static readonly IWaveIn instance;

        private Action<RtpPayload> method;

        private RtpEvent currentEvent;

        private readonly Stopwatch watcher;

        private int sequenceNumber;

        static NAudioRecordDevice()
        {
            instance = new WaveInEvent() { BufferMilliseconds = 20, NumberOfBuffers = 5 };
        }

        public NAudioRecordDevice()
        {
            this.watcher = Stopwatch.StartNew();
            instance.WaveFormat = new WaveFormat(8000, 16, 1);
            instance.DataAvailable += this.DataAvailable;
            instance.StartRecording();
        }

        private void DataAvailable(object sender, WaveInEventArgs e)
        {
            if (this.currentEvent != null)
            {
                if (this.currentEvent.IsValid())
                {
                    var data = this.currentEvent.GetData(this.watcher.ElapsedMilliseconds);
                    this.method.Invoke(data);
                }
                else
                {
                    this.currentEvent = null;
                }
            }
        }


        public void PlayDtmf(Digit digit, int duration)
        {
            this.currentEvent = new RtpEvent(digit, duration, (short)Interlocked.Increment(ref this.sequenceNumber), 0xFA);
        }

        public void SetSendDelegate(Action<RtpPayload> method)
        {
            this.method = method;
        }
    }
}