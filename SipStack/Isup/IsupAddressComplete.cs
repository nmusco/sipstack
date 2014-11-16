namespace SipStack.Isup
{
    using System.Collections.Generic;

    public class IsupAddressComplete : IsupBody
    {
        private readonly IDictionary<IsupParameterType, IsupParameter> optionalParameters = new Dictionary<IsupParameterType, IsupParameter>();

        public IsupAddressComplete(ByteStream bs)
            : this()
        {
            this.LoadData(bs);
        }

        public IsupAddressComplete() : base(IsupMessageType.AddressComplete)
        {
            this.BackwardCallIndicator = new BackwardCallIndicator();
        }

        public BackwardCallIndicator BackwardCallIndicator { get; private set; }

        public override T AddOptionalParameter<T>(T isupParameter)
        {
            this.optionalParameters[isupParameter.ParameterType] = isupParameter;

            return isupParameter;
        }

        protected override IsupParameter GetRequiredParameter()
        {
            return null;
        }

        protected override IEnumerable<IsupParameter> GetOptionalParameters()
        {
            return this.optionalParameters.Values;
        }

        protected override IEnumerable<IsupParameter> GetOptionalHeaders()
        {
            yield return this.BackwardCallIndicator;
        }
    }
}