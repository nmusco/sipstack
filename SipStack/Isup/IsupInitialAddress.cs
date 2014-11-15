namespace SipStack.Isup
{
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
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
            /*
            this.parameters[IsupParameterType.CallingPartyNumber] = new IsupPhoneNumberParameter(IsupParameterType.CallingPartyNumber);

            this.parameters[IsupParameterType.OptionalForwardCallIndicator] = new OptionalForwardCallIndicator();
            this.parameters[IsupParameterType.UserServiceInformation] = new OptionalIsupParameter(IsupParameterType.UserServiceInformation, 3, new byte[] { 0x80, 0x90, 0xA3 });
            this.parameters[IsupParameterType.OriginalCallingNumber] = new IsupPhoneNumberParameter(IsupParameterType.OriginalCallingNumber);
            this.parameters[IsupParameterType.RedirectingNumber] = new IsupPhoneNumberParameter(IsupParameterType.RedirectingNumber);
            this.parameters[IsupParameterType.RedirectionInfo] = new RedirectInfo();
            
            this.OptionalForwardCallIndicator.IsPresent = false;*/
        }

        public IsupInitialAddress(ByteStream bs)
            : this()
        {
            // TODO: Complete member initialization
            ((IsupParameter)this.parameters[IsupParameterType.NatureOfConnectionIndicators]).Load(bs);
            ((IsupParameter)this.parameters[IsupParameterType.ForwardCallIndicators]).Load(bs);
            ((IsupParameter)this.parameters[IsupParameterType.CallingPartyCategory]).Load(bs);
            ((IsupParameter)this.parameters[IsupParameterType.TransmissionMediumRequirement]).Load(bs);
            this.calledPartyNumber.Load(bs);
            while (true)
            {
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
                    case IsupParameterType.OptionalForwardCallIndicator:
                        p = new OptionalForwardCallIndicator();
                        break;
                    case IsupParameterType.RedirectionInfo:
                        p = new RedirectInfo();
                        break;
                    default:
                        p = new OptionalIsupParameter(parameterType, len);
                        break;
                }
                p.LoadParameterData(data);


                this.parameters[parameterType] = p;
            }
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

        /*
        public OptionalForwardCallIndicator OptionalForwardCallIndicator
        {
            get
            {
                return (OptionalForwardCallIndicator)this.parameters[IsupParameterType.OptionalForwardCallIndicator];
            }
        }

       

        public IsupPhoneNumberParameter OriginalCalledNumber
        {
            get
            {
                return (IsupPhoneNumberParameter)this.parameters[IsupParameterType.OriginalCallingNumber];
            }
        }

        public IsupPhoneNumberParameter RedirectingNumber
        {
            get
            {
                return (IsupPhoneNumberParameter)this.parameters[IsupParameterType.RedirectingNumber];
            }
        }

        /// <summary>
        /// Gets the calling number information
        /// </summary>
        public IsupPhoneNumberParameter CallingNumber
        {
            get
            {
                return (IsupPhoneNumberParameter)this.parameters[IsupParameterType.CallingPartyNumber];
            }
        }

        public RedirectInfo RedirInfo
        {
            get
            {
                return (RedirectInfo)this.parameters[IsupParameterType.RedirectionInfo];
            }
        }
        */
        /*public void WriteTo(BinaryWriter writer)
        {
            writer.Write(this.CalledNumber.Number.SafeEmpty());
            writer.Write(this.CallingNumber.Number.SafeEmpty());
            writer.Write(this.OriginalCalledNumber.Number.SafeEmpty());
            writer.Write(this.RedirInfo.RedirectCounter);
            writer.Write((byte)this.RedirInfo.RedirectReason);
            writer.Write(this.RedirectingNumber.Number);
        }*/

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

        /*private void ReadFrom(BinaryReader reader)
        {
            this.CalledNumber.Number = reader.ReadString();
            this.CallingNumber.Number = reader.ReadString();
            this.OriginalCalledNumber.Number = reader.ReadString();
            this.RedirInfo.RedirectCounter = reader.ReadByte();
            this.RedirInfo.RedirectReason = (RedirReason)reader.ReadByte();
            this.RedirectingNumber.Number = reader.ReadString();
        }*/

        public T AddOptionalParameter<T>(T isupParameter) where T : IsupParameter
        {
            this.parameters[isupParameter.ParameterType] = isupParameter;
            return isupParameter;

        }
    }
}