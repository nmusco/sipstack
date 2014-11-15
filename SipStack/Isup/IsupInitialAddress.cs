namespace SipStack.Isup
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;

    /// <summary>
    /// It's a Isup IAM
    /// </summary>
    public class IsupInitialAddress : IsupBody
    {
        private readonly CalledNumber calledPartyNumber;

        private readonly OrderedDictionary parameters = new OrderedDictionary();

        public IsupInitialAddress()
            : base(IsupMessageType.IAM)
        {
            this.calledPartyNumber = new CalledNumber();

            this.parameters[IsupParameterType.NatureOfConnectionIndicators] = new NatureOfConnection();
            this.parameters[IsupParameterType.ForwardCallIndicators] = new ForwardCallIndicators();
            this.parameters[IsupParameterType.CallingPartyCategory] = new CallingPartyCategory();
            this.parameters[IsupParameterType.TransmissionMediumRequirement] = new IsupHeader(IsupParameterType.TransmissionMediumRequirement, 1);
        }

        public IsupInitialAddress(ByteStream bs)
            : this()
        {
            this.LoadData(bs);
        }

        /// <summary>
        /// Gets the nature of connection indicator
        /// </summary>
        public NatureOfConnection NatureOfConnectionIndicator
        {
            get
            {
                return this.parameters[IsupParameterType.NatureOfConnectionIndicators] as NatureOfConnection;
            }
        }

        public ForwardCallIndicators ForwardCallIndicator
        {
            get
            {
                return this.parameters[IsupParameterType.ForwardCallIndicators] as ForwardCallIndicators;
            }
        }

        public CallingPartyCategory CallingPartyCategory
        {
            get
            {
                return (CallingPartyCategory)this.parameters[IsupParameterType.CallingPartyCategory];
            }
        }

        public CalledNumber CalledNumber
        {
            get
            {
                return this.calledPartyNumber;
            }
        }

        public override T AddOptionalParameter<T>(T isupParameter)
        {
            this.parameters[isupParameter.ParameterType] = isupParameter;
            return isupParameter;
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
            return this.parameters.Values.OfType<OptionalIsupParameter>();
        }
    }
}