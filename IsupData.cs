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
        NoReply = 20
    }

    public enum IsupParameterType
    {
        EndOfOptionalParameters = 0,
        TransmissionMediumRequirement = 0x2,

        CalledPartyNumber = 0x4,

        NatureOfConnectionIndicators = 0x06,

        ForwardCallIndicators = 0x7,

        CallingPartyCategory = 0x9,

        CallingPartyNumber = 0x0a,

        BackwardCallIndicator = 0x11,

        OriginalCallingNumber = 0x28,

        RedirectingNumber = 0x0b,

        RedirectionInfo = 0x13
    }

    public abstract class IsupBody : Body
    {
        protected IsupBody()
        {
            this.ContentType = "application/isup; version=itu-t92+; base=itu-t92+";
            this.Headers["Content-Disposition"] = "signal; handling=optional";
        }

        public override string ContentText
        {
            get
            {
                return this.ToHexString();
            }
        }

        protected abstract IsupMessageType Type { get; }

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

        private string ToHexString()
        {
            var optionalParameters = this.GetOptionalParameters();

            var parameters =
                new[] { new[] { this.GetRequiredParameter() }, this.GetOptionalHeaders(), optionalParameters }.SelectMany(a => a).ToList();
            
            var bytes = new List<byte> { (byte)this.Type };
            foreach (var a in parameters.Where(a => a != null))
            {
                bytes.AddRange(a.Serialize());
            }

            if (optionalParameters != null)
            {
                bytes.Add(0);
            }

            var sb = new StringBuilder(); // initial address
            foreach (var b in bytes)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }
    }
}