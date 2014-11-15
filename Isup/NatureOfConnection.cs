namespace SipStack.Isup
{
    public class NatureOfConnection : IsupHeader
    {
        public NatureOfConnection()
            : base(IsupParameterType.NatureOfConnectionIndicators, 1)
        {
        }

        public enum SatelliteIndicatorFlags
        {
            None = 0,
            One = 0x01,
            Two = 0x02,
            Three = 0x03
        }

        public enum ContinuityCheckIndicatorFlags
        {
            NotRequired = 0x0,
            Required = 0x01,
            Previous = 0x02,
            Spare = 0x03
        }

        public SatelliteIndicatorFlags SatelliteIndicator { get; set; }

        public ContinuityCheckIndicatorFlags ContinuityCheckIndicator { get; set; }

        public bool EchoControlIncluded { get; set; }

        public override void LoadParameterData(byte[] parameterData)
        {
            this.EchoControlIncluded = parameterData[0] >> 5 == 1;
            this.ContinuityCheckIndicator = (ContinuityCheckIndicatorFlags)((parameterData[0] & 0x20) >> 2);
            this.SatelliteIndicator = (SatelliteIndicatorFlags)((parameterData[0] & 0x20) ^ (byte)this.ContinuityCheckIndicator);
            base.LoadParameterData(parameterData);
        }

        public override byte[] GetParameterData()
        {
            byte b = 0;
            if (this.EchoControlIncluded)
            {
                b |= 0x10;
            }

            b |= (byte)this.SatelliteIndicator;
            b |= (byte)((byte)this.ContinuityCheckIndicator << 2);
            return new[] { b };
        }
    }
}