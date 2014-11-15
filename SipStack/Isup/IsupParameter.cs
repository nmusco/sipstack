namespace SipStack.Isup
{
    using System.Collections.Generic;

    public abstract class IsupParameter
    {
        protected IsupParameter()
        {
            this.IsPresent = true;
        }

        public IsupParameterType ParameterType { get; set; }
        
        public bool Mandatory { get; set; }

        public bool IsPresent { get; set; }


        public abstract IEnumerable<byte> Serialize();

        public abstract void Load(ByteStream byteStream);

        public abstract byte[] GetParameterData();

        public abstract void LoadParameterData(byte[] parameterData);
    }
}