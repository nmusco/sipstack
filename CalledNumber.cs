namespace SipStack.Isup
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;


    [Flags]
    public enum PhoneFlags
    {
        IsOdd = 128,
        IsEven = 0,
        NAINationalNumber = 3,
    }

    [Flags]
    public enum NAIFlags
    {
        RoutingNotAllowed = 128,
        RoutingAllowed = 0,
        IsdnNPI = 1 << 5
    }

    public class CalledNumber : RequiredIsupParameter
    {
        public string Number { get; set; }

        public PhoneFlags Flags { get; set; }

        public NAIFlags NumberingFlags { get; set; }


        public override byte[] GetParameterData()
        {
            return new[] { (byte)this.Flags, (byte)this.NumberingFlags }.Concat(IsupUtility.GetReversedNumber(this.Number)).ToArray();
        }

        public override void LoadParameterData(byte[] data)
        {
            this.Flags = (PhoneFlags)data[0];
            this.NumberingFlags = (NAIFlags)data[1];

            this.Number = IsupUtility.UnReverseNumber(data, 2, this.Flags.HasFlag(PhoneFlags.IsOdd));
        }
    }
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
            return new[] { (byte)this.Flags, (byte)this.NumberingFlags }.Concat(IsupUtility.GetReversedNumber(this.Number)).ToArray();
        }

        public override void LoadParameterData(byte[] data)
        {
            this.Flags = (PhoneFlags)data[0];
            this.NumberingFlags = (NAIFlags)data[1];

            this.Number = IsupUtility.UnReverseNumber(data, 2, this.Flags.HasFlag(PhoneFlags.IsOdd));
        }

    }

    class IsupUtility
    {
        internal static string UnReverseNumber(byte[] buffer, int start, bool isOdd)
        {
            var sb = new StringBuilder();
            for (var i = start; i < buffer.Length; i++)
            {
                sb.Append(buffer[i].ToString("D2"));
            }

            var str = sb.ToString();
            if (!isOdd)
            {
                return str;
            }

            var lastDigit = str.Substring(str.Length - 2);

            return str.Substring(0, str.Length - 2) + lastDigit[0];
        }

        internal static IEnumerable<byte> GetReversedNumber(string number)
        {
            var isEven = (number.Length % 2) == 0;
            for (var i = 0; i < number.Length; i += 2)
            {
                if (!isEven && i + 1 == number.Length)
                {
                    yield return byte.Parse("0" + number[i]);
                }
                else
                {
                    yield return byte.Parse(number[i + 1] + number[i].ToString(CultureInfo.InvariantCulture));
                }
            }
        }
    }

    /// <summary>
    /// It's a Isup IAM
    /// </summary>
    public class IsupInitialAddress : IsupBody
    {
        private readonly CalledNumber calledPartyNumber;



        private readonly IDictionary<IsupParameterType, IsupParameter> parameters = new Dictionary<IsupParameterType, IsupParameter>();

        public IsupInitialAddress()
        {
            this.calledPartyNumber = new CalledNumber();

            this.parameters[IsupParameterType.NatureOfConnectionIndicators] = new IsupHeader(IsupParameterType.NatureOfConnectionIndicators, 1);
            this.parameters[IsupParameterType.ForwardCallIndicators] = new IsupHeader(IsupParameterType.ForwardCallIndicators, 2);
            this.parameters[IsupParameterType.CallingPartyCategory] = new IsupHeader(IsupParameterType.CallingPartyCategory, 1);
            this.parameters[IsupParameterType.TransmissionMediumRequirement] = new IsupHeader(IsupParameterType.TransmissionMediumRequirement, 1);

            this.parameters[IsupParameterType.CallingPartyNumber] = new IsupPhoneNumberParameter(IsupParameterType.CallingPartyNumber);
            this.parameters[IsupParameterType.OriginalCallingNumber] = new IsupPhoneNumberParameter(IsupParameterType.OriginalCallingNumber);
            this.parameters[IsupParameterType.RedirectingNumber] = new IsupPhoneNumberParameter(IsupParameterType.RedirectingNumber);
            this.parameters[IsupParameterType.RedirectionInfo] = new OptionalIsupParameter(IsupParameterType.RedirectionInfo, 2);
        }

        public IsupInitialAddress(ByteStream bs)
            : this()
        {
            // TODO: Complete member initialization
            this.parameters[IsupParameterType.NatureOfConnectionIndicators].Load(bs);
            this.parameters[IsupParameterType.ForwardCallIndicators].Load(bs);
            this.parameters[IsupParameterType.CallingPartyCategory].Load(bs);
            this.parameters[IsupParameterType.TransmissionMediumRequirement].Load(bs);
            this.calledPartyNumber.Load(bs);
            while (true)
            {
                var parameterType = (IsupParameterType)bs.Read();
                if (parameterType == IsupParameterType.EndOfOptionalParameters)
                {
                    break;
                }

                int len = (byte)bs.Read();
                var data = bs.Read(len);

                this.parameters[parameterType].LoadParameterData(data);
            }
        }

        public string CallingNumber
        {
            get
            {
                return ((IsupPhoneNumberParameter)this.parameters[IsupParameterType.CallingPartyNumber]).Number;
            }
            set
            {
                ((IsupPhoneNumberParameter)this.parameters[IsupParameterType.CallingPartyNumber]).Number = value;
            }
        }

        public string CalledNumber
        {
            get
            {
                return this.calledPartyNumber.Number;
            }
            set
            {
                this.calledPartyNumber.Number = value;
            }
        }

        public string RedirectingNumber
        {
            get
            {
                return ((IsupPhoneNumberParameter)this.parameters[IsupParameterType.RedirectingNumber]).Number;
            }
            set
            {
                ((IsupPhoneNumberParameter)this.parameters[IsupParameterType.RedirectingNumber]).Number = value;
            }
        }

        public RedirReason RedirectReason { get; set; }

        public byte RedirectCounter { get; set; }

        public string OriginalCalledNumber
        {
            get
            {
                return ((IsupPhoneNumberParameter)this.parameters[IsupParameterType.OriginalCallingNumber]).Number;
            }
            set
            {
                ((IsupPhoneNumberParameter)this.parameters[IsupParameterType.OriginalCallingNumber]).Number = value;
            }
        }

        protected override IsupMessageType Type
        {
            get
            {
                return IsupMessageType.IAM;
            }
        }

        public void WriteTo(BinaryWriter writer)
        {
            writer.Write(this.CalledNumber.SafeEmpty());
            writer.Write(this.CallingNumber.SafeEmpty());
            writer.Write(this.OriginalCalledNumber.SafeEmpty());
            writer.Write(this.RedirectCounter);
            writer.Write((byte)this.RedirectReason);
            writer.Write(this.RedirectingNumber);
        }

        protected override IsupParameter GetRequiredParameter()
        {
            return this.calledPartyNumber;
        }

        protected override IEnumerable<IsupParameter> GetOptionalHeaders()
        {
            return this.parameters.Values.OfType<IsupHeader>();
        }

        protected override IEnumerable<IsupParameter> GetOptionalParameters()
        {
            return this.parameters.OfType<OptionalIsupParameter>();
        }

        private void ReadFrom(BinaryReader reader)
        {
            this.CalledNumber = reader.ReadString();
            this.CallingNumber = reader.ReadString();
            this.OriginalCalledNumber = reader.ReadString();
            this.RedirectCounter = reader.ReadByte();
            this.RedirectReason = (RedirReason)reader.ReadByte();
            this.RedirectingNumber = reader.ReadString();
        }
    }
}