namespace SipStack.Isup
{
    using System.Collections.Generic;

    public class EventInformation : RequiredIsupParameter
    {
        public enum EventIndicator
        {
            Progress = 0x2
        }

        public EventInformation()
            : base(IsupParameterType.EventInformation, 1)
        {
        }

        public EventIndicator Indicator { get; set; }

        public override void Load(ByteStream byteStream)
        {
            this.Indicator = (EventIndicator)byteStream.Read();
            this.PointerToOptionalParameter = byteStream.Read();
        }

        public override IEnumerable<byte> Serialize()
        {
            yield return (byte)this.Indicator;
            yield return 1;
        }

        public override byte[] GetParameterData()
        {
            return new[] { (byte)this.Indicator };
        }
    }
}