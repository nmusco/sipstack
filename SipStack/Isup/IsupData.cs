namespace SipStack.Isup
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public enum IsupMessageType
    {
        Answer = 0x09,
        Release = 0x0c,
        IAM = 0x1,
        AddressComplete = 0x6
    }

    public enum RedirReason
    {
        NoReply = 1 << 5 // 0x20
    }

    public enum IsupParameterType
    {
        EndOfOptionalParameters = 0,
        TransmissionMediumRequirement = 0x2,

        CalledPartyNumber = 0x4,

        NatureOfConnectionIndicators = 0x10,

        ForwardCallIndicators = 0x7,

        OptionalForwardCallIndicator = 0x8,

        CallingPartyCategory = 0x9,

        CallingPartyNumber = 0x0a,

        BackwardCallIndicator = 0x11,

        OriginalCalledNumber = 0x28,

        RedirectingNumber = 0x0b,

        RedirectionInfo = 0x13,

        UserServiceInformation = 0x1d,

        MLPPPrecedence = 0x3A,

        OptionalBackwardCall = 0x29,

        CauseIndicator = 0x18
    }

    public abstract class IsupBody : Body
    {
        protected IsupBody(IsupMessageType isupType)
        {
            this.ContentType = "application/ISUP;version=itu-t92+;base=nxv3";
            this.Headers["Content-Disposition"] = "signal;handling=optional";
            this.IsupType = isupType;
        }

        public override string ContentText
        {
            get
            {
                return Encoding.Default.GetString(this.GetByteArray());
            }
        }

        public IsupMessageType IsupType { get; private set; }

        public static IsupBody Load(ByteStream bs)
        {
            var type = (IsupMessageType)bs.Read();
            switch (type)
            {
                case IsupMessageType.IAM:
                    return new IsupInitialAddress(bs);

                case IsupMessageType.AddressComplete:
                    return new IsupAddressComplete(bs);
            }

            throw new NotImplementedException();
        }

        public byte[] GetByteArray()
        {
            var optionalParameters = this.GetOptionalParameters();

            var parameters = new[] { this.GetOptionalHeaders().ToArray(), new[] { this.GetRequiredParameter() }, optionalParameters.ToArray() }.SelectMany(a => a).ToList();

            var bytes = new List<byte> { (byte)this.IsupType };
            foreach (var a in parameters.Where(a => a != null))
            {
                bytes.AddRange(a.Serialize());
            }

            if (optionalParameters != null)
            {
                bytes.Add(0);
            }

            return bytes.ToArray();
        }

        public abstract T AddOptionalParameter<T>(T isupParameter) where T : IsupParameter;

        protected void LoadData(ByteStream bs)
        {
            foreach (var h in this.GetOptionalHeaders())
            {
                h.Load(bs);
            }

            var required = this.GetRequiredParameter();
            if (required != null)
            {
                required.Load(bs);
            }

            while (true)
            {
                if (required == null)
                {
                    var startOfOptionalParameter = bs.Read();

                    if (startOfOptionalParameter == 0)
                    {
                        break;
                    }
                }

                var parameterType = (IsupParameterType)bs.Read();
                if (parameterType == IsupParameterType.EndOfOptionalParameters)
                {
                    break;
                }

                int len = bs.Read();
                var data = bs.Read(len);
                IsupParameter p;
                switch (parameterType)
                {
                    case IsupParameterType.CallingPartyNumber:
                    case IsupParameterType.RedirectingNumber:
                    case IsupParameterType.OriginalCalledNumber:
                        p = new IsupPhoneNumberParameter(parameterType);
                        break;
                    case IsupParameterType.RedirectionInfo:
                        p = new RedirectInfo();
                        break;
                    default:
                        p = new OptionalIsupParameter(parameterType, len);
                        break;
                }

                p.LoadParameterData(data);

                this.AddOptionalParameter(p);
            }
        }

        protected abstract IsupParameter GetRequiredParameter();

        protected abstract IEnumerable<IsupParameter> GetOptionalParameters();

        protected abstract IEnumerable<IsupParameter> GetOptionalHeaders();
    }
}