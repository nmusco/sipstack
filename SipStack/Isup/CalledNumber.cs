namespace SipStack.Isup
{
    using System;
    using System.Linq;

    public class CalledNumber : RequiredIsupParameter
    {
        public CalledNumber() : base(IsupParameterType.CalledPartyNumber)
        {
            this.PointerToParameter = 2;
        }

        public string Number { get; set; }

        public PhoneFlags Flags { get; set; }

        public NAIFlags NumberingFlags { get; set; }

        public override byte[] GetParameterData()
        {
            if (string.IsNullOrWhiteSpace(this.Number))
            {
                throw new InvalidOperationException("Cant serialize an empty called number");
            }

            var hexData = IsupUtility.GetReversedNumber(this.Number).ToArray();
            this.PointerToOptionalParameter = (byte)(hexData.Length + this.PointerToParameter + 2);
            var isEven = (this.Number.Length % 2) == 0;
            this.Flags |= isEven ? PhoneFlags.IsEven : PhoneFlags.IsOdd;

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