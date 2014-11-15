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
        IAM = 0x1
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

        MLPPPrecedence = 0x3A
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

        public static IsupBody Load(byte[] data)
        {
            var bs = new ByteStream(data, 0);
            switch ((IsupMessageType)bs.Read())
            {
                case IsupMessageType.Answer:
                    return new IsupAnswer(bs);
                case IsupMessageType.Release:
                    return new IsupRelease(bs);
                case IsupMessageType.IAM:
                    return new IsupInitialAddress(bs);
                default:
                    throw new NotImplementedException();
            }
        }

        protected virtual IsupParameter GetRequiredParameter()
        {
            return null;
        }

        protected virtual IEnumerable<IsupParameter> GetOptionalParameters()
        {
            yield break;
        }

        protected virtual IEnumerable<IsupParameter> GetOptionalHeaders()
        {
            yield break;
        }

        public byte[] GetByteArray()
        {
            var optionalParameters = this.GetOptionalParameters();

            var parameters =
                new[] { this.GetOptionalHeaders(), new[] { this.GetRequiredParameter() }, optionalParameters }.SelectMany(a => a).ToList();
            
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
        private string ToHexString()
        {
            return this.GetByteArray().ToHex();
        }
    }
}