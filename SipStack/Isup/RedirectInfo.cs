namespace SipStack.Isup
{
    using System;

    public class RedirectInfo : OptionalIsupParameter
    {
        public enum RedirectIndicator
        {
            RedirectPresentationRestricted = 0x4,

            CallDiverted = 3
        }

        public RedirectInfo()
            : base(IsupParameterType.RedirectionInfo, 2)
        {
        }

        public byte RedirectCounter { get; set; }

        public RedirReason RedirectReason { get; set; }

        public RedirectIndicator RedirectIndicatorFlags { get; set; }

        public override byte[] GetParameterData()
        {
            return new[] { (byte)this.RedirectIndicatorFlags, (byte)(this.RedirectCounter | (byte)this.RedirectReason) };
        }

        public override void LoadParameterData(byte[] parameterData)
        {
            this.RedirectIndicatorFlags = (RedirectIndicator)parameterData[0];
            this.RedirectCounter = (byte)(parameterData[1] & 0x07);
            this.RedirectReason = (RedirReason)(parameterData[1] & 0x20);
        }
    }
}