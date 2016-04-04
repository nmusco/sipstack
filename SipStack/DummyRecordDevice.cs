namespace SipStack
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    using SipStack.Media;

    public class DummyRecordDevice : IRecordingDevice
    {
        private readonly Stopwatch watch = new Stopwatch();

        private readonly Timer ptime;

        private Action<RtpPayload> method;

        private RtpEvent currentEvent;

        private long sequenceNumber;


        public DummyRecordDevice()
        {
            this.ptime = new Timer(this.OnPacketTime, null, -1, 20);
            this.watch.Start();
        }


        public void PlayDtmf(Digit digit, int duration)
        {
            this.currentEvent = new RtpEvent(digit, duration, (short)Interlocked.Increment(ref this.sequenceNumber), 0xFA);
            //this.currentEventDuration = duration;
            throw new NotImplementedException();
        }

        public void SetSendDelegate(Action<RtpPayload> delegateMethod)
        {
            this.method = delegateMethod;
            this.ptime.Change(0, 20);
        }

        private void OnPacketTime(object state)
        {
            if (this.method == null || this.currentEvent == null)
            {
                // TODO: send mute data here
                return;
            }

            if (this.currentEvent.IsValid())
            {
                var data = this.currentEvent.GetData(this.watch.ElapsedMilliseconds);
                this.method.Invoke(data);
            }
            else
            {
                this.currentEvent = null;
            }
        }
    }
}