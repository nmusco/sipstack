namespace SipStack.Isup
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;

    public class IsupCallProgress : IsupBody
    {
        private readonly OrderedDictionary parameters = new OrderedDictionary();

        private readonly EventInformation eventInformation = new EventInformation();

        public EventInformation EventInformation
        {
            get
            {
                return this.eventInformation;
            }
        }

        public IsupCallProgress()
            : base(IsupMessageType.CallProgress)
        {
        }
        public IsupCallProgress(ByteStream bs)
            : this()
        {
            this.LoadData(bs);
        }
        public override T AddOptionalParameter<T>(T isupParameter)
        {
            this.parameters[isupParameter.ParameterType] = isupParameter;
            return isupParameter;
        }

        protected override IsupParameter GetRequiredParameter()
        {
            return this.eventInformation;
        }

        protected override IEnumerable<IsupParameter> GetOptionalHeaders()
        {
            return this.parameters.Values.OfType<IsupHeader>();
        }

        protected override IEnumerable<IsupParameter> GetOptionalParameters()
        {
            return this.parameters.Values.OfType<OptionalIsupParameter>();
        }
    }
}