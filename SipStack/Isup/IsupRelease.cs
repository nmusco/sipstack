namespace SipStack.Isup
{
    using System.Collections.Generic;

    public class IsupRelease : IsupBody
    {
        public IsupRelease()
            : base(IsupMessageType.Release)
        {
        }

        public IsupRelease(ByteStream data)
            : this()
        {
        }

        public override T AddOptionalParameter<T>(T isupParameter)
        {
            throw new System.NotImplementedException();
        }

        protected override IsupParameter GetRequiredParameter()
        {
            throw new System.NotImplementedException();
        }

        protected override IEnumerable<IsupParameter> GetOptionalHeaders()
        {
            throw new System.NotImplementedException();
        }

        protected override IEnumerable<IsupParameter> GetOptionalParameters()
        {
            throw new System.NotImplementedException();
        }
    }
}