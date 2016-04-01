namespace SipStack
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    using NAudio.Wave;

    using SipStack.Media;

    public class NAudioRecordDevice : IRecordingDevice
    {
        private static readonly IWaveIn Instance;

        private readonly Stopwatch watcher;

        private Action<RtpPayload> method;

        private RtpEvent currentEvent;

        private int sequenceNumber;

        static NAudioRecordDevice()
        {
            Instance = new WaveInEvent { BufferMilliseconds = 20, NumberOfBuffers = 5 };
        }

        public NAudioRecordDevice()
        {
            this.watcher = Stopwatch.StartNew();
            Instance.WaveFormat = new WaveFormat(8000, 16, 1);
            Instance.DataAvailable += this.DataAvailable;
            Instance.StartRecording();
        }

        public void PlayDtmf(Digit digit, int duration)
        {
            this.currentEvent = new RtpEvent(digit, duration, (short)Interlocked.Increment(ref this.sequenceNumber), 0xFA);
        }

        public void SetSendDelegate(Action<RtpPayload> delegateMethod)
        {
            this.method = delegateMethod;
        }

        private void DataAvailable(object sender, WaveInEventArgs e)
        {
            if (this.method == null)
            {
                return;
            }

            if (this.currentEvent == null)
            {
                this.method.Invoke(new RtpPayload(e.Buffer, (short)Interlocked.Increment(ref this.sequenceNumber), 0xFA, this.watcher.ElapsedMilliseconds, false));
                return;
            }

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

        public void Stop()
        {
            Instance.StopRecording();
        }
    }
}