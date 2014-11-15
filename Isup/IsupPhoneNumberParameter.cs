namespace SipStack.Isup
{
    using System.Linq;

    public class IsupPhoneNumberParameter : OptionalIsupParameter
    {
        public IsupPhoneNumberParameter(IsupParameterType parameterType)
        {
            this.ParameterType = parameterType;   
        }

        public string Number
        {
            get;
            set;
        }

        public PhoneFlags Flags { get; set; }

        public NAIFlags NumberingFlags { get; set; }

        public override byte[] GetParameterData()
        {
            if (string.IsNullOrWhiteSpace(this.Number))
            {
                return new byte[0];
            }

            var hexData = IsupUtility.GetReversedNumber(this.Number).ToArray();
            var isEven = (this.Number.Length % 2) == 0;
            this.Flags = isEven ? PhoneFlags.IsEven : PhoneFlags.IsOdd;
            this.Flags |= PhoneFlags.NAINationalNumber;
            this.NumberingFlags |= NAIFlags.Isdn;

            return new[] { (byte)this.Flags, (byte)this.NumberingFlags }.Concat(hexData).ToArray();
        }

        public override void LoadParameterData(byte[] parameterData)
        {
            this.Flags = (PhoneFlags)parameterData[0];
            this.NumberingFlags = (NAIFlags)parameterData[1];

            this.Number = IsupUtility.UnReverseNumber(parameterData, 2, this.Flags.HasFlag(PhoneFlags.IsOdd));
        }
    }
}